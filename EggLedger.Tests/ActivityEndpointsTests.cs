using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers the room activity feed: it merges orders, settlements, and approved memberships from
/// three independent sources into one timeline, sorted newest-first and room-scoped.
/// </summary>
public class ActivityEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public ActivityEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private sealed class ActivityEvent
    {
        public int EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActorName { get; set; } = "";
        public string? CounterpartyName { get; set; }
        public string? ContainerName { get; set; }
        public int? Quantity { get; set; }
        public decimal? Amount { get; set; }
    }

    private static async Task<int> CreateOpenRoomAsync(HttpClient client, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/room/create/").WithAuth(accessToken);
        request.Content = JsonContent.Create(new { roomName = $"Room {Guid.NewGuid():N}", isOpen = true });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, "value", StringComparison.OrdinalIgnoreCase))
            .Value.GetInt32();
    }

    private static async Task JoinRoomAsync(HttpClient client, string accessToken, int roomCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/join/{roomCode}").WithAuth(accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task StockAsync(HttpClient client, string accessToken, int roomCode, int quantity, decimal amount)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/stock").WithAuth(accessToken);
        request.Content = JsonContent.Create(new { containerName = $"Carton {Guid.NewGuid():N}", quantity, amount });
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ConsumeAsync(HttpClient client, string accessToken, int roomCode, int quantity)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/consume").WithAuth(accessToken);
        request.Content = JsonContent.Create(new { quantity });
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<List<ActivityEvent>> GetActivityAsync(HttpClient client, string accessToken, int roomCode, int? page = null, int? pageSize = null)
    {
        var query = new List<string>();
        if (page.HasValue) query.Add($"page={page.Value}");
        if (pageSize.HasValue) query.Add($"pageSize={pageSize.Value}");
        var url = $"/egg-ledger-api/room/{roomCode}/activity" + (query.Count > 0 ? $"?{string.Join("&", query)}" : "");

        var request = new HttpRequestMessage(HttpMethod.Get, url).WithAuth(accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ActivityEvent>>())!;
    }

    [Fact]
    public async Task GetActivity_ForNonMember_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var outsider = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/activity").WithAuth(outsider.AccessToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetActivity_IncludesStockConsumeAndJoinEvents()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        await JoinRoomAsync(client, joiner.AccessToken, roomCode);
        await StockAsync(client, owner.AccessToken, roomCode, 12, 6.00m);
        await ConsumeAsync(client, joiner.AccessToken, roomCode, 4);

        var events = await GetActivityAsync(client, owner.AccessToken, roomCode);

        Assert.Contains(events, e => e.EventType == 1); // Stock
        Assert.Contains(events, e => e.EventType == 2 && e.Quantity == 4); // Consume
        Assert.Contains(events, e => e.EventType == 4); // MemberJoined (the room creator counts too)
    }

    [Fact]
    public async Task GetActivity_IncludesSettlementEvent()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);
        await StockAsync(client, owner.AccessToken, roomCode, 10, 5.00m);
        await ConsumeAsync(client, joiner.AccessToken, roomCode, 4);

        var settleRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/ledger/settle").WithAuth(owner.AccessToken);
        settleRequest.Content = JsonContent.Create(new { payerId = joiner.UserId, amount = 2.00m });
        await client.SendAsync(settleRequest);

        var events = await GetActivityAsync(client, owner.AccessToken, roomCode);

        var settlement = Assert.Single(events, e => e.EventType == 3);
        Assert.Equal(2.00m, settlement.Amount);
    }

    [Fact]
    public async Task GetActivity_IsSortedNewestFirstAcrossEventTypes()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        await StockAsync(client, owner.AccessToken, roomCode, 10, 5.00m);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);
        await ConsumeAsync(client, joiner.AccessToken, roomCode, 3);

        var events = await GetActivityAsync(client, owner.AccessToken, roomCode);

        Assert.Equal(events.OrderByDescending(e => e.Timestamp).Select(e => e.Timestamp), events.Select(e => e.Timestamp));
    }

    [Fact]
    public async Task GetActivity_DoesNotLeakEventsFromOtherRooms()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomA = await CreateOpenRoomAsync(client, owner.AccessToken);
        var roomB = await CreateOpenRoomAsync(client, owner.AccessToken);

        await StockAsync(client, owner.AccessToken, roomA, 10, 5.00m);
        await ConsumeAsync(client, owner.AccessToken, roomA, 2);

        var eventsB = await GetActivityAsync(client, owner.AccessToken, roomB);

        // Room B only has the creator's own MemberJoined event - no stock/consume from Room A.
        Assert.All(eventsB, e => Assert.NotEqual(1, e.EventType));
        Assert.All(eventsB, e => Assert.NotEqual(2, e.EventType));
    }

    [Fact]
    public async Task GetActivity_RespectsPageSize()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        await StockAsync(client, owner.AccessToken, roomCode, 50, 25.00m);
        for (var i = 0; i < 5; i++)
        {
            await ConsumeAsync(client, owner.AccessToken, roomCode, 1);
        }

        var firstPage = await GetActivityAsync(client, owner.AccessToken, roomCode, page: 1, pageSize: 3);

        Assert.Equal(3, firstPage.Count);
    }
}

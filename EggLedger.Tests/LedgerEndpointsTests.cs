using System.Net;
using System.Net.Http.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers the ledger/settlement flow: computing who-owes-whom from consumption history,
/// recording settlements, and the authorization rules around both.
/// </summary>
public class LedgerEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public LedgerEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private sealed class LedgerEntry
    {
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class UserBalance
    {
        public Guid UserId { get; set; }
        public decimal NetBalance { get; set; }
    }

    private sealed class RoomLedger
    {
        public List<UserBalance> Balances { get; set; } = [];
        public List<LedgerEntry> PairwiseDebts { get; set; } = [];
        public List<LedgerEntry> SuggestedSettlements { get; set; } = [];
    }

    private static async Task<int> CreateOpenRoomAsync(HttpClient client, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/room/create/").WithAuth(accessToken);
        request.Content = JsonContent.Create(new { roomName = $"Room {Guid.NewGuid():N}", isOpen = true });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
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

    private static async Task<RoomLedger> GetLedgerAsync(HttpClient client, string accessToken, int roomCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/ledger").WithAuth(accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoomLedger>())!;
    }

    [Fact]
    public async Task GetLedger_OneStocksOtherConsumes_ShowsConsumerOwesBuyer()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var consumer = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, buyer.AccessToken);
        await JoinRoomAsync(client, consumer.AccessToken, roomCode);

        // 12 eggs for 6.00 => 0.50 per egg. Consumer eats 5 => owes 2.50.
        await StockAsync(client, buyer.AccessToken, roomCode, 12, 6.00m);
        await ConsumeAsync(client, consumer.AccessToken, roomCode, 5);

        var ledger = await GetLedgerAsync(client, buyer.AccessToken, roomCode);

        var debt = Assert.Single(ledger.PairwiseDebts);
        Assert.Equal(consumer.UserId, debt.FromUserId);
        Assert.Equal(buyer.UserId, debt.ToUserId);
        Assert.Equal(2.50m, debt.Amount);

        var suggestion = Assert.Single(ledger.SuggestedSettlements);
        Assert.Equal(consumer.UserId, suggestion.FromUserId);
        Assert.Equal(buyer.UserId, suggestion.ToUserId);
        Assert.Equal(2.50m, suggestion.Amount);

        Assert.Equal(2.50m, ledger.Balances.Single(b => b.UserId == buyer.UserId).NetBalance);
        Assert.Equal(-2.50m, ledger.Balances.Single(b => b.UserId == consumer.UserId).NetBalance);
    }

    [Fact]
    public async Task RecordSettlement_ClearsTheDebt()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var consumer = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, buyer.AccessToken);
        await JoinRoomAsync(client, consumer.AccessToken, roomCode);

        await StockAsync(client, buyer.AccessToken, roomCode, 12, 6.00m);
        await ConsumeAsync(client, consumer.AccessToken, roomCode, 5);

        // The buyer (receiver of the money) confirms they were paid.
        var settleRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/ledger/settle")
            .WithAuth(buyer.AccessToken);
        settleRequest.Content = JsonContent.Create(new { payerId = consumer.UserId, amount = 2.50m, note = "Cash" });
        var settleResponse = await client.SendAsync(settleRequest);
        Assert.Equal(HttpStatusCode.OK, settleResponse.StatusCode);

        var ledger = await GetLedgerAsync(client, buyer.AccessToken, roomCode);
        Assert.Empty(ledger.PairwiseDebts);
        Assert.Empty(ledger.SuggestedSettlements);
        Assert.All(ledger.Balances, b => Assert.Equal(0m, b.NetBalance));
    }

    [Fact]
    public async Task RecordSettlement_WithSelfAsPayer_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, buyer.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/ledger/settle")
            .WithAuth(buyer.AccessToken);
        request.Content = JsonContent.Create(new { payerId = buyer.UserId, amount = 1.00m });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordSettlement_WithPayerNotInRoom_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var outsider = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, buyer.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/ledger/settle")
            .WithAuth(buyer.AccessToken);
        request.Content = JsonContent.Create(new { payerId = outsider.UserId, amount = 1.00m });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSettlement_ByNonReceiver_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var consumer = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, buyer.AccessToken);
        await JoinRoomAsync(client, consumer.AccessToken, roomCode);

        var settleRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/ledger/settle")
            .WithAuth(buyer.AccessToken);
        settleRequest.Content = JsonContent.Create(new { payerId = consumer.UserId, amount = 1.00m });
        var settleResponse = await client.SendAsync(settleRequest);
        var settlement = await settleResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var settlementId = settlement.GetProperty("settlementId").GetGuid();

        // The payer did not record it, so the payer cannot undo it either.
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/egg-ledger-api/room/{roomCode}/ledger/settle/{settlementId}")
            .WithAuth(consumer.AccessToken);
        var deleteResponse = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetLedger_ForNonMember_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var outsider = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, buyer.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/ledger").WithAuth(outsider.AccessToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLedger_DoesNotLeakDebtsFromOtherRooms()
    {
        var client = _factory.CreateClient();
        var buyer = await EggLedgerTestHelpers.RegisterAsync(client);
        var consumer = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomA = await CreateOpenRoomAsync(client, buyer.AccessToken);
        var roomB = await CreateOpenRoomAsync(client, buyer.AccessToken);
        await JoinRoomAsync(client, consumer.AccessToken, roomA);
        await JoinRoomAsync(client, consumer.AccessToken, roomB);

        // Debt only created in Room A.
        await StockAsync(client, buyer.AccessToken, roomA, 10, 5.00m);
        await ConsumeAsync(client, consumer.AccessToken, roomA, 4);

        var ledgerB = await GetLedgerAsync(client, buyer.AccessToken, roomB);

        Assert.Empty(ledgerB.PairwiseDebts);
        Assert.Empty(ledgerB.SuggestedSettlements);
    }

    [Fact]
    public async Task GetLedger_ThreeWayDebtCycle_SimplifiesToZeroSuggestedSettlements()
    {
        // A closed cycle - B owes A, C owes B, A owes C, all for the same amount - nets to a
        // zero balance for everyone even though there are 3 distinct pairwise debts. This is the
        // clearest possible proof that the suggested-settlements list is genuinely simplified
        // rather than just echoing the pairwise debts.
        var client = _factory.CreateClient();
        var a = await EggLedgerTestHelpers.RegisterAsync(client);
        var b = await EggLedgerTestHelpers.RegisterAsync(client);
        var c = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, a.AccessToken);
        await JoinRoomAsync(client, b.AccessToken, roomCode);
        await JoinRoomAsync(client, c.AccessToken, roomCode);

        // Each stocks a container FIFO will drain in purchase order: A's, then B's, then C's.
        await StockAsync(client, a.AccessToken, roomCode, 10, 5.00m); // 0.50/egg
        await StockAsync(client, b.AccessToken, roomCode, 10, 5.00m); // 0.50/egg
        await StockAsync(client, c.AccessToken, roomCode, 10, 5.00m); // 0.50/egg

        await ConsumeAsync(client, b.AccessToken, roomCode, 10); // drains A's container -> B owes A 5.00
        await ConsumeAsync(client, c.AccessToken, roomCode, 10); // drains B's container -> C owes B 5.00
        await ConsumeAsync(client, a.AccessToken, roomCode, 10); // drains C's container -> A owes C 5.00

        var ledger = await GetLedgerAsync(client, a.AccessToken, roomCode);

        Assert.Equal(3, ledger.PairwiseDebts.Count);
        Assert.Empty(ledger.SuggestedSettlements);
        Assert.All(ledger.Balances, balance => Assert.Equal(0m, balance.NetBalance));
    }
}

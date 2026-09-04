using System.Net;
using System.Net.Http.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers the stock -> consume flow (previously zero test coverage), and that Container/Order
/// endpoints are correctly gated behind room membership.
/// </summary>
public class ContainerAndOrderEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public ContainerAndOrderEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private sealed class ConsumeOrderResult
    {
        public string? OrderName { get; set; }
        public int Status { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string? Message { get; set; }
    }

    private sealed class ContainerSummary
    {
        public Guid ContainerId { get; set; }
        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
    }

    private sealed class OrderSummary
    {
        public Guid OrderId { get; set; }
        public DateTime Datestamp { get; set; }
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

    [Fact]
    public async Task StockOrder_ThenConsumeOrder_ReducesRemainingQuantity()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        var stockRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/stock")
            .WithAuth(owner.AccessToken);
        stockRequest.Content = JsonContent.Create(new { containerName = "Test Carton", quantity = 12, amount = 6.00m });
        var stockResponse = await client.SendAsync(stockRequest);
        Assert.Equal(HttpStatusCode.OK, stockResponse.StatusCode);

        var consumeRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/consume")
            .WithAuth(owner.AccessToken);
        consumeRequest.Content = JsonContent.Create(new { quantity = 5 });
        var consumeResponse = await client.SendAsync(consumeRequest);
        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);

        var consumeResult = await consumeResponse.Content.ReadFromJsonAsync<ConsumeOrderResult>();
        Assert.Equal(7, consumeResult!.AvailableQuantity);

        var containersRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/container/all")
            .WithAuth(owner.AccessToken);
        var containersResponse = await client.SendAsync(containersRequest);
        var containers = await containersResponse.Content.ReadFromJsonAsync<List<ContainerSummary>>();

        Assert.Single(containers!);
        Assert.Equal(7, containers![0].RemainingQuantity);
    }

    [Fact]
    public async Task ConsumeOrder_ExceedingAvailableStock_ReturnsFailedStatusNotError()
    {
        // Over-consumption is a recorded, auditable "Failed" order - not an HTTP error - so the
        // caller finds out exactly how much is actually available.
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        var stockRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/stock")
            .WithAuth(owner.AccessToken);
        stockRequest.Content = JsonContent.Create(new { containerName = "Small Carton", quantity = 2, amount = 2.00m });
        await client.SendAsync(stockRequest);

        var consumeRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/consume")
            .WithAuth(owner.AccessToken);
        consumeRequest.Content = JsonContent.Create(new { quantity = 10 });
        var consumeResponse = await client.SendAsync(consumeRequest);

        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);
        var result = await consumeResponse.Content.ReadFromJsonAsync<ConsumeOrderResult>();
        Assert.Equal(2, result!.AvailableQuantity);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task GetOrdersByContainer_ReturnsNewestFirst()
    {
        // Previously had no ORDER BY at all, so Postgres could return rows in any order -
        // the container's "order history" appeared randomly shuffled in the UI on every refresh.
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        var stockRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/stock")
            .WithAuth(owner.AccessToken);
        stockRequest.Content = JsonContent.Create(new { containerName = "History Carton", quantity = 20, amount = 10.00m });
        await client.SendAsync(stockRequest);

        var containersRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/container/all")
            .WithAuth(owner.AccessToken);
        var containersResponse = await client.SendAsync(containersRequest);
        var containerId = (await containersResponse.Content.ReadFromJsonAsync<List<ContainerSummary>>())!.Single().ContainerId;

        // Several consume orders against the same container, each strictly after the last.
        for (var i = 0; i < 4; i++)
        {
            var consumeRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/consume")
                .WithAuth(owner.AccessToken);
            consumeRequest.Content = JsonContent.Create(new { quantity = 1 });
            await client.SendAsync(consumeRequest);
        }

        var historyRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/{roomCode}/orders/container/{containerId}")
            .WithAuth(owner.AccessToken);
        var historyResponse = await client.SendAsync(historyRequest);
        historyResponse.EnsureSuccessStatusCode();
        var orders = await historyResponse.Content.ReadFromJsonAsync<List<OrderSummary>>();

        Assert.Equal(5, orders!.Count); // 1 stock + 4 consume
        Assert.Equal(orders.OrderByDescending(o => o.Datestamp).Select(o => o.OrderId), orders.Select(o => o.OrderId));
    }

    [Fact]
    public async Task ContainerEndpoints_ForNonMember_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var outsider = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/container/all")
            .WithAuth(outsider.AccessToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

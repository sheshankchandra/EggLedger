using System.Net.Http.Json;
using System.Text.Json;
using EggLedger.Data;
using EggLedger.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EggLedger.Tests;

/// <summary>
/// Covers the multi-resource groundwork migration: a seeded ResourceType row that every
/// container links to. This is currently invisible at the API surface (no DTO exposes it
/// yet), so these tests assert directly against the database rather than an HTTP response.
/// </summary>
public class ResourceTypeGroundworkTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public ResourceTypeGroundworkTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private async Task<int> CreateOpenRoomAsync(HttpClient client, string accessToken)
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

    [Fact]
    public async Task Migration_SeedsExactlyOneEggsResourceType()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var resourceType = Assert.Single(await context.ResourceTypes.AsNoTracking().ToListAsync());

        Assert.Equal(ResourceType.EggsId, resourceType.ResourceTypeId);
        Assert.Equal("eggs", resourceType.Name);
        Assert.Equal("Eggs", resourceType.DisplayName);
        Assert.Equal("egg", resourceType.Singular);
        Assert.Equal("eggs", resourceType.Plural);
        Assert.Equal("batch", resourceType.InventorySingular);
        Assert.Equal("batches", resourceType.InventoryPlural);
        Assert.True(resourceType.IsActive);
    }

    [Fact]
    public async Task StockedContainer_IsLinkedToEggsResourceType()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, owner.AccessToken);

        var stockRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/{roomCode}/orders/stock").WithAuth(owner.AccessToken);
        stockRequest.Content = JsonContent.Create(new { containerName = "Test Carton", quantity = 12, amount = 6.00m });
        var stockResponse = await client.SendAsync(stockRequest);
        stockResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var container = await context.Containers.AsNoTracking().SingleAsync(c => c.RoomId == context.Rooms.First(r => r.RoomCode == roomCode).RoomId);

        Assert.Equal(ResourceType.EggsId, container.ResourceTypeId);
    }
}

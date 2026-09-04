using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers room creation and the join-by-code flow, including the fix for a bug where Private
/// rooms could never be joined at all (JoinRoomAsync rejected any room with IsPublic == false,
/// even with the correct code) - Private should only mean "not discoverable", not "unjoinable".
/// </summary>
public class RoomEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public RoomEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private static async Task<int> CreateRoomAsync(HttpClient client, string accessToken, bool isOpen)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/room/create/")
            .WithAuth(accessToken);
        request.Content = JsonContent.Create(new { roomName = $"Room {Guid.NewGuid():N}", isOpen });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, "value", StringComparison.OrdinalIgnoreCase))
            .Value.GetInt32();
    }

    [Fact]
    public async Task CreateRoom_ThenJoinAsPrivateRoom_Succeeds()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);

        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);

        var joinRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/join/{roomCode}")
            .WithAuth(joiner.AccessToken);
        var joinResponse = await client.SendAsync(joinRequest);

        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_ThenJoinAsOpenRoom_Succeeds()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);

        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: true);

        var joinRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/join/{roomCode}")
            .WithAuth(joiner.AccessToken);
        var joinResponse = await client.SendAsync(joinRequest);

        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_AlreadyAMember_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: true);

        // The owner is already in the room from creation - joining again must fail.
        var joinRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/join/{roomCode}")
            .WithAuth(owner.AccessToken);
        var joinResponse = await client.SendAsync(joinRequest);

        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_WithUnknownCode_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var joinRequest = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/room/join/999999")
            .WithAuth(user.AccessToken);
        var joinResponse = await client.SendAsync(joinRequest);

        Assert.Equal(HttpStatusCode.BadRequest, joinResponse.StatusCode);
    }
}

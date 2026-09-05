using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers the Private-room join approval workflow: Public rooms join instantly, Private rooms
/// create a Pending request that has zero room access until a room admin approves it.
/// </summary>
public class RoomApprovalWorkflowTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public RoomApprovalWorkflowTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private sealed class JoinResult
    {
        public bool IsSuccess { get; set; }
        public JoinValue? Value { get; set; }
    }

    private sealed class JoinValue
    {
        public int RoomCode { get; set; }
        public bool IsPending { get; set; }
    }

    private sealed class RoomSummary
    {
        public int? RoomCode { get; set; }
        public bool IsPending { get; set; }
    }

    private static async Task<int> CreateRoomAsync(HttpClient client, string accessToken, bool isOpen)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/room/create/").WithAuth(accessToken);
        request.Content = JsonContent.Create(new { roomName = $"Room {Guid.NewGuid():N}", isOpen });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, "value", StringComparison.OrdinalIgnoreCase))
            .Value.GetInt32();
    }

    private static async Task<JoinResult> JoinRoomAsync(HttpClient client, string accessToken, int roomCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/join/{roomCode}").WithAuth(accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JoinResult>())!;
    }

    [Fact]
    public async Task JoinPrivateRoom_CreatesPendingRequest_WithNoRoomAccessYet()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);

        var join = await JoinRoomAsync(client, joiner.AccessToken, roomCode);
        Assert.True(join.Value!.IsPending);

        // Still blocked from room-scoped endpoints until approved.
        var containersRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/container/all")
            .WithAuth(joiner.AccessToken);
        var containersResponse = await client.SendAsync(containersRequest);
        Assert.Equal(HttpStatusCode.Forbidden, containersResponse.StatusCode);
    }

    [Fact]
    public async Task JoinPublicRoom_IsApprovedImmediately()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: true);

        var join = await JoinRoomAsync(client, joiner.AccessToken, roomCode);
        Assert.False(join.Value!.IsPending);

        var containersRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/container/all")
            .WithAuth(joiner.AccessToken);
        var containersResponse = await client.SendAsync(containersRequest);
        Assert.Equal(HttpStatusCode.OK, containersResponse.StatusCode);
    }

    [Fact]
    public async Task AdminApprovesPendingRequest_GrantsRoomAccess()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);

        var approveRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/approve-member/{joiner.UserId}")
            .WithAuth(owner.AccessToken);
        var approveResponse = await client.SendAsync(approveRequest);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var containersRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/container/all")
            .WithAuth(joiner.AccessToken);
        var containersResponse = await client.SendAsync(containersRequest);
        Assert.Equal(HttpStatusCode.OK, containersResponse.StatusCode);
    }

    [Fact]
    public async Task AdminRejectsPendingRequest_RemovesRowAndAllowsRejoinRequest()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);

        var rejectRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/reject-member/{joiner.UserId}")
            .WithAuth(owner.AccessToken);
        var rejectResponse = await client.SendAsync(rejectRequest);
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        // The unique (UserId, RoomId) index would reject a second insert if the row weren't
        // actually deleted - requesting again must succeed cleanly.
        var secondJoin = await JoinRoomAsync(client, joiner.AccessToken, roomCode);
        Assert.True(secondJoin.Value!.IsPending);
    }

    [Fact]
    public async Task NonAdmin_CannotApproveOrViewPendingMembers()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var outsider = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);

        var pendingRequest = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}/pending-members")
            .WithAuth(outsider.AccessToken);
        var pendingResponse = await client.SendAsync(pendingRequest);
        Assert.Equal(HttpStatusCode.Forbidden, pendingResponse.StatusCode);

        var approveRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/{roomCode}/approve-member/{joiner.UserId}")
            .WithAuth(outsider.AccessToken);
        var approveResponse = await client.SendAsync(approveRequest);
        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
    }

    [Fact]
    public async Task JoinPrivateRoomTwice_SecondRequestReturnsConflict()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);

        var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"/egg-ledger-api/room/join/{roomCode}").WithAuth(joiner.AccessToken);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetAllUserRooms_MarksPendingMembershipDistinctly()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);

        var request = new HttpRequestMessage(HttpMethod.Get, "/egg-ledger-api/room/user/all").WithAuth(joiner.AccessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomSummary>>();

        var joinedRoom = Assert.Single(rooms!, r => r.RoomCode == roomCode);
        Assert.True(joinedRoom.IsPending);
    }

    [Fact]
    public async Task PendingMember_DoesNotCountTowardMemberCount()
    {
        var client = _factory.CreateClient();
        var owner = await EggLedgerTestHelpers.RegisterAsync(client);
        var joiner = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateRoomAsync(client, owner.AccessToken, isOpen: false);
        await JoinRoomAsync(client, joiner.AccessToken, roomCode);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/room/{roomCode}").WithAuth(owner.AccessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var memberCount = doc.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, "memberCount", StringComparison.OrdinalIgnoreCase))
            .Value.GetInt32();

        Assert.Equal(1, memberCount); // only the owner - the pending joiner doesn't count yet
    }
}

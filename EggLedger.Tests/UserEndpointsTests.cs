using System.Net;
using System.Net.Http.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers the UserController authorization fixes: previously every one of these endpoints was
/// completely unauthenticated, and registration accepted a client-supplied Role.
/// </summary>
public class UserEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public UserEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAllUsers_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/egg-ledger-api/user/all");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_AsRegularUser_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/egg-ledger-api/user/all").WithAuth(user.AccessToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_AsAnotherUser_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var userA = await EggLedgerTestHelpers.RegisterAsync(client);
        var userB = await EggLedgerTestHelpers.RegisterAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/user/{userB.UserId}").WithAuth(userA.AccessToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_AsSelf_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/egg-ledger-api/user/{user.UserId}").WithAuth(user.AccessToken);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_SelfWithRoleField_AsRegularUser_ReturnsForbidden()
    {
        // A regular user must not be able to promote themselves by including a Role in their own update.
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/egg-ledger-api/user/{user.UserId}")
            .WithAuth(user.AccessToken);
        request.Content = JsonContent.Create(new { role = 1 });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithRoleFieldInBody_IsIgnored_UserStaysRegularRole()
    {
        // Registration no longer accepts a Role at all (UserCreateDto has no Role property), so
        // an attacker-supplied "role": 1 in the request body must have zero effect.
        var client = _factory.CreateClient();

        var user = await EggLedgerTestHelpers.RegisterAsync(client, extraFields: new { role = 1 });

        var request = new HttpRequestMessage(HttpMethod.Get, "/egg-ledger-api/user/profile").WithAuth(user.AccessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var role = doc.RootElement.EnumerateObject()
            .First(p => string.Equals(p.Name, "role", StringComparison.OrdinalIgnoreCase))
            .Value.GetInt32();

        Assert.Equal(0, role); // UserRoles.User
    }
}

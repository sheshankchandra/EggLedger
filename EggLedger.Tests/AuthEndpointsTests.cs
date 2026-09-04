using System.Net;
using System.Net.Http.Json;

namespace EggLedger.Tests;

public class AuthEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private const string CsrfHeader = "X-EggLedger-CSRF";
    private readonly EggLedgerWebApplicationFactory _factory;

    public AuthEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private static object NewUser(out string email, out string password)
    {
        email = $"user_{Guid.NewGuid():N}@example.com";
        password = "Password123!";
        return new { firstName = "Test", lastName = "User", email, password };
    }

    private static string? RefreshCookieValue(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;
        var cookie = cookies.FirstOrDefault(c => c.StartsWith("eggledger_refresh_token=", StringComparison.Ordinal));
        return cookie?.Split(';')[0]["eggledger_refresh_token=".Length..];
    }

    private sealed class TokenResponse
    {
        public string? AccessToken { get; set; }
    }

    [Fact]
    public async Task Register_Returns200_WithAccessToken_AndHttpOnlyRefreshCookie()
    {
        var client = _factory.CreateClient();
        var body = NewUser(out _, out _);

        var response = await client.PostAsJsonAsync("/egg-ledger-api/auth/register", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));

        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        Assert.Contains("eggledger_refresh_token=", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_AfterRegister_Returns200_WithAccessToken()
    {
        var client = _factory.CreateClient();
        var body = NewUser(out var email, out var password);
        await client.PostAsJsonAsync("/egg-ledger-api/auth/register", body);

        var response = await client.PostAsJsonAsync("/egg-ledger-api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));
    }

    [Fact]
    public async Task Refresh_WithCookieAndCsrfHeader_Returns200_AndRotatesCookie()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/egg-ledger-api/auth/register", NewUser(out _, out _));
        var originalCookie = RefreshCookieValue(registerResponse);
        Assert.False(string.IsNullOrEmpty(originalCookie));

        var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/auth/refresh");
        request.Headers.Add(CsrfHeader, "1");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));

        // Rotation: the refresh must hand back a different refresh token.
        var rotatedCookie = RefreshCookieValue(response);
        Assert.False(string.IsNullOrEmpty(rotatedCookie));
        Assert.NotEqual(originalCookie, rotatedCookie);
    }

    [Fact]
    public async Task Refresh_WithoutCsrfHeader_Returns403()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/egg-ledger-api/auth/register", NewUser(out _, out _));

        // Cookie is present (registered), but no CSRF header -> blocked before anything else.
        var response = await client.PostAsync("/egg-ledger-api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithCsrfHeaderButNoCookie_Returns401()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/auth/refresh");
        request.Headers.Add(CsrfHeader, "1");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

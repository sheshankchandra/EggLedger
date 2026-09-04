using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EggLedger.Tests;

/// <summary>
/// Shared helpers for integration tests that need an authenticated user. Each helper hits the
/// real endpoints (register/profile) rather than minting tokens directly, so tests exercise the
/// same code paths a real client would.
/// </summary>
internal static class EggLedgerTestHelpers
{
    private sealed class TokenResponse
    {
        public string? AccessToken { get; set; }
    }

    private sealed class ProfileResponse
    {
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Role { get; set; }
    }

    public sealed record TestUser(string AccessToken, Guid UserId, string Email);

    /// <summary>Registers a brand-new user and returns their access token and id.</summary>
    public static async Task<TestUser> RegisterAsync(HttpClient client, object? extraFields = null)
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var body = MergeFields(new { firstName = "Test", lastName = "User", email, password = "Password123!" }, extraFields);

        var registerResponse = await client.PostAsJsonAsync("/egg-ledger-api/auth/register", body);
        registerResponse.EnsureSuccessStatusCode();
        var token = await registerResponse.Content.ReadFromJsonAsync<TokenResponse>();

        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "/egg-ledger-api/user/profile");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var profileResponse = await client.SendAsync(profileRequest);
        profileResponse.EnsureSuccessStatusCode();
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();

        return new TestUser(token.AccessToken!, profile!.UserId, email);
    }

    /// <summary>Builds a request with a Bearer token attached.</summary>
    public static HttpRequestMessage WithAuth(this HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static object MergeFields(object baseFields, object? extraFields)
    {
        if (extraFields == null)
            return baseFields;

        var merged = new Dictionary<string, object?>();
        foreach (var prop in baseFields.GetType().GetProperties())
            merged[prop.Name] = prop.GetValue(baseFields);
        foreach (var prop in extraFields.GetType().GetProperties())
            merged[prop.Name] = prop.GetValue(extraFields);
        return merged;
    }
}

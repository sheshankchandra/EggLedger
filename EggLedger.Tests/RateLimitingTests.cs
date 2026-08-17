using System.Net;
using System.Net.Http.Json;

namespace EggLedger.Tests;

public class RateLimitingTests : IClassFixture<RateLimitedWebApplicationFactory>
{
    private const string CsrfHeader = "X-EggLedger-CSRF";
    private readonly RateLimitedWebApplicationFactory _factory;

    public RateLimitingTests(RateLimitedWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ExceedingAuthLimit_Returns429()
    {
        // The factory sets the auth permit limit to 3, so the 4th+ attempt should be throttled.
        var client = _factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 6; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/egg-ledger-api/auth/login")
            {
                Content = JsonContent.Create(new { email = "nobody@example.com", password = "wrong" })
            };
            request.Headers.Add(CsrfHeader, "1");
            var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        // The final attempts must be throttled, not served.
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}

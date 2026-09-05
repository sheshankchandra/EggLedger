using System.Net;
using System.Net.Http.Json;

namespace EggLedger.Tests;

/// <summary>
/// Covers the gamified stats endpoint: totals/protein/calorie math, range selection, and that
/// stats aggregate across every room a user is in (streaks are a personal habit, not room-scoped).
/// </summary>
public class StatsEndpointsTests : IClassFixture<EggLedgerWebApplicationFactory>
{
    private readonly EggLedgerWebApplicationFactory _factory;

    public StatsEndpointsTests(EggLedgerWebApplicationFactory factory) => _factory = factory;

    private sealed class UserStats
    {
        public int TotalEggsConsumed { get; set; }
        public decimal TotalProteinGrams { get; set; }
        public decimal TotalCalories { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public List<StatsBucket> Buckets { get; set; } = [];
    }

    private sealed class StatsBucket
    {
        public string Label { get; set; } = "";
        public int EggsConsumed { get; set; }
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

    private static async Task<UserStats> GetStatsAsync(HttpClient client, string accessToken, string? range = null)
    {
        var url = range == null ? "/egg-ledger-api/user/stats" : $"/egg-ledger-api/user/stats?range={range}";
        var request = new HttpRequestMessage(HttpMethod.Get, url).WithAuth(accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserStats>())!;
    }

    [Fact]
    public async Task GetStats_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/egg-ledger-api/user/stats");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_NoConsumptionEver_ReturnsAllZeros()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var stats = await GetStatsAsync(client, user.AccessToken);

        Assert.Equal(0, stats.TotalEggsConsumed);
        Assert.Equal(0m, stats.TotalProteinGrams);
        Assert.Equal(0m, stats.TotalCalories);
        Assert.Equal(0, stats.CurrentStreakDays);
        Assert.Equal(0, stats.LongestStreakDays);
    }

    [Fact]
    public async Task GetStats_DefaultRange_ReturnsSevenDailyBuckets()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var stats = await GetStatsAsync(client, user.AccessToken); // no ?range= at all

        Assert.Equal(7, stats.Buckets.Count);
    }

    [Theory]
    [InlineData("Week", 7)]
    [InlineData("Month", 30)]
    [InlineData("Year", 12)]
    public async Task GetStats_ExplicitRange_ReturnsExpectedBucketCount(string range, int expectedBuckets)
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var stats = await GetStatsAsync(client, user.AccessToken, range);

        Assert.Equal(expectedBuckets, stats.Buckets.Count);
    }

    [Fact]
    public async Task GetStats_MaxRangeWithNoHistory_ReturnsEmptyBuckets()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);

        var stats = await GetStatsAsync(client, user.AccessToken, "Max");

        Assert.Empty(stats.Buckets);
    }

    [Fact]
    public async Task GetStats_AfterConsuming_ComputesTotalsProteinAndCaloriesAndStreak()
    {
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomCode = await CreateOpenRoomAsync(client, user.AccessToken);

        await StockAsync(client, user.AccessToken, roomCode, 20, 10.00m);
        await ConsumeAsync(client, user.AccessToken, roomCode, 6);

        var stats = await GetStatsAsync(client, user.AccessToken, "Week");

        Assert.Equal(6, stats.TotalEggsConsumed);
        Assert.Equal(6 * 6.3m, stats.TotalProteinGrams); // default 6.3g protein per egg
        Assert.Equal(6 * 78m, stats.TotalCalories); // default 78 kcal per egg
        Assert.True(stats.CurrentStreakDays >= 1); // consumed today
        Assert.True(stats.LongestStreakDays >= 1);
        Assert.Equal(6, stats.Buckets.Sum(b => b.EggsConsumed));
    }

    [Fact]
    public async Task GetStats_AggregatesAcrossMultipleRooms()
    {
        // Streaks/stats are a personal habit, not a per-room stat - consuming in two different
        // rooms on the same day should add up in one combined total.
        var client = _factory.CreateClient();
        var user = await EggLedgerTestHelpers.RegisterAsync(client);
        var roomA = await CreateOpenRoomAsync(client, user.AccessToken);
        var roomB = await CreateOpenRoomAsync(client, user.AccessToken);

        await StockAsync(client, user.AccessToken, roomA, 10, 5.00m);
        await StockAsync(client, user.AccessToken, roomB, 10, 5.00m);
        await ConsumeAsync(client, user.AccessToken, roomA, 3);
        await ConsumeAsync(client, user.AccessToken, roomB, 4);

        var stats = await GetStatsAsync(client, user.AccessToken, "Week");

        Assert.Equal(7, stats.TotalEggsConsumed);
    }
}

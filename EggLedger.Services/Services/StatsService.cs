using System.Globalization;
using EggLedger.Data;
using EggLedger.DTO.Stats;
using EggLedger.Models.Enums;
using EggLedger.Models.Options;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EggLedger.Services.Services;

public class StatsService : IStatsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatsService> _logger;
    private readonly NutritionOptions _nutritionOptions;

    public StatsService(ApplicationDbContext context, IOptions<NutritionOptions> nutritionOptions, ILogger<StatsService> logger)
    {
        _context = context;
        _nutritionOptions = nutritionOptions.Value;
        _logger = logger;
    }

    public async Task<Result<UserStatsDto>> GetUserStatsAsync(Guid userId, StatsRange range, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId, cancellationToken);
            if (!userExists)
                return Result.Fail("User not found");

            var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            // Streaks are always computed from full history, independent of the requested range -
            // a "1 week" view shouldn't make someone's real streak look artificially short.
            var consumeDatesRaw = await _context.Orders
                .Where(o => o.UserId == userId && o.OrderType == OrderType.Consuming && o.OrderStatus == OrderStatus.Completed)
                .Select(o => o.Datestamp.Date)
                .Distinct()
                .ToListAsync(cancellationToken);
            var consumeDates = consumeDatesRaw.Select(d => DateTime.SpecifyKind(d, DateTimeKind.Utc)).ToHashSet();

            var (currentStreak, longestStreak) = ComputeStreaks(consumeDates, todayUtc);

            var buckets = range switch
            {
                // Week has room for a day name; Month packs 30 labels in, so date-only avoids
                // the labels overlapping each other on the chart.
                StatsRange.Week => BuildDailyBuckets(todayUtc, 7, "ddd d MMM"),
                StatsRange.Month => BuildDailyBuckets(todayUtc, 30, "d MMM"),
                StatsRange.Year => BuildMonthlyBuckets(todayUtc, 12),
                _ => BuildMaxBuckets(todayUtc, consumeDates),
            };

            var rangeStart = buckets.Count > 0 ? buckets[0].Start : todayUtc;
            var orders = await _context.Orders
                .Where(o => o.UserId == userId
                         && o.OrderType == OrderType.Consuming
                         && o.OrderStatus == OrderStatus.Completed
                         && o.Datestamp >= rangeStart)
                .Select(o => new { o.Datestamp, o.Quantity })
                .ToListAsync(cancellationToken);

            var bucketDtos = buckets.Select(bucket => new StatsBucketDto
            {
                BucketStart = bucket.Start,
                Label = bucket.Label,
                EggsConsumed = orders.Where(o => o.Datestamp >= bucket.Start && o.Datestamp < bucket.End).Sum(o => o.Quantity),
            }).ToList();

            var totalEggs = bucketDtos.Sum(b => b.EggsConsumed);

            return Result.Ok(new UserStatsDto
            {
                TotalEggsConsumed = totalEggs,
                TotalProteinGrams = Math.Round(totalEggs * _nutritionOptions.ProteinGramsPerUnit, 1),
                TotalCalories = Math.Round(totalEggs * _nutritionOptions.CaloriesPerUnit, 0),
                CurrentStreakDays = currentStreak,
                LongestStreakDays = longestStreak,
                Buckets = bucketDtos,
            });
        }, "An error occurred while computing user stats.");
    }

    private static (int Current, int Longest) ComputeStreaks(HashSet<DateTime> consumeDates, DateTime todayUtc)
    {
        if (consumeDates.Count == 0)
            return (0, 0);

        // Anchor "current streak" on today if already logged, otherwise yesterday - the streak
        // isn't broken just because today hasn't happened yet.
        var anchor = consumeDates.Contains(todayUtc) ? todayUtc : todayUtc.AddDays(-1);
        var current = 0;
        var cursor = anchor;
        while (consumeDates.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        var longest = 0;
        var run = 0;
        DateTime? previous = null;
        foreach (var date in consumeDates.OrderBy(d => d))
        {
            run = previous.HasValue && date == previous.Value.AddDays(1) ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = date;
        }

        return (current, Math.Max(longest, current));
    }

    private static List<(DateTime Start, DateTime End, string Label)> BuildDailyBuckets(DateTime todayUtc, int days, string labelFormat)
    {
        var buckets = new List<(DateTime, DateTime, string)>();
        for (var i = days - 1; i >= 0; i--)
        {
            var day = todayUtc.AddDays(-i);
            buckets.Add((day, day.AddDays(1), day.ToString(labelFormat, CultureInfo.InvariantCulture)));
        }
        return buckets;
    }

    private static List<(DateTime Start, DateTime End, string Label)> BuildMonthlyBuckets(DateTime todayUtc, int months)
    {
        var buckets = new List<(DateTime, DateTime, string)>();
        var firstOfThisMonth = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = months - 1; i >= 0; i--)
        {
            var monthStart = firstOfThisMonth.AddMonths(-i);
            buckets.Add((monthStart, monthStart.AddMonths(1), monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture)));
        }
        return buckets;
    }

    private static List<(DateTime Start, DateTime End, string Label)> BuildMaxBuckets(DateTime todayUtc, HashSet<DateTime> consumeDates)
    {
        if (consumeDates.Count == 0)
            return [];

        var earliest = consumeDates.Min();
        var monthsSpan = ((todayUtc.Year - earliest.Year) * 12) + todayUtc.Month - earliest.Month + 1;
        return BuildMonthlyBuckets(todayUtc, monthsSpan);
    }
}

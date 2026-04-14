namespace Kitsune7Den.Tests;

/// <summary>
/// Tests the math used by the Day/Night calculator in ConfigViewModel.
/// Mirrors KitsuneDen's resolveDayNightConfig / reverseDayNightConfig from
/// src/lib/day-night.ts — same inputs should produce the same outputs.
/// </summary>
public class DayNightCalculatorTests
{
    private const int MinCycle = 10;
    private const int MaxCycle = 240;
    private const int MinDaylight = 1;
    private const int MaxDaylight = 23;

    // Mirrors the private helper in ConfigViewModel.ApplyDayNightToRaw
    private static (int total, int daylightHours) Resolve(int dayMinutes, int nightMinutes)
    {
        var day = Math.Max(1, dayMinutes);
        var night = Math.Max(1, nightMinutes);
        var total = Math.Clamp(day + night, MinCycle, MaxCycle);
        var daylightHours = Math.Clamp(
            (int)Math.Round((day / (double)(day + night)) * 24),
            MinDaylight, MaxDaylight);
        return (total, daylightHours);
    }

    // Mirrors the private helper in ConfigViewModel.SeedCalculatorFromRaw
    private static (int dayMinutes, int nightMinutes) Reverse(int total, int daylightHours)
    {
        total = Math.Clamp(total, MinCycle, MaxCycle);
        daylightHours = Math.Clamp(daylightHours, MinDaylight, MaxDaylight);
        var day = (int)Math.Round((daylightHours / 24.0) * total);
        var night = total - day;
        if (day <= 0) { day = 1; night = total - 1; }
        if (night <= 0) { night = 1; day = total - 1; }
        return (day, night);
    }

    [Theory]
    [InlineData(45, 15, 60, 18)] // vanilla-ish: 45 day + 15 night = 60 min cycle, 18 hrs daylight
    [InlineData(30, 30, 60, 12)] // equinox
    [InlineData(50, 10, 60, 20)] // long day
    [InlineData(10, 50, 60, 4)]  // long night
    public void Resolve_ProducesExpectedRawValues(int day, int night, int expectedTotal, int expectedDaylight)
    {
        var (total, daylight) = Resolve(day, night);
        Assert.Equal(expectedTotal, total);
        Assert.Equal(expectedDaylight, daylight);
    }

    [Theory]
    [InlineData(60, 18, 45, 15)]
    [InlineData(60, 12, 30, 30)]
    [InlineData(120, 18, 90, 30)] // 2-hour cycle, 18 daylight hours
    public void Reverse_ProducesFriendlyInputs(int total, int daylightHours, int expectedDay, int expectedNight)
    {
        var (day, night) = Reverse(total, daylightHours);
        Assert.Equal(expectedDay, day);
        Assert.Equal(expectedNight, night);
    }

    [Theory]
    [InlineData(45, 15)]
    [InlineData(30, 30)]
    [InlineData(20, 40)]
    [InlineData(60, 60)]
    public void RoundTrip_PreservesInputsApproximately(int day, int night)
    {
        var (total, daylight) = Resolve(day, night);
        var (newDay, newNight) = Reverse(total, daylight);

        // The reverse may differ by up to ~ceil(total/24) minutes because of the
        // integer daylight-hours quantization step. Assert within that tolerance.
        var tolerance = Math.Max(2, total / 24 + 1);
        Assert.InRange(newDay, day - tolerance, day + tolerance);
        Assert.InRange(newNight, night - tolerance, night + tolerance);
        Assert.Equal(day + night, newDay + newNight); // total always preserved
    }

    [Fact]
    public void Resolve_ClampsExcessivelyLongCycles()
    {
        // 200 + 200 = 400, but MaxCycle is 240
        var (total, _) = Resolve(200, 200);
        Assert.Equal(MaxCycle, total);
    }

    [Fact]
    public void Resolve_AllowsMinimumOneMinute()
    {
        // 1 + 9 = 10 min cycle, 24 * (1/10) = 2.4 → round to 2 daylight hours
        var (total, daylight) = Resolve(1, 9);
        Assert.Equal(10, total);
        Assert.InRange(daylight, MinDaylight, MaxDaylight);
    }

    [Fact]
    public void Reverse_NeverReturnsZeroDayOrNight()
    {
        // Edge case: 10 min total, 1 hour daylight = 0.4 rounds to 0 day minutes
        // The safety net should bump it to 1.
        var (day, night) = Reverse(10, 1);
        Assert.True(day >= 1, "day should never be zero");
        Assert.True(night >= 1, "night should never be zero");
    }
}

using Kitsune7Den.Services;

namespace Kitsune7Den.Tests;

/// <summary>
/// Tests for the semver comparison logic used by the self-updater.
/// Pure logic, no network needed.
/// </summary>
public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.10.0", "1.9.0", 1)] // numeric compare, not lexicographic
    [InlineData("1.0.0", "1.0", 0)]    // missing patch treated as 0
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("1.0.2", "1.0.1", 1)]
    public void CompareVersions_ReturnsExpectedSign(string a, string b, int expectedSign)
    {
        var result = UpdateService.CompareVersions(a, b);
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Theory]
    [InlineData("v1.0.0", "1.0.0", 0)]  // leading v should be stripped by caller
    [InlineData("V1.2.3", "1.2.3", 0)]
    public void CompareVersions_HandlesVPrefix(string a, string b, int expectedSign)
    {
        // Note: CompareVersions itself doesn't strip 'v' — NormalizeVersion does,
        // which is called on the tag_name inside CheckForUpdateAsync.
        // Here we're asserting that raw numeric comparison still works when fed clean values.
        var result = UpdateService.CompareVersions(
            a.TrimStart('v', 'V'),
            b.TrimStart('v', 'V'));
        Assert.Equal(expectedSign, Math.Sign(result));
    }
}

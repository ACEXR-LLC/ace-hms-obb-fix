using System.Text.RegularExpressions;
using AceHeadsetFix.Adb;

namespace AceHeadsetFix.Headset;

public sealed record AcePackageInfo(bool Installed, int VersionCode, string VersionName, string AppUser)
{
    public static readonly AcePackageInfo NotInstalled = new(false, 0, string.Empty, string.Empty);
}

public static partial class AcePackageInspector
{
    public const string PackageName = "com.Ace.Ace";

    public static async Task<AcePackageInfo> InspectAsync(AdbRunner adb, string serial)
    {
        var result = await adb.ShellAsync(serial, $"dumpsys package {PackageName}", TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        return Parse(result.CombinedOutput);
    }

    public static AcePackageInfo Parse(string dumpsysOutput)
    {
        var versionCode = VersionCodePattern().Match(dumpsysOutput);
        var appId = AppIdPattern().Match(dumpsysOutput);
        if (!versionCode.Success || !appId.Success)
            return AcePackageInfo.NotInstalled;
        var versionName = VersionNamePattern().Match(dumpsysOutput);
        return new AcePackageInfo(
            true,
            int.Parse(versionCode.Groups[1].Value),
            versionName.Success ? versionName.Groups[1].Value : "?",
            AndroidUserName(int.Parse(appId.Groups[1].Value)));
    }

    static string AndroidUserName(int appId) => $"u0_a{appId - 10000}";

    [GeneratedRegex(@"versionCode=(\d+)")]
    private static partial Regex VersionCodePattern();

    [GeneratedRegex(@"versionName=(\S+)")]
    private static partial Regex VersionNamePattern();

    [GeneratedRegex(@"(?:appId|userId)=(\d+)")]
    private static partial Regex AppIdPattern();
}

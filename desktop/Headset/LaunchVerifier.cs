using AceHeadsetFix.Adb;

namespace AceHeadsetFix.Headset;

public enum LaunchOutcome
{
    ReachedVr,
    ContentUnreadable,
    Undetermined,
}

public sealed record LaunchReport(LaunchOutcome Outcome, string Detail);

public sealed class LaunchVerifier
{
    const string MainActivity = AcePackageInspector.PackageName + "/com.unity3d.player.UnityPlayerActivity";
    static readonly string[] VrReadyMarkers = ["xrBeginSession", "CompositorOpenXR::Initialize"];
    static readonly string[] ContentFailureMarkers = ["ApkAddCentralDirectory : Unable to open", "Error loading subsystem: Display"];
    static readonly TimeSpan QuickTimeout = TimeSpan.FromSeconds(60);
    static readonly TimeSpan WatchLimit = TimeSpan.FromSeconds(75);
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    readonly AdbRunner _adb;

    public LaunchVerifier(AdbRunner adb) => _adb = adb;

    public async Task<LaunchReport> LaunchAndWatchAsync(string serial)
    {
        await _adb.ShellAsync(serial, $"am force-stop {AcePackageInspector.PackageName}", QuickTimeout).ConfigureAwait(false);
        await _adb.ShellAsync(serial, "logcat -c", QuickTimeout).ConfigureAwait(false);
        var start = await _adb.ShellAsync(serial, $"am start -n {MainActivity}", QuickTimeout).ConfigureAwait(false);
        if (!start.Succeeded)
            return new LaunchReport(LaunchOutcome.Undetermined, "Couldn't start Ace: " + start.CombinedOutput);

        var deadline = DateTime.UtcNow + WatchLimit;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval, _adb.Cancellation).ConfigureAwait(false);
            var log = await _adb.ShellAsync(serial, "logcat -d -s Unity:* OVRPlugin:* OpenXR-Loader:*", QuickTimeout).ConfigureAwait(false);
            var outcome = Classify(log.StandardOutput);
            if (outcome != LaunchOutcome.Undetermined)
                return new LaunchReport(outcome, string.Empty);
        }
        return new LaunchReport(LaunchOutcome.Undetermined, "Ace started but didn't report either way within a minute.");
    }

    public static LaunchOutcome Classify(string logcat)
    {
        if (ContentFailureMarkers.Any(logcat.Contains))
            return LaunchOutcome.ContentUnreadable;
        if (VrReadyMarkers.Any(logcat.Contains))
            return LaunchOutcome.ReachedVr;
        return LaunchOutcome.Undetermined;
    }
}

using AceHeadsetFix.Adb;
using AceHeadsetFix.Reporting;

namespace AceHeadsetFix.Headset;

public sealed record RepairResult(bool Succeeded, string Message);

public sealed class ObbRepair
{
    const long FreeSpaceMargin = 256L * 1024 * 1024;
    static readonly TimeSpan QuickTimeout = TimeSpan.FromSeconds(60);
    static readonly TimeSpan LargeFileTimeout = TimeSpan.FromMinutes(20);

    readonly AdbRunner _adb;
    readonly IProgress<StatusLine> _status;

    public ObbRepair(AdbRunner adb, IProgress<StatusLine> status)
    {
        _adb = adb;
        _status = status;
    }

    public async Task<RepairResult> RepairAsync(string serial, ObbEntry contentFile)
    {
        var original = contentFile.Path;
        var copy = original + ".acefix";
        var retired = original + ".aceold";

        await _adb.ShellAsync(serial, $"am force-stop {AcePackageInspector.PackageName}", QuickTimeout).ConfigureAwait(false);
        await _adb.ShellAsync(serial, $"rm -f {copy} {retired}", QuickTimeout).ConfigureAwait(false);

        var freeBytes = await FreeBytesAsync(serial).ConfigureAwait(false);
        if (freeBytes < contentFile.Size + FreeSpaceMargin)
            return new RepairResult(false, $"The headset needs {contentFile.SizeText} of free space to make the fix and only has {freeBytes / 1_000_000_000d:F1} GB. Free up some space on the headset and try again.");

        _status.Report(new StatusLine(StepOutcome.Working, $"Making a copy of the content file that Ace can read ({contentFile.SizeText}). This takes about a minute…"));
        var copyResult = await _adb.ShellAsync(serial, $"cp {original} {copy} && chmod 666 {copy}", LargeFileTimeout).ConfigureAwait(false);
        if (!copyResult.Succeeded)
            return await AbandonAsync(serial, copy, $"Couldn't make the copy. The headset said: {copyResult.CombinedOutput}").ConfigureAwait(false);

        _status.Report(new StatusLine(StepOutcome.Working, "Checking the copy…"));
        var checksumResult = await _adb.ShellAsync(serial, $"md5sum {original} {copy}", LargeFileTimeout).ConfigureAwait(false);
        if (!checksumResult.Succeeded || !ChecksumsMatch(checksumResult.StandardOutput))
            return await AbandonAsync(serial, copy, $"The copy didn't match the original, so it was thrown away. The headset said: {checksumResult.CombinedOutput}").ConfigureAwait(false);

        _status.Report(new StatusLine(StepOutcome.Working, "Swapping the copy into place…"));
        var swapResult = await _adb.ShellAsync(serial, $"mv {original} {retired} && mv {copy} {original} && rm -f {retired}", TimeSpan.FromMinutes(5)).ConfigureAwait(false);
        if (!swapResult.Succeeded)
        {
            await _adb.ShellAsync(serial, $"[ -e {original} ] || mv {retired} {original}", QuickTimeout).ConfigureAwait(false);
            return await AbandonAsync(serial, copy, $"Couldn't swap the copy into place. The headset said: {swapResult.CombinedOutput}").ConfigureAwait(false);
        }

        var after = await ObbInspector.ListAsync(_adb, serial).ConfigureAwait(false);
        var replaced = after.FirstOrDefault(entry => entry.Path == original);
        if (replaced is null || !replaced.WorldReadable)
            return new RepairResult(false, "The swap finished but the content file still isn't readable. Please send the log file to Ace support.");
        return new RepairResult(true, $"The content file now belongs to '{replaced.Owner}' with permissions {replaced.Mode}, which Ace can read.");
    }

    async Task<RepairResult> AbandonAsync(string serial, string copy, string message)
    {
        await _adb.ShellAsync(serial, $"rm -f {copy}", QuickTimeout).ConfigureAwait(false);
        return new RepairResult(false, message);
    }

    async Task<long> FreeBytesAsync(string serial)
    {
        var result = await _adb.ShellAsync(serial, "df -k /sdcard", QuickTimeout).ConfigureAwait(false);
        return ParseAvailableBytes(result.StandardOutput);
    }

    public static long ParseAvailableBytes(string dfOutput)
    {
        var lastLine = dfOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? string.Empty;
        var columns = lastLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return columns.Length >= 4 && long.TryParse(columns[3], out var availableKilobytes) ? availableKilobytes * 1024 : long.MaxValue;
    }

    public static bool ChecksumsMatch(string md5sumOutput)
    {
        var checksums = md5sumOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
            .ToList();
        return checksums.Count == 2 && checksums[0] == checksums[1];
    }
}

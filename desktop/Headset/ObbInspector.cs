using AceHeadsetFix.Adb;

namespace AceHeadsetFix.Headset;

public sealed record ObbEntry(string Path, string Owner, string Group, string Mode, long Size)
{
    public string FileName => Path[(Path.LastIndexOf('/') + 1)..];

    public bool WorldReadable => Mode.Length > 0 && ((Mode[^1] - '0') & 4) != 0;

    public string SizeText => $"{Size / 1_000_000_000d:F1} GB";
}

public enum ContentFileState
{
    NotDownloaded,
    Readable,
    Unreadable,
}

public sealed record ContentFileReport(ContentFileState State, ObbEntry? ContentFile, IReadOnlyList<ObbEntry> Entries);

public static class ObbInspector
{
    public const string ObbDirectory = "/sdcard/Android/obb/" + AcePackageInspector.PackageName;

    public static string ExpectedFileName(int versionCode) => $"main.{versionCode}.{AcePackageInspector.PackageName}.obb";

    public static async Task<ContentFileReport> InspectAsync(AdbRunner adb, string serial, AcePackageInfo package)
    {
        var entries = await ListAsync(adb, serial).ConfigureAwait(false);
        var expectedName = ExpectedFileName(package.VersionCode);
        var contentFile = entries.FirstOrDefault(entry => entry.FileName == expectedName);
        if (contentFile is null)
            return new ContentFileReport(ContentFileState.NotDownloaded, null, entries);
        var readable = contentFile.Owner == package.AppUser || contentFile.WorldReadable;
        return new ContentFileReport(readable ? ContentFileState.Readable : ContentFileState.Unreadable, contentFile, entries);
    }

    public static async Task<IReadOnlyList<ObbEntry>> ListAsync(AdbRunner adb, string serial)
    {
        var result = await adb.ShellAsync(serial, $"stat -c '%n|%U|%G|%a|%s' {ObbDirectory}/*", TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        return Parse(result.StandardOutput);
    }

    public static IReadOnlyList<ObbEntry> Parse(string statOutput)
    {
        var entries = new List<ObbEntry>();
        foreach (var rawLine in statOutput.Split('\n'))
        {
            var fields = rawLine.Trim().Split('|');
            if (fields.Length == 5 && long.TryParse(fields[4], out var size))
                entries.Add(new ObbEntry(fields[0], fields[1], fields[2], fields[3], size));
        }
        return entries;
    }
}

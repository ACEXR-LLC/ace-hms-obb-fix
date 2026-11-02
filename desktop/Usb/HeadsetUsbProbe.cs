using System.Management;

namespace AceHeadsetFix.Usb;

public enum UsbHeadsetState
{
    Unknown,
    NotSeen,
    DebuggingOff,
    DriverProblem,
    DebuggingOn,
}

public sealed record UsbHeadsetReport(UsbHeadsetState State, int HeadsetCount, string Detail);

public static class HeadsetUsbProbe
{
    const string MetaDevicesQuery = @"SELECT DeviceID, Name, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB\\VID_2833%'";

    public static UsbHeadsetReport Probe()
    {
        try
        {
            return Classify(ReadEntries());
        }
        catch (Exception exception)
        {
            return new UsbHeadsetReport(UsbHeadsetState.Unknown, 0, exception.Message);
        }
    }

    public static UsbHeadsetReport Classify(IReadOnlyList<UsbEntry> entries)
    {
        var headsetCount = entries.Count(entry => !entry.IsInterface);
        var interfaces = entries.Where(entry => entry.IsInterface).ToList();
        if (headsetCount == 0 && interfaces.Count == 0)
            return new UsbHeadsetReport(UsbHeadsetState.NotSeen, 0, "Windows does not see a Meta headset on USB.");

        headsetCount = Math.Max(1, headsetCount);
        var names = string.Join("; ", interfaces.Select(entry => entry.Describe()));
        var adbInterfaces = interfaces.Where(entry => entry.LooksLikeAdb).ToList();
        if (adbInterfaces.Any(entry => entry.ErrorCode != 0))
            return new UsbHeadsetReport(UsbHeadsetState.DriverProblem, headsetCount, names);
        if (adbInterfaces.Count > 0)
            return new UsbHeadsetReport(UsbHeadsetState.DebuggingOn, headsetCount, names);
        if (interfaces.Count >= 4 * headsetCount)
        {
            var state = interfaces.Any(entry => entry.ErrorCode != 0) ? UsbHeadsetState.DriverProblem : UsbHeadsetState.DebuggingOn;
            return new UsbHeadsetReport(state, headsetCount, names);
        }
        return new UsbHeadsetReport(UsbHeadsetState.DebuggingOff, headsetCount, names);
    }

    static List<UsbEntry> ReadEntries()
    {
        using var searcher = new ManagementObjectSearcher(MetaDevicesQuery);
        using var results = searcher.Get();
        var entries = new List<UsbEntry>();
        foreach (var result in results)
        {
            using (result)
            {
                entries.Add(new UsbEntry(
                    result["DeviceID"]?.ToString() ?? string.Empty,
                    result["Name"]?.ToString() ?? string.Empty,
                    result["ConfigManagerErrorCode"] is uint code ? code : 0));
            }
        }
        return entries;
    }

    public sealed record UsbEntry(string DeviceId, string Name, uint ErrorCode)
    {
        public bool IsInterface => DeviceId.Contains("&MI_", StringComparison.OrdinalIgnoreCase);

        public bool LooksLikeAdb => Name.Contains("ADB", StringComparison.OrdinalIgnoreCase);

        public string Describe() => ErrorCode == 0 ? Name : $"{Name} (Windows error code {ErrorCode})";
    }
}

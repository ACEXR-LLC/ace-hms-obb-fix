namespace AceHeadsetFix.Adb;

public enum AdbDeviceState
{
    Ready,
    Unauthorized,
    Offline,
    Other,
}

public sealed record AdbDevice(string Serial, AdbDeviceState State, string Model)
{
    public string DisplayName => $"{Model} ({Serial})";
}

public static class DeviceListParser
{
    static readonly char[] ColumnSeparators = [' ', '\t'];

    public static IReadOnlyList<AdbDevice> Parse(string adbDevicesOutput)
    {
        var devices = new List<AdbDevice>();
        foreach (var rawLine in adbDevicesOutput.Split('\n'))
        {
            var device = ParseLine(rawLine.Trim());
            if (device is not null)
                devices.Add(device);
        }
        return devices;
    }

    static AdbDevice? ParseLine(string line)
    {
        if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase) || line.StartsWith('*'))
            return null;
        var tokens = line.Split(ColumnSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
            return null;
        return new AdbDevice(tokens[0], ParseState(tokens[1]), ParseModel(tokens));
    }

    static AdbDeviceState ParseState(string state) => state.ToLowerInvariant() switch
    {
        "device" => AdbDeviceState.Ready,
        "unauthorized" => AdbDeviceState.Unauthorized,
        "offline" => AdbDeviceState.Offline,
        _ => AdbDeviceState.Other,
    };

    static string ParseModel(string[] tokens)
    {
        var modelToken = tokens.Skip(2).FirstOrDefault(token => token.StartsWith("model:", StringComparison.OrdinalIgnoreCase));
        return modelToken is null ? "Headset" : modelToken["model:".Length..].Replace('_', ' ');
    }
}

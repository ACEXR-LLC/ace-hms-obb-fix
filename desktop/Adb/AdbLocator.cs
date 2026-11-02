namespace AceHeadsetFix.Adb;

public static class AdbLocator
{
    public static string? Find(string appDataDirectory)
    {
        foreach (var candidate in Candidates(appDataDirectory))
            if (ExistsSafely(candidate))
                return candidate;
        return null;
    }

    static IEnumerable<string> Candidates(string appDataDirectory)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "platform-tools", "adb.exe");
        yield return Path.Combine(appDataDirectory, "platform-tools", "adb.exe");
        foreach (var directory in PathEnvironmentDirectories())
            yield return CombineSafely(directory, "adb.exe");
        foreach (var sdkRoot in AndroidSdkRoots())
            yield return CombineSafely(sdkRoot, "platform-tools", "adb.exe");
        foreach (var editorDirectory in UnityEditorDirectories())
            yield return CombineSafely(editorDirectory, "Editor", "Data", "PlaybackEngines", "AndroidPlayer", "SDK", "platform-tools", "adb.exe");
    }

    static IEnumerable<string> PathEnvironmentDirectories()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var entry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var directory = entry.Trim('"');
            if (directory.Length > 0)
                yield return directory;
        }
    }

    static IEnumerable<string> AndroidSdkRoots()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (localAppData.Length > 0)
            yield return Path.Combine(localAppData, "Android", "Sdk");
        foreach (var variableName in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
        {
            var sdkRoot = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrEmpty(sdkRoot))
                yield return sdkRoot;
        }
    }

    static IReadOnlyList<string> UnityEditorDirectories()
    {
        var editorRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor");
        try
        {
            return Directory.Exists(editorRoot) ? Directory.EnumerateDirectories(editorRoot).OrderDescending().ToList() : [];
        }
        catch
        {
            return [];
        }
    }

    static string CombineSafely(params string[] parts)
    {
        try
        {
            return Path.Combine(parts);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    static bool ExistsSafely(string path)
    {
        try
        {
            return path.Length > 0 && File.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}

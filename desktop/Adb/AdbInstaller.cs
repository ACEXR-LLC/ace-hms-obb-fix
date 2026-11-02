using System.IO.Compression;

namespace AceHeadsetFix.Adb;

public sealed class AdbInstaller
{
    const string PlatformToolsUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";

    readonly string _appDataDirectory;

    public AdbInstaller(string appDataDirectory) => _appDataDirectory = appDataDirectory;

    public async Task<string> InstallAsync(Action<int> reportPercent, CancellationToken cancellation)
    {
        Directory.CreateDirectory(_appDataDirectory);
        var zipPath = Path.Combine(_appDataDirectory, "platform-tools-download.zip");
        try
        {
            await DownloadAsync(zipPath, reportPercent, cancellation).ConfigureAwait(false);
            ZipFile.ExtractToDirectory(zipPath, _appDataDirectory, overwriteFiles: true);
        }
        finally
        {
            DeleteQuietly(zipPath);
        }

        var adbPath = Path.Combine(_appDataDirectory, "platform-tools", "adb.exe");
        if (!File.Exists(adbPath))
            throw new InvalidOperationException("The download finished but adb.exe was not inside it.");
        return adbPath;
    }

    static async Task DownloadAsync(string zipPath, Action<int> reportPercent, CancellationToken cancellation)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        using var response = await http.GetAsync(PlatformToolsUrl, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var source = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            var destination = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (destination.ConfigureAwait(false))
            {
                await CopyWithProgressAsync(source, destination, totalBytes, reportPercent, cancellation).ConfigureAwait(false);
            }
        }
    }

    static async Task CopyWithProgressAsync(Stream source, Stream destination, long totalBytes, Action<int> reportPercent, CancellationToken cancellation)
    {
        var buffer = new byte[81920];
        long copied = 0;
        var lastReportedBucket = -1;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellation).ConfigureAwait(false);
            if (read == 0)
                return;
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
            copied += read;
            if (totalBytes <= 0)
                continue;
            var percent = (int)(copied * 100 / totalBytes);
            if (percent / 25 == lastReportedBucket)
                continue;
            lastReportedBucket = percent / 25;
            reportPercent(percent);
        }
    }

    static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}

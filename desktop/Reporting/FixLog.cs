namespace AceHeadsetFix.Reporting;

public sealed class FixLog
{
    readonly object _gate = new();

    public string LogDirectory { get; }
    public string LogPath { get; }

    public FixLog(string appDataDirectory)
    {
        LogDirectory = Path.Combine(appDataDirectory, "logs");
        Directory.CreateDirectory(LogDirectory);
        LogPath = Path.Combine(LogDirectory, $"fix-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
    }

    public void Write(string message)
    {
        lock (_gate)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
    }

    public void WriteStatus(StatusLine line)
    {
        var detail = line.Detail is null ? string.Empty : Environment.NewLine + line.Detail;
        Write($"[{line.Outcome}] {line.Text}{detail}");
    }
}

using System.Diagnostics;
using System.Text;
using AceHeadsetFix.Reporting;

namespace AceHeadsetFix.Adb;

public sealed class AdbRunner
{
    readonly string _adbPath;
    readonly FixLog _log;

    public AdbRunner(string adbPath, FixLog log)
    {
        _adbPath = adbPath;
        _log = log;
    }

    public CancellationToken Cancellation { get; init; }

    public Task<AdbResult> RunAsync(TimeSpan timeout, params string[] arguments) => RunAsync(arguments, timeout);

    public Task<AdbResult> ShellAsync(string serial, string command, TimeSpan timeout) =>
        RunAsync(new[] { "-s", serial, "shell", command }, timeout);

    public async Task<AdbResult> RunAsync(IReadOnlyList<string> arguments, TimeSpan timeout)
    {
        Cancellation.ThrowIfCancellationRequested();
        var description = "adb " + string.Join(" ", arguments);
        using var process = new Process { StartInfo = BuildStartInfo(arguments) };
        process.Start();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        try
        {
            await Task.WhenAll(standardOutput, standardError, process.WaitForExitAsync()).WaitAsync(timeout, Cancellation).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            TryKill(process);
            _log.Write($"{description} timed out after {timeout.TotalSeconds:F0}s");
            return new AdbResult(-1, string.Empty, $"adb did not finish within {timeout.TotalSeconds:F0} seconds.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            _log.Write($"{description} cancelled");
            throw;
        }

        var result = new AdbResult(process.ExitCode, await standardOutput.ConfigureAwait(false), await standardError.ConfigureAwait(false));
        _log.Write($"{description} exited {result.ExitCode}{Environment.NewLine}{result.CombinedOutput}");
        return result;
    }

    ProcessStartInfo BuildStartInfo(IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _adbPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        return startInfo;
    }

    static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill();
        }
        catch
        {
        }
    }
}

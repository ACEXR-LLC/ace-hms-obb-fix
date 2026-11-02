namespace AceHeadsetFix.Adb;

public sealed record AdbResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    public string CombinedOutput => (StandardOutput + Environment.NewLine + StandardError).Trim();
}

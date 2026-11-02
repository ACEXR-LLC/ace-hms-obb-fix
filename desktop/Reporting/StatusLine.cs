namespace AceHeadsetFix.Reporting;

public enum StepOutcome
{
    Working,
    Passed,
    Failed,
    Info,
    Instruction,
}

public sealed record StatusLine(StepOutcome Outcome, string Text, string? Detail = null);

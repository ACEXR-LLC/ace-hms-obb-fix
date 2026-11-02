using AceHeadsetFix.Adb;
using AceHeadsetFix.Headset;
using AceHeadsetFix.Reporting;
using AceHeadsetFix.Usb;

namespace AceHeadsetFix;

public sealed class FixSummary
{
    public int Fixed { get; set; }
    public int AlreadyFine { get; set; }
    public int Problems { get; set; }

    public string Headline =>
        Problems > 0 ? Explanations.SummaryProblems :
        Fixed > 0 ? Explanations.SummaryFixed :
        Explanations.SummaryNothingToFix;
}

public sealed class HeadsetFixer
{
    static readonly TimeSpan HeadsetWaitLimit = TimeSpan.FromMinutes(5);
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    static readonly TimeSpan QuickTimeout = TimeSpan.FromSeconds(60);

    readonly string _appDataDirectory;
    readonly FixLog _log;
    readonly IProgress<StatusLine> _status;
    readonly CancellationToken _cancellation;

    public HeadsetFixer(string appDataDirectory, FixLog log, IProgress<StatusLine> status, CancellationToken cancellation)
    {
        _appDataDirectory = appDataDirectory;
        _log = log;
        _status = status;
        _cancellation = cancellation;
    }

    public async Task<FixSummary> RunAsync()
    {
        var summary = new FixSummary();
        var adbPath = await EnsureAdbAsync().ConfigureAwait(false);
        var adb = new AdbRunner(adbPath, _log) { Cancellation = _cancellation };
        var headsets = await WaitForReadyHeadsetsAsync(adb).ConfigureAwait(false);
        if (headsets.Count == 0)
        {
            summary.Problems++;
            return summary;
        }
        foreach (var headset in headsets)
            await FixHeadsetAsync(adb, headset, summary).ConfigureAwait(false);
        return summary;
    }

    async Task<string> EnsureAdbAsync()
    {
        Report(StepOutcome.Working, "Checking this computer has the Android tool (adb)…");
        var existing = AdbLocator.Find(_appDataDirectory);
        if (existing is not null)
        {
            _log.Write("adb at " + existing);
            Report(StepOutcome.Passed, "Android tool found.");
            return existing;
        }
        Report(StepOutcome.Info, Explanations.DownloadingAdb);
        var installed = await new AdbInstaller(_appDataDirectory)
            .InstallAsync(percent => Report(StepOutcome.Working, $"Downloading… {percent}%"), _cancellation)
            .ConfigureAwait(false);
        _log.Write("adb installed at " + installed);
        Report(StepOutcome.Passed, "Android tool installed.");
        return installed;
    }

    async Task<IReadOnlyList<AdbDevice>> WaitForReadyHeadsetsAsync(AdbRunner adb)
    {
        Report(StepOutcome.Working, "Looking for the headset…");
        var deadline = DateTime.UtcNow + HeadsetWaitLimit;
        var lastSituation = string.Empty;
        var restartedAdb = false;
        while (true)
        {
            var listing = await adb.RunAsync(QuickTimeout, "devices", "-l").ConfigureAwait(false);
            var devices = DeviceListParser.Parse(listing.StandardOutput);
            var ready = devices.Where(device => device.State == AdbDeviceState.Ready).ToList();
            if (ready.Count > 0)
            {
                Report(StepOutcome.Passed, $"Found {ready.Count} headset{(ready.Count == 1 ? string.Empty : "s")}: {string.Join(", ", ready.Select(device => device.DisplayName))}");
                return ready;
            }

            var (situation, instruction, hopeless) = DescribeSituation(devices);
            if (situation != lastSituation)
            {
                Report(hopeless ? StepOutcome.Failed : StepOutcome.Instruction, instruction);
                lastSituation = situation;
            }
            if (hopeless)
                return [];
            if (situation == "adb-blind" && !restartedAdb)
            {
                restartedAdb = true;
                await adb.RunAsync(QuickTimeout, "kill-server").ConfigureAwait(false);
            }
            if (DateTime.UtcNow > deadline)
            {
                Report(StepOutcome.Failed, Explanations.GaveUpWaiting);
                return [];
            }
            await Task.Delay(PollInterval, _cancellation).ConfigureAwait(false);
        }
    }

    static (string Situation, string Instruction, bool Hopeless) DescribeSituation(IReadOnlyList<AdbDevice> devices)
    {
        if (devices.Any(device => device.State == AdbDeviceState.Unauthorized))
            return ("unauthorized", Explanations.WaitingUnauthorized, false);
        if (devices.Any(device => device.State == AdbDeviceState.Offline))
            return ("offline", Explanations.WaitingOffline, false);

        var usb = HeadsetUsbProbe.Probe();
        return usb.State switch
        {
            UsbHeadsetState.NotSeen => ("not-seen", Explanations.WaitingNotSeen, false),
            UsbHeadsetState.DebuggingOff => ("debugging-off", Explanations.WaitingDebuggingOff, false),
            UsbHeadsetState.DriverProblem => ("driver", Explanations.DriverMissing + Environment.NewLine + "Windows reports: " + usb.Detail, true),
            UsbHeadsetState.DebuggingOn => ("adb-blind", Explanations.WaitingAdbBlind, false),
            _ => ("unknown", Explanations.WaitingUnknown + Environment.NewLine + usb.Detail, false),
        };
    }

    async Task FixHeadsetAsync(AdbRunner adb, AdbDevice headset, FixSummary summary)
    {
        Report(StepOutcome.Working, $"Checking Ace on {headset.DisplayName}…");
        var package = await AcePackageInspector.InspectAsync(adb, headset.Serial).ConfigureAwait(false);
        if (!package.Installed)
        {
            Report(StepOutcome.Failed, Explanations.AceNotInstalled);
            summary.Problems++;
            return;
        }
        Report(StepOutcome.Passed, $"Ace {package.VersionName} is installed.");

        Report(StepOutcome.Working, "Checking Ace's content file…");
        var content = await ObbInspector.InspectAsync(adb, headset.Serial, package).ConfigureAwait(false);
        switch (content.State)
        {
            case ContentFileState.NotDownloaded:
                await HandleMissingContentFileAsync(adb, headset, package, content, summary).ConfigureAwait(false);
                return;
            case ContentFileState.Readable:
                Report(StepOutcome.Passed, Explanations.AlreadyReadable, DescribeEntries(package, content.Entries));
                await ConfirmLaunchAsync(adb, headset, summary, fixedJustNow: false).ConfigureAwait(false);
                return;
        }

        var contentFile = content.ContentFile!;
        Report(StepOutcome.Info, $"Found the problem: the content file belongs to '{contentFile.Owner}' with permissions {contentFile.Mode}, so Ace (which runs as '{package.AppUser}') can't read it.");
        var repair = await new ObbRepair(adb, _status).RepairAsync(headset.Serial, contentFile).ConfigureAwait(false);
        if (!repair.Succeeded)
        {
            Report(StepOutcome.Failed, repair.Message);
            summary.Problems++;
            return;
        }
        Report(StepOutcome.Passed, "Fixed. Ace can now read its content file.", repair.Message);
        await ConfirmLaunchAsync(adb, headset, summary, fixedJustNow: true).ConfigureAwait(false);
    }

    async Task HandleMissingContentFileAsync(AdbRunner adb, AdbDevice headset, AcePackageInfo package, ContentFileReport content, FixSummary summary)
    {
        Report(StepOutcome.Info, "No content file for this version of Ace is on the headset. Starting Ace to see whether it needs one…", DescribeEntries(package, content.Entries));
        var launch = await new LaunchVerifier(adb).LaunchAndWatchAsync(headset.Serial).ConfigureAwait(false);
        if (launch.Outcome == LaunchOutcome.ReachedVr)
        {
            Report(StepOutcome.Passed, "Ace started and reached VR without a separate content file. Nothing to fix on this headset.");
            summary.AlreadyFine++;
            return;
        }
        Report(StepOutcome.Failed, Explanations.ContentNotDownloaded);
        summary.Problems++;
    }

    async Task ConfirmLaunchAsync(AdbRunner adb, AdbDevice headset, FixSummary summary, bool fixedJustNow)
    {
        Report(StepOutcome.Working, "Starting Ace on the headset to confirm it works…");
        var launch = await new LaunchVerifier(adb).LaunchAndWatchAsync(headset.Serial).ConfigureAwait(false);
        switch (launch.Outcome)
        {
            case LaunchOutcome.ReachedVr:
                Report(StepOutcome.Passed, "Ace started and reached VR. It is open on the headset now.");
                if (fixedJustNow)
                    summary.Fixed++;
                else
                    summary.AlreadyFine++;
                return;
            case LaunchOutcome.ContentUnreadable:
                Report(StepOutcome.Failed, Explanations.LaunchStillBroken);
                summary.Problems++;
                return;
            default:
                Report(StepOutcome.Info, Explanations.LaunchUndetermined, launch.Detail);
                if (fixedJustNow)
                    summary.Fixed++;
                else
                    summary.AlreadyFine++;
                return;
        }
    }

    static string DescribeEntries(AcePackageInfo package, IReadOnlyList<ObbEntry> entries)
    {
        var expected = $"Expected: {ObbInspector.ExpectedFileName(package.VersionCode)}";
        var contentFiles = entries.Where(entry => entry.FileName.Contains(".obb", StringComparison.OrdinalIgnoreCase)).ToList();
        if (contentFiles.Count == 0)
            return expected + Environment.NewLine + "Found: no content file in " + ObbInspector.ObbDirectory;
        var found = contentFiles.Select(entry => $"{entry.FileName}  owner {entry.Owner}  permissions {entry.Mode}  {entry.SizeText}");
        return expected + Environment.NewLine + "Found: " + string.Join(Environment.NewLine + "       ", found);
    }

    void Report(StepOutcome outcome, string text, string? detail = null) =>
        _status.Report(new StatusLine(outcome, text, detail));
}

namespace AceHeadsetFix;

public static class Explanations
{
    public const string AdbDriverUrl = "https://developers.meta.com/horizon/downloads/package/oculus-adb-drivers/";
    public const string SupportUrl = "https://www.acexr.com/support/contact";

    public const string Intro =
        "Select Ace customers have reported an issue related to accessing the Ace app after the latest Meta system update was installed " +
        "on their headsets. This issue may be the culprit if an enterprise Ace Virtual Shooting end user is seemingly caught in an endless " +
        "loading state after opening the Ace Virtual Shooting VR app on the Meta Quest.\n\n" +
        "Meta is aware of this issue and is working on a fix. Until then, connecting the impacted headset(s) to your computer and following " +
        "the instructions in this app will resolve the issue. The entire process should take about two minutes.\n\n" +
        "1. Turn on USB debugging in the headset (see \"How to turn on USB debugging\" below).\n" +
        "2. Plug the headset into this computer with a USB cable and make sure it is switched on.\n" +
        "3. Click the button below.\n" +
        "4. If anything else is needed, additional instructions will appear below the button.";

    public const string Why =
        "When Meta Device Manager installs Ace on a shared headset, it downloads Ace's content file (about 2.8 GB) correctly " +
        "but stores it in a way that Ace is not allowed to open. Ace starts, cannot read its content, and stays on the loading " +
        "screen. This tool makes a copy of that file that Ace can read and puts it in place of the original. It does not download " +
        "anything from Meta, does not change any headset settings, and does not touch anything else on the headset. Device Manager may do " +
        "the same thing again when it installs the next Ace update, so run this tool again after each update. Meta is aware of " +
        "the issue and is working on a fix, so this tool will not be needed forever.";

    public const string DownloadingAdb =
        "This computer needs a small helper from Google (about 7 MB) to talk to the headset. Downloading it…";

    public const string WaitingUnauthorized =
        "The headset is asking for permission to connect to this computer. Put the headset on, check \"Always allow from this computer\", then select Allow. Waiting for your answer…";

    public const string WaitingOffline =
        "Windows can see the headset but it is not responding. Unplug the USB cable, wait five seconds and plug it back in. Waiting…";

    public const string WaitingNotSeen =
        "No headset found on USB. Check that the headset is switched on, the cable is firmly plugged in at both ends, " +
        "and the cable is a data cable (the cable that came with the headset is; many charge-only cables are not). Try another USB port. Waiting…";

    public const string WaitingDebuggingOff =
        "The headset is plugged in, but USB debugging is turned off, so this computer cannot connect to it. " +
        "On the headset go to Settings > Developer > USB Debugging and turn it on. The headset turns this setting off again every time it " +
        "restarts. Click \"How to turn on USB debugging\" below for step-by-step instructions. Waiting…";

    public const string WaitingAdbBlind =
        "Windows can see the headset, but the connection has not come up yet. Retrying… " +
        "if this does not clear within a minute, unplug and reconnect the cable.";

    public const string DriverMissing =
        "The headset is plugged in with USB debugging on, but Windows is missing the driver for it. Install Meta's ADB driver from " +
        AdbDriverUrl + " (unzip it, right-click android_winusb.inf and choose Install), then click the button again.";

    public const string WaitingUnknown =
        "Could not check which USB devices are connected. Check the cable and that USB debugging is turned on in the headset's Settings > Developer. Waiting…";

    public const string GaveUpWaiting =
        "Stopped waiting for the headset. Resolve the point above, then click the button again.";

    public const string FoundTheProblem =
        "Found the problem: Ace's content file is stored in a way Ace cannot read. This is the issue this tool fixes.";

    public const string AceNotInstalled =
        "Ace is not installed on this headset. Install it from Meta Device Manager (or the Meta Store) first, then run this tool again.";

    public const string ContentNotDownloaded =
        "Ace's content file is not on the headset yet. Device Manager is still downloading it, or the download failed. Keep the headset " +
        "on Wi-Fi and charging and give it time. In Meta Device Manager you can also open this headset's Apps tab and choose \"Retry failed installations\". " +
        "Then run this tool again.";

    public const string AlreadyReadable =
        "Ace's content file is already fine. There is nothing to fix on this headset.";

    public const string LaunchStillBroken =
        "Ace started but still could not open its content file. Restart the headset and run this tool again. If it still fails, send the log file (link below) to Ace support.";

    public const string LaunchUndetermined =
        "This tool could not confirm the result automatically. Put on the headset and check that Ace has opened. If it is still stuck, restart the headset and run this tool again.";

    public const string SummaryFixed = "All done. Put on the headset and open Ace. It should start normally now.";
    public const string SummaryNothingToFix = "Nothing needed fixing.";
    public const string SummaryProblems = "Something still needs attention. Follow the red lines above, then click the button again. If you are still stuck, send the log file to Ace support.";
}

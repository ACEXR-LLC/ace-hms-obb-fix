namespace AceHeadsetFix;

public static class Explanations
{
    public const string AdbDriverUrl = "https://developers.meta.com/horizon/downloads/package/oculus-adb-drivers/";

    public const string Intro =
        "Use this when Ace stays stuck on Meta's three loading dots after Device Manager installed it. " +
        "Plug the headset into this computer with a USB cable, make sure the headset is switched on, then click the button. " +
        "If the headset needs something from you, instructions appear below.";

    public const string Why =
        "When Meta's Device Manager installs Ace on a shared-mode headset, it downloads Ace's 2.8 GB content file correctly " +
        "but saves it under Device Manager's own account with permissions that don't let Ace open it. Ace starts, can't read " +
        "its content, and never gets past the loading dots. This tool makes a copy of that file which Ace is allowed to read " +
        "and puts it in place of the original. It doesn't download anything from Meta, doesn't change any headset settings, " +
        "and doesn't touch anything else on the headset. Device Manager will do the same thing again the next time it installs " +
        "an Ace update, so run this tool again after each update until Meta fixes the installer.";

    public const string DownloadingAdb =
        "This computer doesn't have Google's Android tool (adb), which is needed to talk to the headset. Downloading it from Google (about 7 MB)…";

    public const string WaitingUnauthorized =
        "The headset is asking whether to trust this computer. Put the headset on, tick \"Always allow from this computer\" and tap Allow. Waiting…";

    public const string WaitingOffline =
        "Windows sees the headset but it isn't responding. Unplug the USB cable, wait five seconds and plug it back in. Waiting…";

    public const string WaitingNotSeen =
        "No headset found on USB. Check that the headset is switched on, the cable is firmly in both the headset and this computer, " +
        "and the cable carries data (the one that came in the Quest box does; many charge-only cables don't). Try another USB port. Waiting…";

    public const string WaitingDebuggingOff =
        "The headset is plugged in, but USB debugging is switched off, so this computer isn't allowed to talk to it. " +
        "On the headset go to Settings > Developer > USB Debugging and turn it on. Device Manager switches USB debugging off again every time the headset " +
        "restarts, so you may need to do this each time. Click \"How to turn on USB debugging\" below for step-by-step pictures. Waiting…";

    public const string WaitingAdbBlind =
        "Windows sees the headset with USB debugging on, but the Android tool can't reach it yet. Restarting the tool… " +
        "if this doesn't clear within a minute, unplug the cable and plug it back in.";

    public const string DriverMissing =
        "The headset is plugged in with USB debugging on, but Windows is missing the driver for it. Install Meta's ADB driver from " +
        AdbDriverUrl + " (unzip it, right-click android_winusb.inf and choose Install), then click the button again.";

    public const string WaitingUnknown =
        "Couldn't ask Windows which USB devices are connected. Check the cable and that USB debugging is on in the headset's Settings > Developer. Waiting…";

    public const string GaveUpWaiting =
        "Gave up waiting for the headset. Sort out the point above, then click the button again.";

    public const string FoundTheProblem =
        "Found the problem: the headset saved Ace's content file without giving Ace permission to read it.";

    public const string AceNotInstalled =
        "Ace isn't installed on this headset. Install it from Device Manager (or the Meta Store) first, then run this tool again.";

    public const string ContentNotDownloaded =
        "Ace's content file isn't on the headset yet. Device Manager is still downloading it, or the download failed. Keep the headset " +
        "on Wi-Fi and charged and give it time, or in Device Manager open this headset's Apps tab and choose \"Retry failed installations\". " +
        "Then run this tool again.";

    public const string AlreadyReadable =
        "Ace's content file is already readable. There is nothing to fix on this headset.";

    public const string LaunchStillBroken =
        "Ace started but still couldn't open its content file. Restart the headset, run this tool again, and if it still fails send the log file (link below) to Ace support.";

    public const string LaunchUndetermined =
        "Couldn't confirm automatically. Put on the headset and check that Ace has opened. If it is still stuck, restart the headset, run this tool again, and if it still fails send the log file (link below) to Ace support.";

    public const string SummaryFixed = "All done. Put on the headset and open Ace. It should start normally now.";
    public const string SummaryNothingToFix = "Nothing needed fixing.";
    public const string SummaryProblems = "Something still needs attention. Read the red lines above, then click the button again. If you're stuck, send the log file to Ace support.";
}

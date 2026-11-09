export const text = {
  why:
    "When Meta's Device Manager installs Ace on a shared-mode headset, it downloads Ace's 2.8 GB content file correctly " +
    "but saves it under Device Manager's own account with permissions that don't let Ace open it. Ace starts, can't read " +
    "its content, and never gets past the loading dots. This page makes a copy of that file which Ace is allowed to read " +
    "and puts it in place of the original. It doesn't download anything from Meta, doesn't change any headset settings, " +
    "and doesn't touch anything else on the headset. Device Manager will do the same thing again the next time it installs " +
    "an Ace update, so come back here after each update until Meta fixes the installer.",

  browserUnsupported:
    "This browser can't talk to USB devices. Open this page in Google Chrome or Microsoft Edge on a Windows, Mac, Linux or " +
    "Chromebook computer. Safari, Firefox, iPhones and iPads won't work.",

  noDevicePicked:
    "No headset was picked. Click the button again and choose the Quest in the list that pops up.",

  debuggingOffHint:
    "If the list was empty: check the cable is firmly in both ends and carries data (the one from the Quest box does), " +
    "and that USB debugging is on in the headset. On the headset go to Settings > Developer > USB Debugging and turn it on. " +
    "Device Manager switches USB debugging off again every time the headset restarts. Step-by-step pictures:",

  deviceBusy:
    "Another program on this computer is already talking to the headset (for example Meta Quest Developer Hub, SideQuest, " +
    "Meta Quest Link, Unity, or a command-line adb). Close it, unplug and replug the headset, then click the button again. " +
    "If you have Android developer tools installed, run \"adb kill-server\" in a terminal first; the adb server keeps running after the program that started it closes.",

  connectFailed:
    "Couldn't open a connection to the headset. On Windows this usually means the headset's ADB driver isn't the standard " +
    "WinUSB one; closing Meta Quest Link and Meta Quest Developer Hub, then unplugging and replugging the headset, often fixes it. " +
    "If it keeps failing, use the desktop Ace Headset Fix app instead.",

  allowPrompt:
    "The headset is asking whether to trust this computer. Put it on, tick \"Always allow from this computer\" and tap Allow. Waiting…",

  headsetSilent:
    "The headset isn't answering. Unplug the USB cable, wait five seconds, plug it back in, then reload this page and try again. " +
    "If another program on this computer uses the headset (Meta Quest Developer Hub, Meta Quest Link, SideQuest, Unity, adb), close it first.",

  stillWaitingForAllow:
    "Still waiting. Make sure the headset is awake and on your head; the \"Allow USB debugging?\" box only shows while it is worn. " +
    "If there is no box, unplug and replug the cable, reload this page and try again.",

  authenticateFailed:
    "The headset refused or dropped the connection. Unplug and replug the cable, reload this page and try again.",

  foundTheProblem:
    "Found the problem: the headset saved Ace's content file without giving Ace permission to read it.",

  aceNotInstalled:
    "Ace isn't installed on this headset. Install it from Device Manager (or the Meta Store) first, then try again.",

  contentNotDownloaded:
    "Ace's content file isn't on the headset yet. Device Manager is still downloading it, or the download failed. Keep the headset " +
    "on Wi-Fi and charged and give it time, or in Device Manager open this headset's Apps tab and choose \"Retry failed installations\". " +
    "Then try again.",

  alreadyReadable:
    "Ace's content file is already readable. There is nothing to fix on this headset.",

  launchStillBroken:
    "Ace started but still couldn't open its content file. Restart the headset, try again, and if it still fails copy the results and send them to Ace support.",

  launchUndetermined:
    "Couldn't confirm automatically. Put on the headset and check that Ace has opened. If it is still stuck, restart the headset and try again.",

  unexpectedAdvice:
    "Unplug and replug the headset and try again. If it keeps happening, copy the results and send them to Ace support.",

  summaryFixed: "All done. Put on the headset and open Ace. It should start normally now.",
  summaryNothingToFix: "Nothing needed fixing.",
  summaryProblems: "Something still needs attention. Read the red lines above, then try again. If you're stuck, copy the results and send them to Ace support.",
};

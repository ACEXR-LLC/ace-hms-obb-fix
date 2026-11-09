export const text = {
  why:
    "When Meta Device Manager installs Ace on a shared headset, it downloads Ace's content file (about 2.8 GB) correctly " +
    "but stores it in a way that Ace is not allowed to open. Ace starts, cannot read its content, and stays on the loading " +
    "screen. This page makes a copy of that file that Ace can read and puts it in place of the original. It does not download " +
    "anything, does not change any headset settings, and does not touch anything else on the headset. Device Manager may do " +
    "the same thing again when it installs the next Ace update, so run this page again after each update. Meta is aware of " +
    "the issue and is working on a fix, so this page will not be needed forever.",

  browserUnsupported:
    "This browser cannot connect to the headset. Open this page in Google Chrome or Microsoft Edge on a Windows, Mac, Linux " +
    "or Chromebook computer. Safari, Firefox, iPhones and iPads are not supported.",

  noDevicePicked:
    "No headset was chosen. Click the button again and choose the headset in the list that appears.",

  debuggingOffHint:
    "If the list was empty: check that the cable is firmly plugged in at both ends and is a data cable (the cable that came " +
    "with the headset is). Then check that USB debugging is turned on in the headset: Settings > Developer > USB Debugging. " +
    "The headset turns this setting off again every time it restarts.",

  deviceBusy:
    "Another program on this computer is already using the headset (for example Meta Quest Link, Meta Quest Developer Hub, " +
    "SideQuest or Unity). Close that program, unplug and reconnect the headset, then click the button again. If it still does " +
    "not connect, restart this computer and try again.",

  connectFailed:
    "Could not connect to the headset. Close Meta Quest Link and Meta Quest Developer Hub if they are open, then unplug and " +
    "reconnect the headset and try again. If it keeps failing, download the Windows app from the link below and use that instead.",

  allowPrompt:
    "The headset is asking for permission to connect to this computer. Put the headset on, check \"Always allow from this computer\", " +
    "then select Allow. Waiting for your answer…",

  headsetSilent:
    "The headset is not responding. Unplug the USB cable, wait five seconds, plug it back in, then reload this page and try again. " +
    "If another program on this computer uses the headset (Meta Quest Link, Meta Quest Developer Hub, SideQuest, Unity), close it first.",

  stillWaitingForAllow:
    "Still waiting. Make sure the headset is awake and on your head; the \"Allow USB debugging?\" message only appears while the " +
    "headset is worn. If you do not see it, unplug and reconnect the cable, reload this page and try again.",

  authenticateFailed:
    "The connection to the headset was not completed. Unplug and reconnect the cable, reload this page and try again.",

  foundTheProblem:
    "Found the problem: Ace's content file is stored in a way Ace cannot read. This is the issue this page fixes.",

  aceNotInstalled:
    "Ace is not installed on this headset. Install it from Meta Device Manager (or the Meta Store) first, then try again.",

  contentNotDownloaded:
    "Ace's content file is not on the headset yet. Device Manager is still downloading it, or the download failed. Keep the headset " +
    "on Wi-Fi and charging and give it time. In Meta Device Manager you can also open this headset's Apps tab and choose " +
    "\"Retry failed installations\". Then run this page again.",

  alreadyReadable:
    "Ace's content file is already fine. There is nothing to fix on this headset.",

  launchStillBroken:
    "Ace started but still could not open its content file. Restart the headset and run this page again. If it still fails, " +
    "click Copy results and send them to Ace support.",

  launchUndetermined:
    "This page could not confirm the result automatically. Put on the headset and check that Ace has opened. If it is still " +
    "stuck, restart the headset and try again.",

  unexpectedAdvice:
    "Unplug and reconnect the headset and try again. If it keeps happening, click Copy results and send them to Ace support.",

  summaryFixed: "All done. Put on the headset and open Ace. It should start normally now.",
  summaryNothingToFix: "Nothing needed fixing.",
  summaryProblems: "Something still needs attention. Follow the highlighted lines above, then try again. If you are still stuck, click Copy results and send them to Ace support.",
};

# Ace Headset Fix

A one-button Windows app for non-technical users whose Quest headset gets stuck on Meta's three loading dots when opening Ace after a Meta Device Manager (Horizon Managed Services, shared mode) install.

## What it does

1. Finds `adb` on the computer, or downloads Google's platform-tools into `%LOCALAPPDATA%\AceHeadsetFix` if there is none.
2. Waits for the headset, telling the user exactly why it cannot see it yet: no cable or charge-only cable, USB debugging switched off on the headset (with the shared-mode Settings path), the "Allow USB debugging" prompt, a missing Windows ADB driver, or a stale adb server.
3. Checks that Ace is installed and reads its version code and app user from `dumpsys package`.
4. Lists `/sdcard/Android/obb/com.Ace.Ace` and decides whether `main.<versionCode>.com.Ace.Ace.obb` is readable by Ace (owned by Ace's user, or world-readable).
5. If it is not, copies the file on the headset as the adb shell user, sets it world-readable, verifies the copy with `md5sum`, swaps it into place and deletes the original.
6. Explains the cause in plain language and writes a full log to `%LOCALAPPDATA%\AceHeadsetFix\logs`.

The root cause is documented in the Meta support ticket and PR 931: the shared-mode installer (`com.oculus.ocms`) leaves the expansion file owned by itself with mode 0660 on Horizon OS's `obbfs`, and Ace's process is not in the `ext_obb_rw` group.

## Build

Requires the .NET 8 SDK.

```powershell
.\publish.ps1
```

This produces a single self-contained `publish\AceHeadsetFix.exe` that runs without installing .NET. Send that file to the user; they double-click it and press the button.

The exe is not code-signed, so Windows SmartScreen shows "Windows protected your PC" the first time it is run from a download. The user clicks "More info" and then "Run anyway". Tell them that in the email.

Before the fix can run, USB debugging has to be on in the headset (Settings > Developer > USB Debugging) and the headset has to accept the "Allow USB debugging" prompt from the computer. The app walks the user through both and waits for them. The "How to turn on USB debugging" button opens `Ui/GuideDialog.cs`, a step-by-step guide with the reference pictures in `Assets/guide` (the same ones the web page uses; see `../docs/README.md` for their source).

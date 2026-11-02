# Ace Headset Fix

Fixes a Meta Quest that stays stuck on Meta's three loading dots when opening Ace after a Meta Device Manager (Horizon Managed Services, shared mode) install.

Two ways to run it, same fix underneath:

| | Web page (`docs/`) | Windows app (`desktop/`) |
|---|---|---|
| Needs | Chrome or Edge on Windows, macOS, Linux or ChromeOS | Windows 10/11 |
| Install | nothing, open the page | download one exe, click past SmartScreen |
| Talks to the headset via | WebUSB (Tango, adb in the browser) | adb (downloaded automatically if missing) |

Both need USB debugging switched on in the headset (Settings > Advanced > USB Debugging, Device Manager admin passcode) and the headset's "Allow USB debugging?" prompt accepted once.

## What is wrong on the headset

Device Manager downloads Ace's 2.8 GB expansion file correctly but leaves it owned by its own installer account with permissions `0660` on Horizon OS, and Ace's process is not in the group that can read it. Ace starts, cannot open its content, and never gets past the loading splash. The fix copies the file as the adb shell user, makes the copy world-readable, verifies it with `md5sum`, swaps it into place and deletes the original. It then starts Ace and reads the headset log to confirm it reached VR. Device Manager repeats the mistake on every Ace update, so the fix is needed again after each one until Meta corrects the installer.

## Web page

`docs/` is a static site served by GitHub Pages. See [docs/README.md](docs/README.md) for hosting and user steps.

## Windows app

`desktop/` is a .NET 8 WinForms app. `desktop\publish.ps1` builds a single self-contained `AceHeadsetFix.exe`; tagged releases build it automatically and attach it to the GitHub release. See [desktop/README.md](desktop/README.md).

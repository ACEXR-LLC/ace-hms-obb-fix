# Ace Headset Fix (web)

A browser version of `../desktop`. It repairs the same problem, a Quest stuck on Meta's three loading dots after a Device Manager (Horizon Managed Services, shared mode) install left Ace's expansion file unreadable, but it runs as a web page: nothing to download, no SmartScreen warning, and it works on Windows, macOS, Linux and ChromeOS.

Live at https://acexr-llc.github.io/ace-hms-obb-fix/. The page also links to the Windows app, which the deploy workflow builds and publishes alongside it as `AceHeadsetFix.exe`.

It talks to the headset over WebUSB using [Tango](https://tangoadb.dev), a TypeScript implementation of the adb protocol, so no adb binary is involved.

## Files

- `index.html`, `styles.css` — the page, dark Ace theme matching the desktop app
- `app.js` — device picking, authentication and the step-by-step UI
- `usbConnection.js` — the USB transport: a buffered adb packet reader and writer over WebUSB. Tango's own WebUSB connection keeps a read only when it is exactly a 24-byte header, but the Quest returns the header and its payload in one read, so Tango never sees the headset's first reply. This transport buffers, drops leftover packets from an earlier session, and feeds Tango's `AdbDaemonTransport.authenticate`.
- `headset.js` — the diagnosis and repair logic, a line-for-line port of the desktop app's `HeadsetFixer.cs`, `Headset/ObbRepair.cs` and `Headset/LaunchVerifier.cs`
- `text.js` — every sentence shown to the user
- `guide/` — the two reference pictures in the "How to turn on USB debugging" guide (the headset's quick settings panel and its "Allow USB debugging?" box), cropped from [ArborXR's guide](https://help.arborxr.com/en/articles/10769343-enable-usb-debugging-on-horizon-managed-services-devices) and credited in the dialog. The step text follows [ManageXR's shared-mode guide](https://help.managexr.com/en/articles/13006370-how-to-enable-usb-debugging-on-a-mhms-shared-mode-device). The same pictures are embedded in the Windows app from `../desktop/Assets/guide`.
- `logo-white.png` — the Ace wordmark

Tango is loaded from esm.sh at pinned versions (`@yume-chan/adb@2.6.4`, `@yume-chan/adb-daemon-webusb@2.3.2`, `@yume-chan/adb-credential-web@2.1.0`). There is no build step.

## Deployment

`.github/workflows/deploy.yml` runs on every push to `main`: it builds the Windows app, drops `AceHeadsetFix.exe` into this folder, and publishes the folder to GitHub Pages. The repository's Pages source must be set to **GitHub Actions**. The exe never enters git.

To try changes before pushing, serve this folder locally; WebUSB needs HTTPS or `localhost`:

```powershell
python -m http.server 8765 --bind 127.0.0.1
```

## What the user does

1. Opens the page in Google Chrome or Microsoft Edge. Safari, Firefox, iPhones and iPads do not support WebUSB; the page says so.
2. Turns USB debugging on in the headset (Settings > Developer > USB Debugging). The "How to turn on USB debugging" button opens a step-by-step guide with pictures; the same guide is offered in the results when no headset is picked. Device Manager turns it off again on every restart.
3. Plugs the headset into the computer and clicks the button.
4. Picks the Quest in the browser's USB device list.
5. Puts the headset on if it asks "Allow USB debugging?", ticks "Always allow from this computer" and taps Allow.
6. Reads the results. After a fix the page starts Ace and reads the headset log to confirm it reached VR.

## Known limits

- Only one program can hold the headset's ADB interface at a time. Meta Quest Developer Hub, Meta Quest Link, SideQuest, the Unity Editor or a running `adb` server on the same computer block the page; the page explains this when it happens.
- On Windows the headset's ADB interface must be bound to a WinUSB-based driver. Meta's own ADB driver and the generic driver Windows installs for Android devices both are.
- The adb key the page generates lives in the browser's IndexedDB for this site; clearing site data means the headset will ask "Allow USB debugging?" again.

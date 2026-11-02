# Ace Headset Fix (web)

A browser version of `Tools/AceHeadsetFix`. It repairs the same problem, a Quest stuck on Meta's three loading dots after a Device Manager (Horizon Managed Services, shared mode) install left Ace's expansion file unreadable, but it runs as a web page: nothing to download, no SmartScreen warning, and it works on Windows, macOS, Linux and ChromeOS.

It talks to the headset over WebUSB using [Tango](https://tangoadb.dev), a TypeScript implementation of the adb protocol, so no adb binary is involved.

## Files

- `index.html`, `styles.css` — the page, dark Ace theme matching the desktop app
- `app.js` — WebUSB connection, authentication and the step-by-step UI
- `headset.js` — the diagnosis and repair logic, a line-for-line port of `Tools/AceHeadsetFix/HeadsetFixer.cs`, `Headset/ObbRepair.cs` and `Headset/LaunchVerifier.cs`
- `text.js` — every sentence shown to the user
- `logo-white.png` — the Ace wordmark (stored in Git LFS)

Tango is loaded from esm.sh at pinned versions (`@yume-chan/adb@2.6.4`, `@yume-chan/adb-daemon-webusb@2.3.2`, `@yume-chan/adb-credential-web@2.1.0`). There is no build step.

## Hosting

Any static host works, but the page must be served over HTTPS (or `localhost` for testing) because WebUSB only runs in a secure context. Copy the folder to the host as-is. For a quick local test:

```powershell
python -m http.server 8765 --bind 127.0.0.1
```

then open `http://127.0.0.1:8765/`.

## What the user does

1. Opens the page in Google Chrome or Microsoft Edge. Safari, Firefox, iPhones and iPads do not support WebUSB; the page says so.
2. Turns USB debugging on in the headset (Settings > Advanced > USB Debugging, Device Manager admin passcode, default 5112). Device Manager turns it off again on every restart.
3. Plugs the headset into the computer and clicks the button.
4. Picks the Quest in the browser's USB device list.
5. Puts the headset on if it asks "Allow USB debugging?", ticks "Always allow from this computer" and taps Allow.
6. Reads the results. After a fix the page starts Ace and reads the headset log to confirm it reached VR.

## Known limits

- Only one program can hold the headset's ADB interface at a time. Meta Quest Developer Hub, Meta Quest Link, SideQuest or a running `adb` server on the same computer block the page; the page explains this when it happens.
- On Windows the headset's ADB interface must be bound to a WinUSB-based driver. Meta's own ADB driver and the generic driver Windows installs for Android devices both are.
- The adb key the page generates lives in the browser's IndexedDB for this site; clearing site data means the headset will ask "Allow USB debugging?" again.

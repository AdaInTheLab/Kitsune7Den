# Installing Kitsune7Den

## Download

Grab the latest `Kitsune7Den.exe` from [Releases](https://github.com/Kitsune-Den/Kitsune7Den/releases/latest).

Requires **Windows 10 or 11 x64**. No installer, no dependencies, no runtime needed — it's a single self-contained exe.

## Verify the download (optional but recommended)

Every release ships with a `Kitsune7Den.exe.sha256` file containing the SHA-256 hash of the exe. You can verify you got the exact file we published:

```powershell
# In the folder where you downloaded both files:
Get-FileHash .\Kitsune7Den.exe -Algorithm SHA256
Get-Content .\Kitsune7Den.exe.sha256
```

The two hashes should match. If they don't, don't run it — download again or report it.

## First run: the SmartScreen warning

Because Kitsune7Den isn't code-signed with a commercial certificate, Windows SmartScreen will show a blue dialog the first time you run it:

> **Windows protected your PC**
>
> Microsoft Defender SmartScreen prevented an unrecognized app from starting. Running this app might put your PC at risk.

**This is expected for unsigned open source software.** It doesn't mean the app is malicious — it means the publisher isn't in Microsoft's "known reputation" database yet, which requires paying a commercial CA several hundred dollars a year for an EV code signing certificate. That's not in scope for a free tool right now.

**To run it anyway:**

1. Click **More info** (the small link under the message)
2. Click **Run anyway**

You only have to do this once per downloaded version. After that Windows remembers your decision.

If you want to be extra careful before clicking "Run anyway":

- **Verify the SHA-256** matches the value in the release (see above)
- **Read the source** — everything Kitsune7Den does is in [this repo](https://github.com/Kitsune-Den/Kitsune7Den)
- **Build it yourself** — see [Building from Source](README.md#building-from-source) in the README

## After the first launch

Kitsune7Den stores its config in `%LocalAppData%\Kitsune7Den\settings.json`. Backups go under your server directory in a `Kitsune7Den-Backups` subfolder.

From v1.0.2 onwards the app can update itself — go to **Settings → Check for Updates** and it'll pull the latest release straight from GitHub. No need to re-download from the web once you're in.

## Uninstalling

Delete `Kitsune7Den.exe` and (optionally) the config folder at `%LocalAppData%\Kitsune7Den`. That's it. No registry keys, no leftover services.

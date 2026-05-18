# Privacy Policy

**AeroSSH — SSH Client for Android**

## Data Collection

AeroSSH does **not** collect, transmit, sell, or share any personal data. There
is no telemetry, no analytics, no crash reporting, and no advertising SDK in the
app.

## Local Storage

The following data is stored exclusively on your device, inside the app's
private storage:

- **Connection profiles** (host, port, username, password and/or private key,
  optional passphrase) — stored in AndroidX `EncryptedSharedPreferences` with an
  AES-256 master key backed by the Android Keystore.
- **Host-key fingerprints** (`known_hosts`-equivalent) — same encrypted store.
- **Command history** — per-profile, kept in the same encrypted store, capped at
  100 entries.
- **Session logs** — kept in memory while the app runs and written to
  `<files>/logs/` only when you explicitly tap "Export". Logs never leave the
  device unless you share them yourself.

Removing the app removes all of the above.

## Network

AeroSSH only connects to the SSH servers you explicitly configure. Cleartext
HTTP traffic is disabled by the bundled network security configuration.

## Permissions

| Permission | Why |
|---|---|
| `INTERNET` | SSH connections |
| `ACCESS_NETWORK_STATE` | Detect network changes |
| `WAKE_LOCK` | Keep SSH session alive during long-running commands |
| `FOREGROUND_SERVICE` / `FOREGROUND_SERVICE_DATA_SYNC` | Keep an active session running when the app is backgrounded |
| `POST_NOTIFICATIONS` | Show the active-session notification (Android 13+) |

AeroSSH does **not** request storage permissions — file transfer goes through
Android's Storage Access Framework, which gives the user control on every pick.

## Open Source

The complete source code is published under Apache 2.0 at
<https://github.com/SyberianIT/AeroSSH>. You can audit, build, and modify it.

**Last updated:** 2026-05-18

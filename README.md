# AeroSSH

[![Build](https://github.com/SyberianIT/AeroSSH/actions/workflows/build.yml/badge.svg)](https://github.com/SyberianIT/AeroSSH/actions/workflows/build.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

Open-source SSH client for Android built with .NET for Android.

## Features

- **Connection profiles** with persistent encrypted storage (AES-256 via Android Keystore)
- **Password and private-key authentication** (RSA, ECDSA, Ed25519 via SSH.NET) with optional passphrase
- **TOFU host-key verification** with SHA-256 fingerprints — flagged on first use and on mismatch
- **Interactive shell** (xterm-256color)
- **Quick command runner** with per-profile history (top 100)
- **SFTP browser** — list, upload, download, delete (uses Android SAF, no storage permissions required)
- **Session logging** with JSON or plain-text export via Android Share
- **Dark / Light / System theme**
- **Foreground service** keeps the session alive when the app is backgrounded
- **Material Design 3** UI

## Requirements

- Android 8.0+ (API 26)
- For building: .NET 8 SDK with the `android` workload
  - `dotnet workload install android`

## Build

```bash
# Restore + build APK (debug)
dotnet build AeroSSH/AeroSSH.csproj

# Release APK
dotnet publish AeroSSH/AeroSSH.csproj -c Release -f net8.0-android

# Tests
dotnet test AeroSSH.Tests/AeroSSH.Tests.csproj
```

Output APK: `AeroSSH/bin/Release/net8.0-android/publish/io.github.syberianit.aerossh-Signed.apk`

## Project layout

```
AeroSSH/                       Android app
├── AeroSshApplication.cs      DI root, theme bootstrap
├── MainActivity.cs            Profile list + FAB
├── Activities/                ProfileEdit, Session, Settings
├── Fragments/                 Command, Shell, Sftp, Logs
├── Adapters/                  RecyclerView adapters
├── Models/                    ServerProfile, SshSession, LogEntry, SftpEntry
├── Services/
│   ├── IKeyValueStore         Storage abstraction (tested)
│   ├── SecurePreferences      AndroidX EncryptedSharedPreferences
│   ├── ProfileStore           CRUD over profiles
│   ├── HostKeyStore           known_hosts equivalent (fingerprint cache)
│   ├── CommandHistoryStore    Per-profile command history
│   ├── LogService             In-memory + JSON/TXT export
│   ├── SshServiceImpl         SSH.NET wrapper
│   ├── SessionManager         Multi-session coordinator
│   ├── SshForegroundService   Keep-alive foreground notification
│   └── ThemeManager           AppCompat dark/light/system
└── Resources/                 layouts, drawables, values, values-night

AeroSSH.Tests/                 xUnit tests for storage logic (run on any host)
```

## Privacy

AeroSSH does not collect, transmit, or share any data. Profiles and host-key
fingerprints live in AES-256 encrypted shared preferences. See [PRIVACY.md](PRIVACY.md).

## License

Apache License 2.0 — see [LICENSE](LICENSE).

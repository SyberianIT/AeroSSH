# Changelog

## [1.1.0] - 2026-05-18

### Added
- Connection profiles with AES-256 encrypted storage (AndroidX `EncryptedSharedPreferences`)
- Password and private-key authentication (RSA, ECDSA, Ed25519 via SSH.NET 2024.2)
- TOFU host-key verification: first-use trust dialog, fingerprint mismatch warning
- Quick command runner with per-profile history (top 100, deduplicated)
- Interactive shell tab (xterm-256color, 120×30)
- SFTP browser: list, upload, download, delete — uses Android Storage Access Framework
- Session log: in-memory ring with JSON and plain-text export via Android Share
- Dark / Light / System theme (Material Components DayNight)
- Foreground service notification keeps SSH alive in background
- xUnit test project for storage logic (14 tests covering ProfileStore, HostKeyStore, CommandHistoryStore)
- GitHub Actions CI building APK and running tests

### Changed
- Full rewrite. Target framework `net10.0-android` (minSdk 26, target 35)
- ApplicationId: `io.github.syberianit.aerossh`
- UI rebuilt with Material Components and `androidx.coordinatorlayout` / `BottomNavigationView`
- NuGet packages refreshed to versions compatible with the .NET 10 Android workload
  (SSH.NET 2025.0.0, Xamarin.AndroidX.* on the 1.7/1.8/1.10/1.15 lines, Material 1.13)

### Security
- Encrypted credential storage via Android Keystore-backed master key
- No `cleartextTrafficPermitted` for HTTP; SSH connections go via SSH.NET only
- Host-key checks fail closed on unknown or mismatched fingerprints

## [1.0.0] - 2026-04-01

Initial prototype scaffolding.

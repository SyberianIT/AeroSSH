# F-Droid Publishing Notes

## Prerequisites met

- Apache-2.0 license (see [LICENSE](LICENSE))
- Public GitHub repository
- Reproducible build via plain `dotnet publish`
- No proprietary dependencies (all NuGet packages are open-source)

## Tagging

```bash
git tag v1.1.0
git push origin v1.1.0
```

## Suggested fdroiddata metadata

File `metadata/io.github.syberianit.aerossh.yml`:

```yaml
Categories:
  - System
License: Apache-2.0
AuthorName: SyberianIT
SourceCode: https://github.com/SyberianIT/AeroSSH
IssueTracker: https://github.com/SyberianIT/AeroSSH/issues
Summary: Open-source SSH client for Android
Description: |-
  AeroSSH is an open-source SSH client for Android built with .NET for Android.

  Features:
    * Connection profiles with encrypted storage
    * Password and private-key authentication (RSA, ECDSA, Ed25519)
    * TOFU host-key verification with SHA-256 fingerprints
    * Interactive shell (xterm-256color)
    * Quick command runner with per-profile history
    * SFTP browser (list, upload, download, delete)
    * Session logging with JSON/text export
    * Dark / light / system theme
    * Foreground service for backgrounded sessions

RepoType: git
Repo: https://github.com/SyberianIT/AeroSSH.git

Builds:
  - versionName: 1.1.0
    versionCode: 2
    commit: v1.1.0
    sudo:
      - apt-get install -y dotnet-sdk-8.0
      - dotnet workload install android
    build: |
      dotnet publish AeroSSH/AeroSSH.csproj -c Release -f net8.0-android
    output: AeroSSH/bin/Release/net8.0-android/publish/io.github.syberianit.aerossh-Signed.apk

AutoUpdateMode: Version
UpdateCheckMode: Tags
CurrentVersion: 1.1.0
CurrentVersionCode: 2
```

## Submission

1. Fork [fdroiddata](https://gitlab.com/fdroid/fdroiddata).
2. Add the metadata file above.
3. Open a merge request — F-Droid build server takes it from there.

## Assets to attach to the release

Once the F-Droid bot starts indexing, you can supply (in `metadata/io.github.syberianit.aerossh/en-US/`):

- `icon.png` — 512×512
- `featureGraphic.png` — 1024×500
- `phoneScreenshots/*.png` — 1080×1920 or larger

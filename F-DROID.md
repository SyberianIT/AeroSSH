# F-Droid Publishing Guide

## Prerequisites
1. Open-source license (MIT/Apache 2.0) ✓
2. GitHub repository with public access ✓
3. Proper metadata and documentation ✓

## Publishing Steps

### 1. Add to F-Droid
```bash
git tag v1.1.0
git push origin v1.1.0
```

### 2. Create F-Droid Metadata
File: `metadata/io.github.burse.aerossh.yml`
```yaml
Categories:
  - System
License: Apache-2.0
SourceCode: https://github.com/SyberianIT/AeroSSH
IssueTracker: https://github.com/SyberianIT/AeroSSH/issues
Donation: https://github.com/sponsors/SyberianIT
Summary: SSH Client for Android
Description: |
  AeroSSH is a lightweight, modern SSH client for Android built with .NET.
  Features multiple sessions, SFTP file transfer, and interactive terminal.
VCS: git|https://github.com/SyberianIT/AeroSSH.git
Build:
  - versionName: 1.1.0
    versionCode: 2
    commit: v1.1.0
    gradle: yes
    prebuild: dotnet restore AeroSSH.slnx
    build: dotnet publish -c Release -f net10.0-android
```

### 3. Submit to F-Droid
1. Fork `fdroiddata` repository
2. Add metadata file above
3. Submit pull request
4. F-Droid will review and build

## Screenshots Required
- App icon (512x512 PNG)
- 2-3 screenshots (1080x1920 or 1440x2560)
- Feature graphic (1024x500 PNG)

## Monitoring
- Check build status on F-Droid website
- New version typically appears within 1-2 weeks
- F-Droid auto-updates existing installations

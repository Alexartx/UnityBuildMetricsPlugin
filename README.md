# Build Metrics for Unity

Track Unity build performance and catch regressions before they reach production.

## Features

- Automatic build time tracking
- Build size monitoring
- Platform comparison
- Regression detection
- CI/CD integration
- Free for indie developers (100 builds/month)

## Requirements

- **Unity 2020.3 LTS or newer** (minimum)
- **Unity 2022.2 or newer** (recommended for full file breakdown on Android/WebGL)
- Windows, macOS, or Linux
- .NET Standard 2.1
- Internet connection (for reporting)
- Git installed (optional, for commit tracking)

## Installation

### Method A: Git URL (Recommended)

Fastest way to install and get automatic updates.

1. Open Unity
2. Window → Package Manager
3. Click "+" → Add package from git URL
4. Paste this URL:
   ```
   https://github.com/yourusername/unity-buildmetrics.git
   ```
5. Click "Add"

### Method B: Unity Package

Download and import manually. Works offline.

1. Download the latest `.unitypackage` from [Releases](https://github.com/yourusername/unity-buildmetrics/releases)
2. In Unity: Assets → Import Package → Custom Package
3. Select the downloaded file
4. Click "Import"

## Quick Start

1. **Get your API key**
   - Sign up at [app.buildmetrics.moonlightember.com](https://app.buildmetrics.moonlightember.com)
   - Copy your API key from the dashboard

2. **Configure the plugin**
   - In Unity, go to Tools → Build Metrics → Settings
   - Paste your API key
   - Click "Save"

3. **Build your game**
   - Build your project as usual
   - Metrics are sent automatically after each build
   - View results in your [dashboard](https://app.buildmetrics.moonlightember.com/dashboard)

## Usage

### Automatic Mode (Default)

By default, metrics are sent automatically after every build. No code changes required.

### Manual Mode

If you prefer to control when metrics are sent:

1. Disable auto-upload in settings
2. Use the menu: Tools → Build Metrics → Upload Last Build

### Settings

Access settings via: **Tools → Build Metrics → Settings**

- **API Key**: Your unique identifier
- **API URL**: Server endpoint (default: production)
- **Auto Upload**: Enable/disable automatic upload

## What Gets Tracked?

### Basic Metrics (All Unity Versions)
- Build time (seconds)
- Build size (bytes)
- Platform (Android, iOS, Windows, etc.)
- Unity version
- Build name and version number
- Development vs Release build
- Artifact type (APK, IPA, etc.)
- Machine info (OS, CPU, RAM)
- Timestamp

### Git Information (Optional - Requires Git)
- Commit SHA (short hash)
- Commit message
- Branch name
- Dirty status (uncommitted changes)

### File Breakdown (Unity 2022.2+)
- **Categories**: Scripts, Resources, Streaming Assets, Plugins, Scenes, Shaders, Other
- **Top 20 largest files** with size and category
- **Works on all platforms** including Android and WebGL

**Note**: File breakdown requires Unity 2022.2+ for Android/WebGL. iOS and Standalone builds support file breakdown in earlier Unity versions.

## Troubleshooting

### "Invalid API key" error

- Check that your API key is correct in settings
- Verify your subscription is active at [app.buildmetrics.moonlightember.com](https://app.buildmetrics.moonlightember.com)

### Metrics not appearing in dashboard

- Check Unity console for errors
- Verify internet connection
- Check firewall/proxy settings

### Build fails with Build Metrics installed

- Check Unity console for specific errors
- Disable auto-upload temporarily
- Contact support: moonlightember1@gmail.com

## Support

- Documentation: [moonlightember.com/products/build-metrics/docs](https://moonlightember.com/products/build-metrics/docs)
- Email: support@moonlightember.com
- GitHub Issues: [github.com/Alexartx/UnityBuildMetricsPlugin/issues](https://github.com/Alexartx/UnityBuildMetricsPlugin/issues)

## License

MIT License - see [LICENSE.md](LICENSE.md) for details

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history

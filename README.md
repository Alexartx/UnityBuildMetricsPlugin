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

### ⚡ Fast Setup (Recommended)

1. **Get your API key**
   - Sign up at [app.buildmetrics.moonlightember.com](https://app.buildmetrics.moonlightember.com)
   - Create a project
   - Click **"Copy API Key"** button in dashboard

2. **Open Unity Setup Wizard**
   - In Unity: **Tools → Build Metrics → Setup Wizard**
   - The wizard will **auto-detect your copied API key** and ask: "Found API key in clipboard — use it?"
   - Click **"Use This Key"** → **"Complete Setup"**
   - Done! 🎉

3. **Build your game**
   - Build your project as usual
   - Metrics are sent automatically after each build
   - View results in your [dashboard](https://app.buildmetrics.moonlightember.com/dashboard)

### Manual Setup (Alternative)

If clipboard detection doesn't work or you prefer manual setup:

1. Copy your API key from the dashboard
2. In Unity: **Tools → Build Metrics → Settings**
3. Paste your API key
4. Build your project

## Configuration

### API Key Options

The plugin supports two ways to configure your API key:

#### Option 1: EditorPrefs (Per-Developer) ✅ Recommended

- Each developer configures their own API key
- Stored locally on their machine (never committed to Git)
- Most secure option
- Best for: Individual developers, tracking per-developer metrics

**How to configure:**
1. Tools → Build Metrics → Setup Wizard
2. Enter your API key
3. Click "Complete Setup"

#### Option 2: Environment Variable (Team/CI)

- Shared API key via environment variable `BUILD_METRICS_API_KEY`
- Environment variable takes priority over EditorPrefs
- Never store in Git - use shell profile or CI secrets
- Best for: Teams sharing one key, CI/CD pipelines

**How to configure:**

**macOS/Linux:**
```bash
# Add to ~/.zshrc or ~/.bashrc
export BUILD_METRICS_API_KEY="bm_your_api_key_here"
```

**Windows:**
```powershell
# PowerShell (persistent)
[System.Environment]::SetEnvironmentVariable('BUILD_METRICS_API_KEY', 'bm_your_api_key_here', 'User')
```

**CI/CD:**
```yaml
# GitHub Actions example
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

### Settings

Access settings via: **Tools → Build Metrics → Settings**

- **API Key**: Your unique identifier (or set via environment variable)
- **Auto Upload**: Enable/disable automatic upload after builds

When using environment variable, the settings UI shows: "✓ Using API key from environment variable"

## Usage

### Automatic Mode (Default)

By default, metrics are sent automatically after every build. No code changes required.

### Manual Mode

If you prefer to control when metrics are sent:

1. Disable auto-upload in settings
2. Use the menu: Tools → Build Metrics → Upload Last Build

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
- Contact support: support@moonlightember.com

## Support

- Documentation: [moonlightember.com/products/build-metrics/docs](https://moonlightember.com/products/build-metrics/docs)
- Email: support@moonlightember.com
- GitHub Issues: [github.com/Alexartx/UnityBuildMetricsPlugin/issues](https://github.com/Alexartx/UnityBuildMetricsPlugin/issues)

## License

MIT License - see [LICENSE.md](LICENSE.md) for details

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history

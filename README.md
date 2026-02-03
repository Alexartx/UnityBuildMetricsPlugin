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
   https://github.com/Alexartx/UnityBuildMetricsPlugin.git
   ```
5. Click "Add"

### Method B: Unity Package

Download and import manually. Works offline.

1. Download the latest `.unitypackage` from [Releases](https://github.com/Alexartx/UnityBuildMetricsPlugin/releases)
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

The plugin supports three ways to configure your API key (priority order):

#### Option 1: Command Line Arguments (CI/CD) 🚀 For GameCI

- Highest priority method
- Pass API key via Unity command line arguments
- Best for: GameCI, Docker-based CI/CD systems

**How to configure:**
```bash
Unity -quit -batchmode -projectPath . \
  -executeMethod YourBuild.Build \
  -BUILD_METRICS_API_KEY bm_your_api_key_here
```

**GitHub Actions (GameCI):**
```yaml
- uses: game-ci/unity-builder@v4
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
    UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
    UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  with:
    customParameters: "-development -BUILD_METRICS_API_KEY ${{ secrets.BUILD_METRICS_API_KEY }}"
    targetPlatform: Android
```

See [CI/CD Integration Guide](Documentation~/ci-cd/overview.md) for complete setup instructions.

#### Option 2: Environment Variable (Team/CI)

- Second priority method
- Shared API key via environment variable `BUILD_METRICS_API_KEY`
- Never store in Git - use shell profile or CI secrets
- Best for: Self-hosted runners, local team builds

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

**GitHub Actions (Self-Hosted):**
```yaml
jobs:
  build:
    runs-on: [self-hosted, macOS]
    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

#### Option 3: EditorPrefs (Per-Developer) ✅ For Local Development

- Lowest priority method (fallback)
- Each developer configures their own API key
- Stored locally on their machine (never committed to Git)
- Best for: Individual developers, local builds

**How to configure:**
1. Tools → Build Metrics → Setup Wizard
2. Enter your API key
3. Click "Complete Setup"

### Settings

Access settings via: **Tools → Build Metrics → Settings**

- **API Key**: Your unique identifier (or set via command line/environment variable)
- **Auto Upload**: Enable/disable automatic upload after builds

**Priority chain:**
1. Command line arguments (highest priority) - for CI/CD
2. Environment variable - for team/runner builds
3. EditorPrefs (lowest priority) - for local development

When using command line args or environment variable, the settings UI shows the active source.

## Usage

### Automatic Mode (Default)

By default, metrics are sent automatically after every build. No code changes required.

### Manual Mode

If you prefer to control when metrics are sent:

1. Disable auto-upload in settings
2. Use the menu: Tools → Build Metrics → Upload Last Build

## CI/CD Integration

Build Metrics works with any CI/CD system. Just add your API key as a secret and configure it in your workflow.

### GitHub Actions with GameCI (Recommended)

**Complete working example:**

```yaml
name: Build Unity Project

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      # Free up runner disk space
      - name: Free up runner disk space
        run: |
          df -h
          sudo docker image prune --all --force
          sudo docker builder prune --all --force
          sudo rm -rf /opt/hostedtoolcache/*
          sudo apt clean
          df -h

      # Checkout repository
      - uses: actions/checkout@v4
        with:
          lfs: true
          fetch-depth: 0  # Full git history for commit tracking

      # Build with Unity (license activation happens automatically)
      - name: Build Project
        uses: game-ci/unity-builder@v4
        env:
          BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          customParameters: "-development -BUILD_METRICS_API_KEY ${{ secrets.BUILD_METRICS_API_KEY }}"
          targetPlatform: Android
          unityVersion: auto
          buildName: MyGame

      # Upload build artifacts
      - uses: actions/upload-artifact@v4
        with:
          name: Build-Android
          path: build/Android
```

**Required GitHub Secrets:**
1. `BUILD_METRICS_API_KEY` - Get from [dashboard](https://app.buildmetrics.moonlightember.com)
2. `UNITY_LICENSE` - `.ulf` file contents from [Unity Manual License](https://license.unity3d.com/manual)
3. `UNITY_EMAIL` - Your Unity account email
4. `UNITY_PASSWORD` - Your Unity account password

**📖 Complete setup guide:** [CI/CD Integration Documentation](Documentation~/ci-cd/overview.md)

### GitHub Actions with Self-Hosted Runners

For self-hosted macOS/Windows/Linux runners with Unity installed:

```yaml
jobs:
  build:
    runs-on: [self-hosted, macOS, Unity]

    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true
          fetch-depth: 0

      - name: Build
        run: |
          /Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity \
            -quit -batchmode -nographics \
            -projectPath . \
            -executeMethod YourBuildMethod.Build \
            -logFile -
```

### Other CI/CD Platforms

Build Metrics works with any CI/CD system that supports environment variables or command line arguments:
- ✅ GitLab CI
- ✅ Jenkins
- ✅ CircleCI
- ✅ Azure Pipelines
- ✅ Unity Cloud Build
- ✅ Custom build servers

**📖 See full documentation:** [CI/CD Integration Guide](Documentation~/ci-cd/overview.md)

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

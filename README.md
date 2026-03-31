# Build Metrics for Unity

Build Metrics is the public UPM package that bundles local build analysis and cloud workflows in one install.

## What users get in this package

- automatic local capture after successful builds
- recent build history with size and time trend charts
- baseline comparison to spot regressions quickly
- file breakdown for final build contents
- asset attribution breakdown for project assets
- optional dashboard upload and pending retry queue
- API-key onboarding and CI/CD setup guidance
- Git commit and machine metadata when available

## Requirements

- **Unity 2022.2 or newer**
- Windows, macOS, or Linux editor
- Git optional for commit metadata
- Internet connection only if you want upload and dashboard features

## Installation

### Git URL

1. Open Unity
2. Open **Window -> Package Manager**
3. Click **+ -> Add package from git URL**
4. Paste:

```text
https://github.com/Alexartx/UnityBuildMetricsPlugin.git
```

### Local-first behavior

This package works immediately without an API key:

- builds are captured locally
- recent history is stored in the project
- reports are written to `BuildReports/`

If you also want uploads and dashboard features, configure an API key after install.

## Quick Start

1. Install the package
2. Open **Tools -> Build Metrics -> Build History**
3. Build your project once to generate the first local report
4. If you want cloud sync, open **Tools -> Build Metrics -> Setup Wizard**

## API Key Configuration

Priority order:

1. command-line argument `-BUILD_METRICS_API_KEY`
2. environment variable `BUILD_METRICS_API_KEY`
3. saved editor preference from the Setup Wizard

Use the Setup Wizard for local development, or environment / command-line configuration for CI.

## Menu

This package adds:

- **Tools -> Build Metrics -> Build History**
- **Tools -> Build Metrics -> Open Reports Folder**
- **Tools -> Build Metrics -> Documentation**
- **Tools -> Build Metrics -> Setup Wizard**
- **Tools -> Build Metrics -> Settings**
- **Tools -> Build Metrics -> Upload Last Build**
- **Tools -> Build Metrics -> Upload All Pending**
- **Tools -> Build Metrics -> View Dashboard**
- **Tools -> Build Metrics -> About**

## Documentation

- [Installation Guide](Documentation~/installation.md)
- [Configuration Guide](Documentation~/configuration.md)
- [CI/CD Overview](Documentation~/ci-cd/overview.md)
- [Offline Features](Documentation~/offline-features.md)

## Support

Email: `support@moonlightember.com`

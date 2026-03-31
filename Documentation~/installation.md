# Installation Guide

Build Metrics is distributed here as a single public UPM package that includes both local build analysis and cloud upload workflows.

## Requirements

- **Unity 2022.2 or newer**
- Windows, macOS, or Linux
- Git optional for commit metadata

## Install from Git

1. Open **Window -> Package Manager**
2. Click **+ -> Add package from git URL**
3. Paste:

```text
https://github.com/Alexartx/UnityBuildMetricsPlugin.git
```

4. Wait for Unity to resolve and compile the package

## Verify the Install

After install, open **Tools -> Build Metrics**.

You should see:

- Build History
- Open Reports Folder
- Documentation
- Setup Wizard
- Settings
- Upload Last Build
- Upload All Pending
- View Dashboard
- About

## Local Usage Without Cloud Setup

The package is usable before you configure an API key:

- builds are still captured locally
- reports are written to `BuildReports/`
- recent history is stored in `Library/BuildMetrics/history.json`

## Enable Cloud Features

If you want uploads and dashboard features:

1. Open **Tools -> Build Metrics -> Setup Wizard**
2. Enter or validate your API key
3. Leave auto-upload enabled if you want uploads after every successful build

You can also configure the package with:

- command-line argument `-BUILD_METRICS_API_KEY`
- environment variable `BUILD_METRICS_API_KEY`

## Troubleshooting

### Package not showing in the menu

1. Open **Window -> Console**
2. Resolve any existing project compile errors
3. Let Unity finish recompiling

### No uploads happen after setup

1. Confirm your API key is valid
2. Check **Tools -> Build Metrics -> Settings**
3. Confirm **Auto Upload** is enabled
4. Use **Upload Last Build** once to verify connectivity

## See Also

- [Configuration Guide](configuration.md)
- [Offline Features](offline-features.md)
- [CI/CD Overview](ci-cd/overview.md)

# Installation Guide

Complete installation instructions for Build Metrics Unity plugin.

## Requirements

- **Unity 2020.3 LTS or newer** (minimum)
- **Unity 2022.2 or newer** (recommended for full file breakdown)
- Windows, macOS, or Linux
- .NET Standard 2.1
- Internet connection (for cloud sync)
- Git (optional, for commit tracking)

---

## Installation Methods

### Method 1: UPM via Git URL (Recommended)

Fastest method with automatic updates.

**Steps:**

1. Open Unity Editor
2. Window → Package Manager
3. Click "+" → Add package from git URL
4. Enter URL:
   ```
   https://github.com/Alexartx/UnityBuildMetricsPlugin.git
   ```
5. Click "Add"

**Advantages:**
- ✅ Automatic updates
- ✅ Fastest installation
- ✅ No manual downloads

---

### Method 2: Unity Package (.unitypackage)

Manual installation for offline environments.

**Steps:**

1. Download latest `.unitypackage` from [Releases](https://github.com/Alexartx/UnityBuildMetricsPlugin/releases)
2. In Unity: Assets → Import Package → Custom Package
3. Select downloaded file
4. Click "Import All"
5. Wait for import to complete

**Advantages:**
- ✅ Works offline
- ✅ Full control over version
- ✅ No Git required

---

### Method 3: Unity Asset Store

*Coming soon*

---

## Verification

After installation, verify the plugin loaded correctly:

1. Check Unity menu: **Tools → Build Metrics**
2. You should see:
   - Setup Wizard
   - Settings
   - Build History
   - Upload Last Build

If menu items don't appear:
- Restart Unity Editor
- Check Console for errors
- Verify Unity version compatibility

---

## Next Steps

After installation:

1. [Get your API key](https://app.buildmetrics.moonlightember.com) from the dashboard
2. Configure the plugin (see [Configuration](configuration.md))
3. Build your project to start tracking metrics

---

## Uninstallation

### If installed via UPM:
1. Window → Package Manager
2. Find "Build Metrics" in package list
3. Click "Remove"

### If installed via .unitypackage:
1. Delete folder: `Assets/BuildMetrics/`
2. Delete meta file: `Assets/BuildMetrics.meta`

---

## Troubleshooting

### Package Manager shows "Error adding package"

**Cause:** Git not installed or not in PATH

**Solution:**
```bash
# Install Git
# macOS:
brew install git

# Windows:
# Download from https://git-scm.com/download/win

# Verify Git installation:
git --version
```

### Import fails with "Invalid package" error

**Cause:** Downloaded file corrupted

**Solution:**
- Re-download .unitypackage
- Clear browser cache
- Try different browser

### Plugin not showing in Tools menu

**Cause:** Unity didn't compile scripts

**Solution:**
1. Window → Console
2. Look for compilation errors
3. Fix any errors shown
4. Wait for Unity to recompile

---

## See Also

- [Configuration Guide](configuration.md)
- [Offline Features](offline-features.md)
- [CI/CD Integration](ci-cd/overview.md)

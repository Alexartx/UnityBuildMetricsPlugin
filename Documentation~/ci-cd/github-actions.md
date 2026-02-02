# GitHub Actions Integration

Complete guide for integrating Build Metrics with GitHub Actions using GameCI.

## Overview

Build Metrics works seamlessly with [GameCI](https://game.ci) - the most popular Unity CI/CD solution for GitHub Actions.

**Setup time:** 5-10 minutes
**Complexity:** Low
**Best for:** Developers new to CI/CD, using GitHub-hosted runners

---

## Prerequisites

### Unity License

GameCI requires a Unity license. Choose your license type:

<details>
<summary><b>Unity Personal (Free)</b></summary>

1. **Activate Unity Hub locally**
   - Open Unity Hub
   - Sign in and activate Personal license

2. **Find .ulf file:**
   - **Windows:** `C:\ProgramData\Unity\Unity_lic.ulf`
   - **macOS:** `~/.local/share/unity3d/Unity/Unity_lic.ulf`

3. **Add to GitHub Secrets:**
   ```
   Settings → Secrets → Actions → New repository secret

   Name: UNITY_LICENSE
   Value: <paste .ulf file content>

   Name: UNITY_EMAIL
   Value: your@email.com

   Name: UNITY_PASSWORD
   Value: yourpassword
   ```

**Note:** Base64 encoding recommended for .ulf file:
```bash
cat Unity_lic.ulf | base64 > unity_license.txt
# Then paste base64 content as UNITY_LICENSE secret
```

</details>

<details>
<summary><b>Unity Pro/Plus/Enterprise</b></summary>

**Modern (Named User License):**
```
Settings → Secrets → Actions → New repository secret

Name: UNITY_EMAIL
Value: your@email.com

Name: UNITY_PASSWORD
Value: yourpassword
```

**Legacy (Serial-Based):**
```
Settings → Secrets → Actions → New repository secret

Name: UNITY_SERIAL
Value: XX-XXXX-XXXX-XXXX-XXXX-XXXX

Name: UNITY_EMAIL
Value: your@email.com

Name: UNITY_PASSWORD
Value: yourpassword
```

</details>

### Build Metrics API Key

1. Sign up at [dashboard](https://app.buildmetrics.moonlightember.com)
2. Create project → Copy API key
3. Add to GitHub Secrets:
   ```
   Name: BUILD_METRICS_API_KEY
   Value: bm_your_api_key_here
   ```

---

## Quick Start

### Method 1: Using Our GitHub Action (Recommended)

Easiest setup with GameCI + Build Metrics pre-configured:

```yaml
# .github/workflows/build.yml
name: Unity Build

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true

      - uses: moonlightember/unity-build-metrics-action@v1
        with:
          unity-version: 'auto'
          build-target: Android
          build-metrics-api-key: ${{ secrets.BUILD_METRICS_API_KEY }}
```

**That's it!** Push to GitHub and your builds will automatically upload metrics.

---

### Method 2: Manual GameCI Setup

If you prefer to use GameCI directly:

```yaml
# .github/workflows/build.yml
name: Unity Build with Build Metrics

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest

    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      # Cache Library folder for 50%+ faster builds
      - uses: actions/cache@v3
        with:
          path: Library
          key: Library-${{ matrix.targetPlatform }}-${{ github.ref }}
          restore-keys: |
            Library-${{ matrix.targetPlatform }}-
            Library-

      # Checkout repository
      - uses: actions/checkout@v3
        with:
          lfs: true

      # Activate Unity license
      - uses: game-ci/unity-activate@v2
        with:
          unity-email: ${{ secrets.UNITY_EMAIL }}
          unity-password: ${{ secrets.UNITY_PASSWORD }}
          # For Pro/Enterprise with serial:
          # unity-serial: ${{ secrets.UNITY_SERIAL }}

      # Build project
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: Android
          unityVersion: 'auto'

      # Upload build artifacts
      - uses: actions/upload-artifact@v3
        with:
          name: Build-Android
          path: build/Android
```

**Key point:** The `BUILD_METRICS_API_KEY` env var at the job level makes the plugin auto-upload.

---

## Complete Example with Multiple Platforms

```yaml
# .github/workflows/build-all-platforms.yml
name: Build All Platforms

on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest

    strategy:
      fail-fast: false
      matrix:
        targetPlatform:
          - Android
          - iOS
          - WebGL
          - StandaloneWindows64

    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      - uses: actions/cache@v3
        with:
          path: Library
          key: Library-${{ matrix.targetPlatform }}
          restore-keys: Library-

      - uses: actions/checkout@v3
        with:
          lfs: true
          fetch-depth: 0  # For git commit tracking

      - uses: game-ci/unity-activate@v2
        with:
          unity-email: ${{ secrets.UNITY_EMAIL }}
          unity-password: ${{ secrets.UNITY_PASSWORD }}

      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: ${{ matrix.targetPlatform }}
          buildName: MyGame-${{ matrix.targetPlatform }}

      - uses: actions/upload-artifact@v3
        with:
          name: Build-${{ matrix.targetPlatform }}
          path: build/${{ matrix.targetPlatform }}
```

Each platform build will upload metrics separately to your dashboard.

---

## How It Works

```
GitHub Actions Workflow Starts
    ↓
Set BUILD_METRICS_API_KEY environment variable
    ↓
GameCI builds Unity project
    ↓
Unity build completes successfully
    ↓
Build Metrics post-build hook fires
    ↓
Plugin reads BUILD_METRICS_API_KEY from environment
    ↓
Metrics uploaded to API
    ↓
View results in dashboard
```

**Total overhead:** ~1-2 seconds per build

---

## Advanced Configuration

### Custom Build Method

If using a custom C# build method:

```yaml
- uses: game-ci/unity-builder@v4
  with:
    targetPlatform: Android
    buildScript: Assets/Editor/BuildScripts/CustomBuild.cs
    customParameters: -profile Release -enableAddressables
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

### Disable Auto-Upload

If you want manual control:

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
  BUILD_METRICS_AUTO_UPLOAD: false
```

Then use Unity menu: `Tools → Build Metrics → Upload Last Build`

### Verbose Logging

Debug upload issues:

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
  BUILD_METRICS_VERBOSE: true
```

---

## Troubleshooting

### License Activation Fails

**Unity Personal:**
- Verify .ulf file is valid (re-generate if needed)
- Check `UNITY_EMAIL` and `UNITY_PASSWORD` match

**Unity Pro/Plus/Enterprise:**
- Verify serial number is correct
- Check license hasn't expired
- Ensure enough activations available

**Solution:** Enable retry with exponential backoff:
```yaml
- uses: game-ci/unity-activate@v2
  with:
    retries: 3
```

### Build Metrics Not Uploading

**Check 1:** Verify secret exists
```yaml
- name: Debug
  run: |
    if [ -z "$BUILD_METRICS_API_KEY" ]; then
      echo "API key not set"
    else
      echo "API key is set (length: ${#BUILD_METRICS_API_KEY})"
    fi
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

**Check 2:** View Unity logs
```yaml
- uses: game-ci/unity-builder@v4
  with:
    targetPlatform: Android
    buildOptions: ShowBuiltPlayer
```

**Check 3:** Network access
- Ensure runner can reach `buildmetrics-api.onrender.com`
- Check firewall rules

### "Invalid API Key" Error

**Solution:**
1. Regenerate API key from dashboard
2. Update GitHub secret
3. Rebuild

### Builds Slow / Timing Out

**Solution:** Enable caching (shown in examples above)

**Expected speedup:** 50-70% faster builds after first run

---

## Best Practices

### ✅ DO

- Cache `Library` folder (massive speedup)
- Use `fetch-depth: 0` for git tracking
- Use matrix builds for multiple platforms
- Enable LFS if using large assets
- Store all secrets in GitHub Secrets

### ❌ DON'T

- Hardcode API keys in workflow
- Skip caching (very slow)
- Use `ubuntu-latest` for macOS IL2CPP builds
- Commit Unity license files

---

## Performance Tips

### 1. Enable Library Caching

```yaml
- uses: actions/cache@v3
  with:
    path: Library
    key: Library-${{ matrix.targetPlatform }}
    restore-keys: Library-
```

**Speedup:** 50-70%

### 2. Use Auto Unity Version

```yaml
- uses: game-ci/unity-builder@v4
  with:
    unityVersion: 'auto'  # Reads from ProjectVersion.txt
```

**Benefit:** No manual updates needed

### 3. Parallelize Platform Builds

```yaml
strategy:
  matrix:
    targetPlatform: [Android, iOS, WebGL]
```

**Speedup:** 3x faster (builds run in parallel)

---

## Example Repositories

- [Basic GameCI + Build Metrics](../../Samples~/workflows/basic-github-action.yml)
- [Multi-platform Matrix Build](../../Samples~/workflows/gameci.yml)
- [Advanced with Custom Build Script](https://moonlightember.com/products/build-metrics/examples)

---

## Next Steps

1. Copy workflow from examples
2. Add required secrets to GitHub
3. Push to trigger build
4. View metrics in [dashboard](https://app.buildmetrics.moonlightember.com/dashboard/builds)

---

## See Also

- [GitHub Actions Self-Hosted](github-actions-self-hosted.md) - For self-hosted runners
- [GameCI Documentation](https://game.ci/docs)
- [Unity Builder Reference](https://game.ci/docs/github/builder)
- [Configuration Guide](../configuration.md)

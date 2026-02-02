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

Unity Personal licenses require a `.ulf` license file for CI/CD activation.

**How to get your .ulf file:**

1. Open Unity Hub → Manage Licenses
2. Click "Manual Activation"
3. Save the `.alf` file
4. Upload to [Unity Manual License](https://license.unity3d.com/manual)
5. Download the `.ulf` file
6. Copy its entire contents into the `UNITY_LICENSE` secret

**Add these 3 secrets to GitHub:**
```
Settings → Secrets → Actions → New repository secret

Name: UNITY_EMAIL
Value: your@email.com

Name: UNITY_PASSWORD
Value: yourpassword

Name: UNITY_LICENSE
Value: <paste entire .ulf file contents>
```

📖 **Detailed guide:** [GameCI Activation Documentation](https://game.ci/docs/github/activation/)

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
      - uses: actions/checkout@v4
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

          # Unity Pro/Plus (Paid) - Comment out above, uncomment below
          # UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          # UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
          # UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}
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

**Key points:**
- `customParameters` passes Build Metrics API key to Unity (env vars don't reach Unity in Docker)
- Disk cleanup prevents "No space left on device" errors
- GameCI v4 handles license activation automatically

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

    steps:
      - name: Free up runner disk space
        run: |
          df -h
          sudo docker image prune --all --force
          sudo docker builder prune --all --force
          sudo rm -rf /opt/hostedtoolcache/*
          sudo apt clean
          df -h

      - uses: actions/checkout@v4
        with:
          lfs: true
          fetch-depth: 0  # For git commit tracking

      - uses: game-ci/unity-builder@v4
        env:
          BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          customParameters: "-development -BUILD_METRICS_API_KEY ${{ secrets.BUILD_METRICS_API_KEY }}"
          targetPlatform: ${{ matrix.targetPlatform }}
          buildName: MyGame-${{ matrix.targetPlatform }}
          unityVersion: auto

      - uses: actions/upload-artifact@v4
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
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
    UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
    UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  with:
    targetPlatform: Android
    buildScript: Assets/Editor/BuildScripts/CustomBuild.cs
    customParameters: -profile Release -enableAddressables -BUILD_METRICS_API_KEY ${{ secrets.BUILD_METRICS_API_KEY }}
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

**Solution:** Ensure disk cleanup is enabled (shown in examples above)

**Why:** GitHub runners have limited disk space (~14GB). GameCI Docker images are large, so disk cleanup prevents "No space left on device" errors.

---

## Best Practices

### ✅ DO

- Free up disk space before building (prevents errors)
- Use `fetch-depth: 0` for git tracking
- Use matrix builds for multiple platforms
- Enable LFS if using large assets
- Store all secrets in GitHub Secrets
- Pass Build Metrics API key via `customParameters`

### ❌ DON'T

- Hardcode API keys in workflow
- Use Library caching with GameCI (causes disk space issues)
- Commit Unity license files
- Forget to add UNITY_LICENSE env var

---

## Performance Tips

### 1. Free Up Disk Space (Critical)

```yaml
- name: Free up runner disk space
  run: |
    df -h
    sudo docker image prune --all --force
    sudo docker builder prune --all --force
    sudo rm -rf /opt/hostedtoolcache/*
    sudo apt clean
    df -h
```

**Why:** Prevents "No space left on device" errors with GameCI Docker images

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

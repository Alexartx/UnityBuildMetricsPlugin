# GitHub Actions with Self-Hosted Runners

Guide for integrating Build Metrics with existing GitHub Actions workflows using self-hosted runners and direct Unity CLI builds.

## Overview

If you have existing GitHub Actions workflows using:
- ✅ Self-hosted macOS/Windows/Linux runners
- ✅ Direct Unity CLI builds (not GameCI)
- ✅ Custom build methods
- ✅ Complex pipelines (Addressables, Firebase, S3, etc.)

**Integration requires ONE line of code.**

---

## Prerequisites

- ✅ Self-hosted runner with Unity installed
- ✅ Existing working build workflow
- ✅ Build Metrics plugin installed via UPM
- ✅ API key from [dashboard](https://app.buildmetrics.moonlightember.com)

---

## Quick Start (3 Steps)

### Step 1: Add API Key Secret

```
GitHub repo → Settings → Secrets and variables → Actions
Click "New repository secret"

Name: BUILD_METRICS_API_KEY
Value: bm_your_api_key_here
```

### Step 2: Add Environment Variable to Workflow

Add **one line** to your existing workflow:

```yaml
jobs:
  build:
    runs-on: [self-hosted, macOS, Unity]

    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}  # ← ADD THIS

    steps:
      # Your existing build steps work unchanged
      - uses: actions/checkout@v3
      - name: Build
        run: |
          /Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity \
            -quit -batchmode -nographics \
            -projectPath . \
            -executeMethod YourBuildMethod.Build \
            -logFile -
```

### Step 3: Build and Verify

1. Push changes
2. GitHub Actions runs build
3. Unity build completes
4. Build Metrics auto-uploads
5. View results in [dashboard](https://app.buildmetrics.moonlightember.com)

**That's it!** No changes to build scripts, build methods, or custom pipeline logic.

---

## Complete Example (Android Build)

This is a real-world example similar to what many teams use:

```yaml
# .github/workflows/build_android.yml
name: Build Android

on:
  push:
    branches: [main, develop]
  workflow_dispatch:

jobs:
  build-android:
    runs-on: [self-hosted, macOS, Unity, Mac-Mini-1]
    timeout-minutes: 60

    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      - name: Checkout
        uses: actions/checkout@v3
        with:
          lfs: true
          fetch-depth: 0  # For git commit tracking

      - name: Build Android with Unity
        run: |
          /Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity \
            -quit -batchmode -nographics \
            -projectPath . \
            -executeMethod BuildConfiguration.Profiles.AndroidBuildProfile.Build \
            -logFile -

      # Your existing custom steps continue unchanged
      - name: Upload to Firebase
        run: |
          fastlane android firebase

      - name: Upload to S3
        run: |
          aws s3 cp ./Builds/Android s3://your-bucket/android/ --recursive

      - name: Invalidate CloudFront
        run: |
          aws cloudfront create-invalidation --distribution-id YOUR_ID --paths "/*"

      - name: Commit Addressables
        if: ${{ github.ref == 'refs/heads/main' }}
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add ServerData/
          git commit -m "Update Addressables content state [skip ci]"
          git push
```

**Result:** Build Metrics uploads happen automatically after Unity build completes, before your custom upload steps.

---

## Complete Example (iOS Build)

```yaml
# .github/workflows/build_ios.yml
name: Build iOS

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build-ios:
    runs-on: [self-hosted, macOS, Unity]
    timeout-minutes: 90

    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true

      - name: Build iOS with Unity
        run: |
          /Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity \
            -quit -batchmode -nographics \
            -projectPath . \
            -executeMethod BuildConfiguration.Profiles.iOSBuildProfile.Build \
            -logFile -

      - name: Build Xcode Project
        run: |
          xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj \
            -scheme Unity-iPhone \
            -configuration Release \
            archive -archivePath build.xcarchive

      - name: Export IPA
        run: |
          xcodebuild -exportArchive \
            -archivePath build.xcarchive \
            -exportPath Builds/iOS \
            -exportOptionsPlist ExportOptions.plist

      - name: Upload to TestFlight
        run: |
          xcrun altool --upload-app \
            --type ios \
            --file Builds/iOS/YourGame.ipa \
            --username "${{ secrets.APPLE_ID }}" \
            --password "${{ secrets.APPLE_APP_PASSWORD }}"

      - name: Upload Addressables to S3
        run: |
          aws s3 sync ./ServerData s3://your-bucket/addressables/ --delete
```

---

## How It Works

```
GitHub Actions Workflow Starts
    ↓
Environment variable set: BUILD_METRICS_API_KEY
    ↓
Unity CLI build executes (your custom method)
    ↓
Your build method completes successfully
    ↓
Unity triggers IPostprocessBuildWithReport callbacks
    ↓
Build Metrics plugin callback fires
    ↓
Plugin reads BUILD_METRICS_API_KEY from environment
    ↓
Plugin collects metrics (size, time, git, files, etc.)
    ↓
Plugin uploads to API (async, non-blocking)
    ↓
Unity exits
    ↓
Your workflow continues (Firebase, S3, etc.)
```

**Total overhead:** ~1-2 seconds
**Impact on pipeline:** Zero - happens in background

---

## Multi-Runner Setup

If you have multiple self-hosted runners:

```yaml
jobs:
  build-android:
    runs-on: [self-hosted, macOS, Unity, Mac-Mini-1]
    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
    # ... Android build steps

  build-ios:
    runs-on: [self-hosted, macOS, Unity, Mac-Mini-2]
    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
    # ... iOS build steps
```

**Each runner tracks separately** - you'll see machine fingerprints in dashboard for time baselines.

---

## Advanced Configuration

### Custom API Endpoint (Self-Hosted API)

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
  BUILD_METRICS_API_URL: https://your-api.example.com
```

### Disable Auto-Upload (Manual Mode)

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
  BUILD_METRICS_AUTO_UPLOAD: false
```

Then trigger manually:
```bash
# In Unity Editor menu
Tools → Build Metrics → Upload Last Build
```

### Verbose Logging

Debug upload issues:

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
  BUILD_METRICS_VERBOSE: true
```

Unity console will show detailed logs:
```
[BuildMetrics] Reading API key from environment
[BuildMetrics] API key validated (starts with bm_)
[BuildMetrics] Collecting build metrics...
[BuildMetrics] Upload successful (200 OK)
```

---

## Platform-Specific Notes

### macOS Runners

Unity path format:
```bash
/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity
```

Example:
```bash
/Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity
```

### Windows Runners

Unity path format:
```powershell
C:\Program Files\Unity\Hub\Editor\{version}\Editor\Unity.exe
```

Example workflow:
```yaml
- name: Build
  run: |
    "C:\Program Files\Unity\Hub\Editor\2023.2.20f1\Editor\Unity.exe" `
      -quit -batchmode -nographics `
      -projectPath . `
      -executeMethod YourBuildMethod.Build `
      -logFile -
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

### Linux Runners

Unity path format:
```bash
/opt/unity/Editor/Unity
```

Or if using Unity Hub:
```bash
~/Unity/Hub/Editor/{version}/Editor/Unity
```

---

## Network Requirements

### Firewall / Proxy

If runner is behind corporate firewall:

**Whitelist domain:**
```
buildmetrics-api.onrender.com
```

**Ports:**
```
HTTPS: 443
```

**IP ranges (if needed):**
Contact support@moonlightember.com for current IP addresses

### Proxy Configuration

If using HTTP proxy:

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
  HTTP_PROXY: http://proxy.company.com:8080
  HTTPS_PROXY: http://proxy.company.com:8080
```

---

## Troubleshooting

### Plugin Doesn't Upload

**Check 1: Verify env var is set**
```yaml
- name: Debug Environment
  run: |
    if [ -z "$BUILD_METRICS_API_KEY" ]; then
      echo "❌ API key not set"
    else
      echo "✅ API key is set (starts with: ${BUILD_METRICS_API_KEY:0:5}...)"
    fi
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

**Check 2: Verify Unity sees env var**
```yaml
- name: Build
  run: |
    echo "Env var length: ${#BUILD_METRICS_API_KEY}"
    /Applications/Unity/.../Unity -quit -batchmode ...
  env:
    BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

**Check 3: Check Unity logs**

Add `-logFile unity.log` to Unity command, then:
```yaml
- name: Show Unity Logs
  if: always()
  run: cat unity.log | grep -i "buildmetrics"
```

### Network Timeout

**Symptoms:**
- Upload takes >30 seconds
- "Connection timeout" in logs

**Solutions:**
1. Check network connectivity from runner
2. Verify DNS resolves `buildmetrics-api.onrender.com`
3. Check firewall rules
4. Try from runner shell:
   ```bash
   curl -I https://buildmetrics-api.onrender.com/health
   ```

### Upload Fails with 401 Unauthorized

**Cause:** Invalid API key

**Solutions:**
1. Verify key in dashboard: Settings → API Keys
2. Check key starts with `bm_`
3. Regenerate key if needed
4. Update GitHub secret
5. Rebuild

### Git Information Not Captured

**Cause:** Shallow clone (`fetch-depth: 1`)

**Solution:**
```yaml
- uses: actions/checkout@v3
  with:
    fetch-depth: 0  # Full git history
```

---

## Best Practices

### ✅ DO

- Set env var at job level (applies to all steps)
- Use `fetch-depth: 0` for git tracking
- Enable verbose logging for debugging
- Monitor dashboard for failed uploads
- Use separate API keys for dev/prod

### ❌ DON'T

- Hardcode API key in workflow
- Set env var only at step level (Unity won't see it)
- Skip network access verification
- Ignore failed uploads silently

---

## Performance Impact

**Build time overhead:** ~1-2 seconds

Breakdown:
- Collect metrics: <0.5s
- Upload to API: 0.5-1.5s
- Total: Negligible for any real build

**No impact on:**
- Build output quality
- Build process
- Compiled code
- Your custom pipeline steps

**Async upload:** Happens in background while Unity exits

---

## Integration with Existing Tools

### Works With

- ✅ Fastlane (Android/iOS deployment)
- ✅ AWS CLI (S3, CloudFront)
- ✅ Firebase CLI (distribution)
- ✅ TestFlight uploads
- ✅ Custom deployment scripts
- ✅ Addressables workflows
- ✅ Any post-build automation

**Build Metrics runs BEFORE your custom steps** - it doesn't interfere with anything downstream.

---

## Example: Complete Android + iOS Pipeline

```yaml
# .github/workflows/build-all.yml
name: Build All Platforms

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build-android:
    runs-on: [self-hosted, macOS, Mac-Mini-1]
    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true
          fetch-depth: 0

      - name: Build Android
        run: |
          /Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity \
            -quit -batchmode -nographics \
            -projectPath . \
            -executeMethod BuildConfiguration.Profiles.AndroidBuildProfile.Build

      - name: Upload to Firebase
        run: fastlane android firebase

  build-ios:
    runs-on: [self-hosted, macOS, Mac-Mini-2]
    env:
      BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}

    steps:
      - uses: actions/checkout@v3
        with:
          lfs: true
          fetch-depth: 0

      - name: Build iOS
        run: |
          /Applications/Unity/Hub/Editor/6000.0.52f1/Unity.app/Contents/MacOS/Unity \
            -quit -batchmode -nographics \
            -projectPath . \
            -executeMethod BuildConfiguration.Profiles.iOSBuildProfile.Build

      - name: Build & Export IPA
        run: fastlane ios beta

      - name: Upload to TestFlight
        run: fastlane ios testflight
```

Both platforms upload metrics independently. Dashboard shows separate builds for Android and iOS.

---

## Next Steps

1. Add `BUILD_METRICS_API_KEY` to GitHub Secrets
2. Add `env:` line to your workflow
3. Push and verify build uploads
4. View metrics in [dashboard](https://app.buildmetrics.moonlightember.com)

Need help? Contact support@moonlightember.com

---

## See Also

- [GitHub Actions (GameCI)](github-actions.md) - For hosted runners
- [GitLab CI](gitlab-ci.md) - Similar setup for GitLab
- [Jenkins](jenkins.md) - Similar setup for Jenkins
- [Configuration Guide](../configuration.md)

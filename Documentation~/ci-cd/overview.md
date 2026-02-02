# CI/CD Integration Overview

Build Metrics integrates with GitHub Actions and other CI/CD platforms through command line arguments or environment variables.

## How It Works

```
CI/CD Pipeline
    ↓
Set BUILD_METRICS_API_KEY environment variable
    ↓
Unity builds your project
    ↓
Build Metrics plugin auto-detects API key
    ↓
Metrics uploaded automatically
    ↓
View results in dashboard
```

**No code changes required** - the plugin handles everything automatically.

---

## Quick Start

**3 steps for any platform:**

1. **Get API Key**
   - Sign up at [dashboard](https://app.buildmetrics.moonlightember.com)
   - Create project → Copy API key

2. **Add Secret/Variable**
   - Add `BUILD_METRICS_API_KEY` to your CI platform
   - Value: `bm_your_api_key_here`

3. **Build**
   - Run your existing build
   - Metrics upload automatically

---

## Supported Platforms

### ✅ Verified & Documented

- **[GitHub Actions](github-actions.md)** - GameCI with hosted runners
- **[Self-Hosted GitHub Actions](github-actions-self-hosted.md)** - Your own build machines

### 🤝 Other CI/CD Systems

Build Metrics should work with any CI/CD platform that supports:
- Command line arguments (GameCI-style): `-BUILD_METRICS_API_KEY your_key`
- OR environment variables: `BUILD_METRICS_API_KEY=your_key`

**Platforms that should work** (untested):
- GitLab CI
- Jenkins
- CircleCI
- Azure Pipelines
- TeamCity
- Bitbucket Pipelines
- Unity Cloud Build
- Custom build servers

**Need help?** Contact us: support@moonlightember.com

---

## Integration Paths

### Path 1: GitHub Actions (Easiest)

**Best for:** New to CI/CD, using GitHub

Use our official GitHub Action wrapper:

```yaml
- uses: moonlightember/unity-build-metrics-action@v1
  with:
    unity-version: 'auto'
    build-target: Android
    build-metrics-api-key: ${{ secrets.BUILD_METRICS_API_KEY }}
```

**Docs:** [GitHub Actions Guide](github-actions.md)

---

### Path 2: Direct Unity CLI (Most Common)

**Best for:** Existing CI/CD, any platform

Just add environment variable to your existing workflow:

```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.YOUR_API_KEY }}
```

**Examples:**
- [GitHub Actions (Self-Hosted)](github-actions-self-hosted.md)

---

### Path 3: Programmatic (Advanced)

**Best for:** Custom build systems, enterprise

Call the Build Metrics API from your C# build scripts:

```csharp
// In your custom build method
BuildMetricsUploader.UploadBuildReport(report);
```

**Docs:** [Advanced Integration](https://moonlightember.com/products/build-metrics/docs/advanced/programmatic-api)

---

## Choose Your Setup

### GitHub Actions (Recommended)

**Option 1: GameCI (Hosted Runners)** ✅ Verified
→ [GitHub Actions with GameCI](github-actions.md)

**Option 2: Self-Hosted Runners** ⚠️ Untested
→ [GitHub Actions Self-Hosted](github-actions-self-hosted.md)

---

### Other CI/CD Platforms

**Already have a CI/CD workflow?** Just add these lines to enable Build Metrics:

**⚠️ First: Add Your API Key as a Secret**

Before using these examples, add `BUILD_METRICS_API_KEY` to your CI platform's secrets/variables:
- **GitHub:** Settings → Secrets and variables → Actions
- **GitLab:** Settings → CI/CD → Variables
- **Jenkins:** Credentials → Add Credentials
- **CircleCI:** Project Settings → Environment Variables

---

#### GitLab CI

```yaml
# Add to your existing .gitlab-ci.yml

variables:
  BUILD_METRICS_API_KEY: $BUILD_METRICS_API_KEY  # ← Add this

build:
  script:
    - unity-editor \
        -batchmode \
        -buildTarget Android \
        -BUILD_METRICS_API_KEY $BUILD_METRICS_API_KEY  # ← Add this
```

#### Jenkins

```groovy
// Add to your existing Jenkinsfile

environment {
  BUILD_METRICS_API_KEY = credentials('build-metrics-api-key')  // ← Add this
}

stage('Build') {
  sh """
    unity-editor -batchmode \
      -buildTarget Android \
      -BUILD_METRICS_API_KEY ${BUILD_METRICS_API_KEY}  // ← Add this
  """
}
```

#### CircleCI

```yaml
# Add to your existing .circleci/config.yml

version: 2.1

jobs:
  build:
    environment:
      BUILD_METRICS_API_KEY: ${BUILD_METRICS_API_KEY}  # ← Add this
    steps:
      - run: |
          unity-editor -batchmode \
            -buildTarget Android \
            -BUILD_METRICS_API_KEY $BUILD_METRICS_API_KEY  # ← Add this
```

#### Azure Pipelines

```yaml
# Add to your existing azure-pipelines.yml

variables:
  BUILD_METRICS_API_KEY: $(BUILD_METRICS_API_KEY)  # ← Add this

steps:
  - script: |
      unity-editor -batchmode \
        -buildTarget Android \
        -BUILD_METRICS_API_KEY $(BUILD_METRICS_API_KEY)  # ← Add this
```

#### Unity Cloud Build

```
# Add to Build Target settings (Pre-Export Method or Environment Variables)

Environment Variable:
  BUILD_METRICS_API_KEY = your_api_key_here  # ← Add this

# Metrics upload automatically after build completes
```

#### Custom Build Scripts

```bash
# Add to your existing build script

export BUILD_METRICS_API_KEY="your_api_key_here"  # ← Add this

unity-editor -batchmode \
  -buildTarget Android \
  -BUILD_METRICS_API_KEY $BUILD_METRICS_API_KEY  # ← Add this
```

**Key points:**
- ✅ Works with **any** CI/CD platform
- ✅ Add 2 lines to your existing workflow
- ✅ No code changes in Unity required
- ✅ Metrics upload automatically

**Need setup help?** Contact: support@moonlightember.com

---

## Requirements

### Minimum Requirements

- ✅ Unity 2020.3 LTS or newer
- ✅ Build Metrics plugin installed
- ✅ API key from dashboard
- ✅ Network access to `buildmetrics-api.onrender.com`

### Optional (Recommended)

- Git installed (for commit tracking)
- Unity 2022.2+ (for full file breakdown)

---

## Environment Variables

### Required

```bash
BUILD_METRICS_API_KEY=bm_your_api_key_here
```

### Optional

```bash
# Custom API endpoint (self-hosted)
BUILD_METRICS_API_URL=https://your-api.example.com

# Disable auto-upload
BUILD_METRICS_AUTO_UPLOAD=false

# Enable verbose logging
BUILD_METRICS_VERBOSE=true
```

---

## Security Best Practices

### ✅ DO

- Store API keys in platform secrets/variables
- Use separate keys for dev/staging/production
- Rotate keys periodically
- Restrict API key permissions in dashboard

### ❌ DON'T

- Hardcode API keys in scripts
- Commit keys to Git
- Share keys via chat/email
- Use same key across projects

---

## Verification

### After Setup

1. **Trigger a build** in your CI/CD
2. **Check CI logs** for:
   ```
   [BuildMetrics] Build metrics sent successfully
   ```
3. **View dashboard** - build should appear within seconds

### Debug Failed Uploads

Enable verbose logging:

```bash
BUILD_METRICS_VERBOSE=true
```

Check logs for:
- API key validation
- Network connectivity
- Upload response

---

## Common Issues

### "Invalid API key" error

**Solution:**
- Verify key starts with `bm_`
- Check for typos in secret name
- Ensure secret is available to build step

### "Network timeout" error

**Solution:**
- Check firewall rules
- Verify `buildmetrics-api.onrender.com` is accessible
- Add to allowlist if needed

### Metrics not uploading

**Solution:**
- Verify env var is set: `echo $BUILD_METRICS_API_KEY`
- Check Unity console logs
- Enable verbose mode for details

---

## Performance Impact

**Build time overhead:** ~1-2 seconds

Breakdown:
- Collect metrics: <0.5s
- Upload to API: 0.5-1.5s
- Total: Negligible

**No impact on:**
- Build output
- Build process
- Compiled code

---

## Next Steps

1. Choose your platform from the list above
2. Follow platform-specific guide
3. Add `BUILD_METRICS_API_KEY` secret
4. Build and verify

Need help? Contact support@moonlightember.com

---

## See Also

- [GitHub Actions Guide](github-actions.md)
- [GitHub Actions (Self-Hosted)](github-actions-self-hosted.md)
- [Configuration Guide](../configuration.md)

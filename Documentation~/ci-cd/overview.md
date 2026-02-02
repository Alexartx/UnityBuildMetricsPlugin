# CI/CD Integration Overview

Build Metrics integrates seamlessly with all major CI/CD platforms through a simple environment variable.

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

### Cloud CI/CD

- ✅ [GitHub Actions](github-actions.md) - Hosted & self-hosted runners
- ✅ [GitLab CI](gitlab-ci.md) - GitLab.com & self-hosted
- ✅ [CircleCI](https://moonlightember.com/products/build-metrics/docs/ci-cd/circleci)
- ✅ [Jenkins](jenkins.md) - Any version
- ✅ [Azure Pipelines](https://moonlightember.com/products/build-metrics/docs/ci-cd/azure-pipelines)
- ✅ [TeamCity](https://moonlightember.com/products/build-metrics/docs/ci-cd/teamcity)
- ✅ [Bitbucket Pipelines](https://moonlightember.com/products/build-metrics/docs/ci-cd/bitbucket)

### Unity Platforms

- ✅ [Unity Cloud Build](unity-cloud-build.md)
- ✅ Unity Build Automation (DevOps)

### Custom

- ✅ Any platform that can set environment variables
- ✅ Local build scripts
- ✅ Custom build servers

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
- [GitLab CI](gitlab-ci.md)
- [Jenkins](jenkins.md)
- [Unity Cloud Build](unity-cloud-build.md)

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

## Choose Your Platform

### GitHub Actions

**Scenario 1: Using GameCI**
→ [GitHub Actions with GameCI](github-actions.md)

**Scenario 2: Self-hosted runners + direct Unity CLI**
→ [GitHub Actions Self-Hosted](github-actions-self-hosted.md)

**Scenario 3: Fresh setup**
→ [GitHub Actions Quick Start](github-actions.md)

---

### GitLab CI

**Scenario 1: GitLab.com (cloud)**
→ [GitLab CI Guide](gitlab-ci.md)

**Scenario 2: Self-hosted GitLab**
→ [GitLab CI Guide](gitlab-ci.md) (same setup)

---

### Jenkins

**Scenario 1: Freestyle project**
→ [Jenkins Freestyle](jenkins.md#freestyle)

**Scenario 2: Pipeline (Jenkinsfile)**
→ [Jenkins Pipeline](jenkins.md#pipeline)

---

### Unity Cloud Build

**Scenario: Using Unity's hosted CI**
→ [Unity Cloud Build Guide](unity-cloud-build.md)

---

### Other Platforms

**CircleCI, Azure Pipelines, TeamCity, etc.**

All follow the same pattern:
1. Add `BUILD_METRICS_API_KEY` to secrets/variables
2. Ensure it's available during Unity build
3. Build as usual

Full guides available at: [Website Docs](https://moonlightember.com/products/build-metrics/docs/ci-cd-integration)

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
- [GitLab CI Guide](gitlab-ci.md)
- [Jenkins Guide](jenkins.md)
- [Unity Cloud Build Guide](unity-cloud-build.md)
- [Configuration Guide](../configuration.md)

# Unity Cloud Build Integration

Quick guide for integrating Build Metrics with Unity Cloud Build.

## Quick Start

### Step 1: Get API Key

1. Sign up at [dashboard](https://app.buildmetrics.moonlightember.com)
2. Create project
3. Copy API key

### Step 2: Add Environment Variable

```
Unity Dashboard → DevOps → Cloud Build → Config

Environment Variables → Add Variable

Name: BUILD_METRICS_API_KEY
Value: bm_your_api_key_here
```

### Step 3: Build

Trigger build → Metrics upload automatically.

View in [dashboard](https://app.buildmetrics.moonlightember.com).

---

## Complete Setup

### Configure Build Target

```
1. Unity Dashboard → DevOps → Cloud Build
2. Select project
3. Add build target (Android, iOS, etc.)
4. Configure:
   - Unity version: 2022.3.20f1 (or your version)
   - Build target: Android
   - Advanced Settings → Environment Variables:
     BUILD_METRICS_API_KEY = bm_your_key_here
```

### Enable Git Tracking

For commit information:

```
Advanced Settings:
[x] Clean checkout
[x] Fetch git data
```

### Custom Build Script (Optional)

If using custom build method:

```
Advanced Settings → Build Script Path:
Assets/Editor/BuildScripts/CloudBuild.cs

Environment Variables:
BUILD_METRICS_API_KEY = bm_your_key
```

---

## Multiple Build Targets

Each target gets separate metrics:

```
Targets:
- Android-Dev (BUILD_METRICS_API_KEY set)
- Android-Prod (BUILD_METRICS_API_KEY set)
- iOS-Dev (BUILD_METRICS_API_KEY set)
- iOS-Prod (BUILD_METRICS_API_KEY set)
```

Dashboard will show separate builds for each configuration.

---

## Troubleshooting

### Metrics not uploading

**Check:**
1. Environment variable name is exact: `BUILD_METRICS_API_KEY`
2. API key starts with `bm_`
3. Build completed successfully
4. Check Unity Cloud Build logs for errors

### Network issues

Unity Cloud Build has full internet access - no firewall configuration needed.

---

## See Also

- [Configuration Guide](../configuration.md)
- [GitHub Actions Guide](github-actions.md)
- [GitLab CI Guide](gitlab-ci.md)

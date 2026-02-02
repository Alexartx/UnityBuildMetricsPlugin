# Configuration Guide

How to configure Build Metrics after installation.

## Quick Setup

**Fastest way (5 minutes):**

1. Get API key from [dashboard](https://app.buildmetrics.moonlightember.com)
2. In Unity: **Tools → Build Metrics → Setup Wizard**
3. Paste API key → Click "Complete Setup"
4. Done! Build your project to test

---

## API Key Configuration

Build Metrics supports two configuration methods:

### Method 1: EditorPrefs (Per-Developer) ✅ Recommended

**Best for:**
- Individual developers
- Per-developer tracking
- Local development

**How it works:**
- API key stored locally on developer's machine
- Never committed to Git
- Each developer uses their own key

**Setup:**

```
Tools → Build Metrics → Setup Wizard
1. Enter your API key
2. Click "Complete Setup"
```

**Storage location:**
- macOS: `~/Library/Preferences/com.unity3d.UnityEditor5.x.plist`
- Windows: Registry (`HKCU\Software\Unity Technologies\Unity Editor 5.x`)
- Linux: `~/.config/unity3d/prefs`

---

### Method 2: Environment Variable (Team/CI)

**Best for:**
- Team collaboration
- CI/CD pipelines
- Shared build machines

**How it works:**
- API key read from `BUILD_METRICS_API_KEY` environment variable
- Environment variable takes priority over EditorPrefs
- Same key used by all team members

**Setup:**

**macOS/Linux:**
```bash
# Add to ~/.zshrc or ~/.bashrc
export BUILD_METRICS_API_KEY="bm_your_api_key_here"

# Apply changes
source ~/.zshrc  # or source ~/.bashrc
```

**Windows:**
```powershell
# PowerShell (persistent)
[System.Environment]::SetEnvironmentVariable('BUILD_METRICS_API_KEY', 'bm_your_api_key_here', 'User')

# Restart Unity after setting
```

**GitHub Actions:**
```yaml
env:
  BUILD_METRICS_API_KEY: ${{ secrets.BUILD_METRICS_API_KEY }}
```

**Verification:**
- In Unity: **Tools → Build Metrics → Settings**
- Should show: "✓ Using API key from environment variable"

---

## Settings Panel

Access via: **Tools → Build Metrics → Settings**

### Available Options

**API Key**
- Your unique identifier
- Can be set here or via environment variable
- Starts with `bm_`

**Auto Upload**
- Default: Enabled
- When enabled: Metrics sent automatically after each build
- When disabled: Use "Tools → Build Metrics → Upload Last Build" manually

**API URL** (Advanced)
- Default: `https://buildmetrics-api.onrender.com`
- Change only if using self-hosted API

---

## Advanced Configuration

### Disable Auto-Upload

If you want manual control over uploads:

```
Tools → Build Metrics → Settings
→ Uncheck "Auto Upload"
→ Click "Save"
```

Then use:
```
Tools → Build Metrics → Upload Last Build
```

### Custom API Endpoint

For self-hosted API:

```csharp
// Set via environment variable
BUILD_METRICS_API_URL=https://your-api.example.com
```

---

## API Key Management

### Finding Your API Key

1. Go to [dashboard](https://app.buildmetrics.moonlightember.com/dashboard/keys)
2. Select your project
3. Click "Copy API Key"

### Generating New Key

1. Dashboard → API Keys
2. Select project
3. Enter label (e.g., "My Laptop")
4. Click "Generate API Key"
5. **Copy immediately** (shown only once)

### Revoking Key

If key is compromised:

1. Dashboard → API Keys
2. Find the key
3. Click "Revoke"
4. Generate new key
5. Update configuration

---

## Team Workflows

### Recommended: Per-Developer Keys

**Setup:**
1. Each developer signs up at [dashboard](https://app.buildmetrics.moonlightember.com)
2. Each generates their own API key
3. Each configures locally via Setup Wizard
4. Builds tracked separately per developer

**Advantages:**
- ✅ See who built what
- ✅ Track developer performance
- ✅ No key sharing

### Alternative: Shared Key

**Setup:**
1. Team lead creates project & API key
2. Add `BUILD_METRICS_API_KEY` to team's shared environment
3. Everyone uses same key

**Advantages:**
- ✅ Simple setup
- ✅ Works for small teams

**Disadvantages:**
- ❌ Can't identify individual developers
- ❌ If key leaks, affects whole team

---

## Verification

### Test Configuration

After setup:

1. Build your Unity project
2. Check Unity Console for:
   ```
   [BuildMetrics] Build metrics sent successfully
   ```
3. Check [dashboard](https://app.buildmetrics.moonlightember.com/dashboard/builds)
4. Your build should appear within seconds

### Debug Mode

Enable verbose logging:

```bash
# macOS/Linux
export BUILD_METRICS_VERBOSE=true

# Windows PowerShell
$env:BUILD_METRICS_VERBOSE="true"
```

Then rebuild. Console will show detailed upload logs.

---

## Troubleshooting

### "Invalid API key" error

**Solution:**
1. Verify key starts with `bm_`
2. Check for extra spaces when pasting
3. Regenerate key if needed

### "Environment variable not found"

**Solution:**
```bash
# Verify variable is set
echo $BUILD_METRICS_API_KEY  # macOS/Linux
$env:BUILD_METRICS_API_KEY   # Windows PowerShell
```

**If empty:**
- Restart terminal
- Restart Unity Editor
- Check shell profile file

### Settings panel shows "No API key configured"

**When using environment variable:**
- This is normal - EditorPrefs is empty
- Click "Test Connection" to verify env var works

---

## Security Best Practices

### ✅ DO

- Store API keys in environment variables for CI/CD
- Use per-developer keys for local development
- Revoke compromised keys immediately
- Use `.gitignore` to exclude settings

### ❌ DON'T

- Commit API keys to Git
- Share API keys via Slack/email
- Hardcode keys in scripts
- Use same key for dev and production

---

## See Also

- [Installation Guide](installation.md)
- [CI/CD Integration](ci-cd/overview.md)
- [Offline Features](offline-features.md)

# Offline Features

Build Metrics works **offline-first** - you get value immediately without signing up or configuring an API key.

## Overview

The plugin stores the **last 10 builds** locally in Unity's EditorPrefs, allowing you to:

- ✅ Track builds without internet
- ✅ Compare build sizes offline
- ✅ View build history anytime
- ✅ Test the plugin before committing to cloud sync

**Cloud sync is optional** - enable it when ready by adding an API key.

---

## Build History Window

Access via: **Tools → Build Metrics → Build History**

### Features

**Build List**
- View last 10 builds
- Sort by date, platform, size, or time
- Filter by platform
- Search by date range

**Build Details**
- Full build summary
- File breakdown by category
- Asset breakdown (if available)
- Git information
- Delta vs previous build

**Charts**
- Size trend over time
- Build time trend
- Asset category distribution (pie chart)

**Compare Builds**
- Select any 2 builds
- Side-by-side comparison
- Show size/time deltas
- Highlight regressions

**Export**
- Export build data to JSON
- Share with team
- Archive historical data

---

## What Gets Stored Locally

### Build Data (All Platforms)

```json
{
  "buildGuid": "unique-id",
  "platform": "Android",
  "buildTime": 123.45,
  "outputSize": 50000000,
  "unityVersion": "2022.3.20f1",
  "timestamp": "2026-01-30T12:00:00Z",
  "gitCommitSha": "abc123",
  "gitBranch": "main",
  "isDevelopmentBuild": false
}
```

### File Breakdown (Unity 2022.2+)

Categories tracked:
- Scripts (C# assemblies)
- Resources folder
- Streaming Assets
- Plugins (native libraries)
- Scenes (.unity files)
- Shaders
- Other assets

Each category includes:
- Total size
- File count
- Top files by size

### Asset Breakdown

Top 20 largest assets with:
- Asset path
- Size in bytes
- Category

---

## Storage Details

**Location:**
- Same as EditorPrefs (platform-specific)
- Key prefix: `BuildMetrics_`

**Size limit:**
- ~10KB per build
- 10 builds = ~100KB total
- Negligible disk usage

**Retention:**
- Last 10 builds kept
- Oldest automatically removed
- Per-project storage (isolated)

---

## Using Offline Mode

### Workflow 1: Completely Offline

**Use case:** No internet, no API key needed

1. Install plugin (via .unitypackage)
2. Build your project
3. Open Build History window
4. View all metrics locally

**Limitations:**
- No cross-device sync
- No team collaboration
- No email alerts
- Limited to 10 builds

---

### Workflow 2: Hybrid (Recommended)

**Use case:** Work offline, sync when online

1. Install plugin
2. Work offline → builds stored locally
3. Add API key when ready
4. Historical builds auto-upload on next build
5. Future builds sync to cloud

**Advantages:**
- ✅ Try before committing
- ✅ Work anywhere
- ✅ Sync when convenient
- ✅ No data loss

---

## Build History UI Guide

### Main View

```
┌─────────────────────────────────────────┐
│  Build History (Last 10 Builds)         │
├─────────────────────────────────────────┤
│  Filter: [All Platforms ▼]              │
│  Sort by: [Date ▼]                      │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ Jan 30 - Android - 50MB - 2m15s   │  │
│  │ Jan 29 - iOS - 65MB - 3m42s       │  │
│  │ Jan 29 - Android - 48MB - 2m10s   │  │
│  └───────────────────────────────────┘  │
│                                         │
│  [Compare Selected] [Export JSON]       │
└─────────────────────────────────────────┘
```

### Detail View

Click any build to see:
- Build summary
- File breakdown chart
- Asset categories (pie chart)
- Git information
- Delta vs last build of same platform

### Compare View

Select 2 builds → Click "Compare":
- Side-by-side metrics
- Size difference (± MB)
- Time difference (± seconds)
- Changed files highlighted

---

## Charts & Visualization

### Size Trend Chart

Line chart showing build size over time:
- X-axis: Build date
- Y-axis: Size (MB)
- Color-coded by platform
- Hover for details

### Time Trend Chart

Build duration over time:
- X-axis: Build date
- Y-axis: Time (seconds)
- Compare across builds

### Asset Breakdown (Pie Chart)

Visual breakdown of asset categories:
- Scripts
- Resources
- Textures
- Audio
- Meshes
- Other

---

## Export & Share

### Export to JSON

```json
{
  "builds": [
    {
      "platform": "Android",
      "size": 50000000,
      "time": 135.5,
      "date": "2026-01-30",
      "files": {...},
      "assets": {...}
    }
  ]
}
```

**Use cases:**
- Share with team (email, Slack)
- Archive before Unity upgrade
- Import into Excel/Google Sheets
- Custom analysis tools

---

## Transition to Cloud Sync

When ready to enable cloud sync:

1. **Get API key** from [dashboard](https://app.buildmetrics.moonlightember.com)
2. **Configure** via Tools → Build Metrics → Setup Wizard
3. **Build once** - plugin detects new API key
4. **Historical builds auto-upload** (if within 30 days)
5. **Future builds sync automatically**

**Your local history is preserved** - cloud is additive.

---

## Privacy & Data

### What Stays Local

By default (no API key):
- ✅ All build data
- ✅ File breakdown
- ✅ Git information
- ✅ Personal notes/tags

### What Syncs to Cloud

With API key configured:
- Build metrics (size, time, platform)
- File/asset breakdown
- Git commit info
- Machine fingerprint (for time baselines)

**Not synced:**
- Source code
- Asset files
- Personal notes
- Developer identity (unless you enable it)

---

## Limitations of Offline Mode

**Maximum builds:** 10 (oldest auto-deleted)
**No team sharing:** Data stays on your machine
**No alerts:** Can't detect regressions automatically
**No baselines:** Can't pin reference builds
**No cross-device:** Each machine has separate history

**Solution:** Enable cloud sync for these features

---

## Best Practices

### Use Offline Mode When:

- ✅ Evaluating the plugin
- ✅ Working without internet
- ✅ Personal/hobby projects
- ✅ Prototyping/experiments

### Enable Cloud Sync When:

- ✅ Working in a team
- ✅ Need regression alerts
- ✅ Want cross-device access
- ✅ Need historical trends (>10 builds)
- ✅ CI/CD integration

---

## Troubleshooting

### Build History window is empty

**Cause:** No builds yet

**Solution:** Build your project once

### Can't see old builds

**Cause:** EditorPrefs cleared

**Solution:**
- Builds stored per-project
- Opening different project shows different history
- Clearing EditorPrefs deletes history

### Chart not rendering

**Cause:** Unity Handles not supported in older versions

**Solution:** Upgrade to Unity 2020.3+ for chart support

---

## See Also

- [Configuration Guide](configuration.md)
- [Installation Guide](installation.md)
- [CI/CD Integration](ci-cd/overview.md)

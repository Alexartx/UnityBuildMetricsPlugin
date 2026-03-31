# Offline Features

Build Metrics still provides immediate local value even though this public package also includes cloud workflows.

## Local-First Behavior

Without an API key or internet connection, you can still:

- capture successful builds locally
- inspect build size and build time history
- compare builds and baselines
- review file and asset breakdowns
- inspect Git and machine metadata when available

## Build History Window

Access via: **Tools -> Build Metrics -> Build History**

### Included local views

- **Build List**: recent builds, filtering, search, and trend charts
- **Build Details**: summary, baseline delta, file breakdown, asset breakdown, and Git info
- **Compare Builds**: side-by-side size and time deltas between two builds

## Local Storage

- raw JSON reports are written to `BuildReports/`
- recent history is written to `Library/BuildMetrics/history.json`
- the in-editor history keeps the most recent 20 builds

## Cloud Is Optional

Cloud features are included in this package, but they are not required for the core workflow.

If you never configure an API key:

- local capture still works
- the Build History window still works
- no reports are uploaded

## Notes

- This package is editor-only
- It does not add runtime code to player builds
- Unity 2022.2+ gives the best Android and WebGL breakdown detail

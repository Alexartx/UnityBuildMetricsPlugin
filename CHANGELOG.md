# Changelog

All notable changes to Build Metrics for Unity will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.5] - 2026-03-22

### Added
- Asset Breakdown now reports a dedicated `Sprite Atlases` bucket
- Asset Breakdown now includes `Top Folders` to highlight the heaviest project areas
- Dashboard asset details now show an attribution coverage badge (`Attributed assets / Full build`)

### Fixed
- Asset Breakdown category totals are now rebuilt from the deduplicated asset list, so cards and totals no longer overcount duplicate asset entries
- Empty asset state messaging is now accurate for all collection failure modes, not just IL2CPP builds

### Changed
- Asset Breakdown copy now clarifies that it represents attributed project assets from `Assets/`

## [1.0.4] - 2026-03-16

### Added
- Status bar in Build History window showing live upload state (uploading / success / failed) with quick-action links
- Cloud CTA banner at 20 local builds — non-blocking, purely additive upsell
- Server-side API key validation in Setup Wizard (GET /api/validate) with human-readable error messages

### Fixed
- Trend charts now show the most recent N builds instead of the oldest N builds
- Git info collector no longer freezes the Editor on slow or missing git installs (5 s timeout + kill)
- Build history migrated from EditorPrefs to `Library/BuildMetrics/history.json` — removes 8 KB size limit and makes history project-specific
- Silent upload failures now surface a descriptive error in the status bar

### Changed
- Setup Wizard key validation saves the key immediately on server confirmation
- Cloud CTA copy updated to focus on feature value rather than local storage limits

## [1.0.3] - 2026-02-05

### Added
- Artifact column and filtering in Build History list
- Trend filters for platform and artifact type
- Artifact shown in Build Details

### Changed
- Android/WebGL/iOS (Xcode) build size now prefers parsed build output when available
- Trend charts default to current Unity build target (when available)
- Plugins category label clarified as “Plugins (Unity core)” in comparison view

### Fixed
- Trend charts no longer mix platforms by default
- Build size logging now uses normalized size

## [1.0.2] - 2026-02-02

### Added
- Comprehensive documentation

## [1.0.1] - 2026-01-04

### Added
- Initial stable release
- Automatic build time tracking
- Build size monitoring
- Multi-platform support (Android, iOS, Windows, macOS, Linux, WebGL)
- Build data upload to Build Metrics API
- Settings UI for API key configuration
- Auto-upload toggle
- Manual upload option
- Assembly definitions for fast compilation
- Comprehensive documentation
- MIT License

### Fixed
- Default API URL now points to production server
- Improved error handling and logging

### Technical
- Unity 2020.3+ support
- .NET Standard 2.1
- UPM-compatible package structure
- Editor-only code (no runtime overhead)

## [0.1.0] - 2025-12-20

### Added
- Initial beta release
- Basic build metrics collection
- API integration prototype

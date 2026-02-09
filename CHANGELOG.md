# Changelog

All notable changes to Build Metrics for Unity will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

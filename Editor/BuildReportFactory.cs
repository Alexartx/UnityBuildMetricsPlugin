using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildReportFactory
    {
        public static BuildMetricsReport Create(BuildReport report)
        {
            var summary = report.summary;

            var normalizedStatus = summary.result.ToString().ToLowerInvariant();
            if (summary.result == BuildResult.Unknown && summary.totalErrors == 0)
            {
                normalizedStatus = "success";
            }

            var artifactInfo = GetArtifactInfo(summary.outputPath, summary.platform);
            var outputSizeBytes = GetOutputSize(summary.outputPath, summary.totalSize, artifactInfo);
            var buildOptions = summary.options;

#if UNITY_2021_2_OR_NEWER
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(summary.platform));
            var scriptingBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget).ToString();
#else
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(summary.platform);
            var scriptingBackend = PlayerSettings.GetScriptingBackend(buildTargetGroup).ToString();
#endif

            // Collect git info and file breakdown
            var gitInfo = GitInfoCollector.Collect();
            var fileBreakdown = FileBreakdownCollector.Collect(report);
            var assetBreakdown = FileBreakdownCollector.CollectAssetBreakdown(report);

            // Prefer parsed build output totals when available for select platforms.
            if (fileBreakdown != null)
            {
                var breakdownTotal = GetFileBreakdownTotalSize(fileBreakdown);
                if (breakdownTotal > 0)
                {
                    if (summary.platform == BuildTarget.Android ||
                        summary.platform == BuildTarget.WebGL ||
                        (summary.platform == BuildTarget.iOS && artifactInfo.Type == "xcode"))
                    {
                        outputSizeBytes = breakdownTotal;
                    }
                }
            }

            // Get platform-specific build number
            var platformBuildNumber = GetPlatformBuildNumber(summary.platform);

            return new BuildMetricsReport
            {
                project = Application.productName,
                bundleId = PlayerSettings.applicationIdentifier,
                platform = summary.platform.ToString(),
                unityVersion = Application.unityVersion,
                buildGuid = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow.ToString("o"),
                status = normalizedStatus,
                buildTimeSeconds = Mathf.RoundToInt((float)summary.totalTime.TotalSeconds),
                outputSizeBytes = outputSizeBytes,
                outputPath = summary.outputPath,
                artifactType = artifactInfo.Type,
                artifactExtension = artifactInfo.Extension,
                developmentBuild = buildOptions.HasFlag(BuildOptions.Development),
                allowDebugging = buildOptions.HasFlag(BuildOptions.AllowDebugging),
                scriptDebugging = buildOptions.HasFlag(BuildOptions.Development) && buildOptions.HasFlag(BuildOptions.AllowDebugging),
                scriptingBackend = scriptingBackend,
                machine = new MachineInfo
                {
                    os = SystemInfo.operatingSystem,
                    cpu = SystemInfo.processorType,
                    ramGb = Mathf.RoundToInt(SystemInfo.systemMemorySize / 1024f)
                },
                summary = new BuildSummary
                {
                    errors = summary.totalErrors,
                    warnings = summary.totalWarnings
                },
                // Build Detail: Git & File Breakdown
                // buildName = version (e.g., "1.0.0")
                // buildNumber = platform-specific build number (e.g., "123")
                buildName = PlayerSettings.bundleVersion,
                buildNumber = platformBuildNumber,
                git = gitInfo,
                fileBreakdown = fileBreakdown,
                assetBreakdown = assetBreakdown
            };
        }

        private static long GetFileBreakdownTotalSize(FileBreakdown breakdown)
        {
            if (breakdown == null)
            {
                return 0;
            }

            return (breakdown.scripts?.size ?? 0)
                + (breakdown.resources?.size ?? 0)
                + (breakdown.streamingAssets?.size ?? 0)
                + (breakdown.plugins?.size ?? 0)
                + (breakdown.scenes?.size ?? 0)
                + (breakdown.shaders?.size ?? 0)
                + (breakdown.other?.size ?? 0);
        }

        private static long GetOutputSize(string outputPath, ulong fallbackTotalSize, ArtifactInfo artifactInfo)
        {
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                if (File.Exists(outputPath))
                {
                    return new FileInfo(outputPath).Length;
                }

                if (Directory.Exists(outputPath))
                {
                    var artifacts = Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories)
                        .Where(path => path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
                                       path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase) ||
                                       path.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase))
                        .Select(path => new FileInfo(path))
                        .ToList();

                    if (artifacts.Count > 0)
                    {
                        return artifacts.OrderByDescending(file => file.Length).First().Length;
                    }

                    if (artifactInfo.Type == "app" || artifactInfo.Type == "xcode" || artifactInfo.Type == "webgl" || artifactInfo.Type == "folder")
                    {
                        return Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
                            .Select(path => new FileInfo(path).Length)
                            .Aggregate(0L, (current, size) => current + size);
                    }
                }
            }

            return unchecked((long)fallbackTotalSize);
        }

        private static ArtifactInfo GetArtifactInfo(string outputPath, BuildTarget platform)
        {
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                if (File.Exists(outputPath))
                {
                    return FromFilePath(outputPath, platform);
                }

                if (Directory.Exists(outputPath))
                {
                    var artifacts = Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories)
                        .Where(path => path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
                                       path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase) ||
                                       path.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (artifacts.Count > 0)
                    {
                        return FromFilePath(artifacts[0], platform);
                    }

                    if (outputPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ArtifactInfo("app", ".app");
                    }
                }
            }

            if (platform == BuildTarget.WebGL)
            {
                return new ArtifactInfo("webgl", null);
            }

            if (platform == BuildTarget.iOS)
            {
                return new ArtifactInfo("xcode", null);
            }

            return new ArtifactInfo("folder", null);
        }

        private static string GetPlatformBuildNumber(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.Android:
                    return PlayerSettings.Android.bundleVersionCode.ToString();
                case BuildTarget.iOS:
                    return PlayerSettings.iOS.buildNumber;
                default:
                    // For other platforms, use bundleVersion as fallback
                    return PlayerSettings.bundleVersion;
            }
        }

        private static ArtifactInfo FromFilePath(string path, BuildTarget platform)
        {
            var extension = Path.GetExtension(path);
            if (extension.Equals(".apk", StringComparison.OrdinalIgnoreCase)) return new ArtifactInfo("apk", ".apk");
            if (extension.Equals(".aab", StringComparison.OrdinalIgnoreCase)) return new ArtifactInfo("aab", ".aab");
            if (extension.Equals(".ipa", StringComparison.OrdinalIgnoreCase)) return new ArtifactInfo("ipa", ".ipa");
            if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)) return new ArtifactInfo("exe", ".exe");
            if (extension.Equals(".app", StringComparison.OrdinalIgnoreCase)) return new ArtifactInfo("app", ".app");

            if (platform == BuildTarget.WebGL) return new ArtifactInfo("webgl", null);
            if (platform == BuildTarget.iOS) return new ArtifactInfo("xcode", null);
            return new ArtifactInfo("file", extension);
        }

        private readonly struct ArtifactInfo
        {
            public string Type { get; }
            public string Extension { get; }

            public ArtifactInfo(string type, string extension)
            {
                Type = type;
                Extension = extension;
            }
        }
    }
}

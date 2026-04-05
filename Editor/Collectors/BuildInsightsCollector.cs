using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildInsightsCollector
    {
        private static readonly AndroidSdkPattern[] AndroidSdkPatterns =
        {
            new AndroidSdkPattern("Firebase", "firebase", "google_app_measurement", "datatransport", "crashlytics"),
            new AndroidSdkPattern("Facebook SDK", "facebook", "com/facebook", "facebook_sdk"),
            new AndroidSdkPattern("Google Play Services", "play-services", "playservices", "com/google/android/gms", "gmscore")
        };

        public static BuildStepInfo[] CollectBuildSteps(BuildReport report)
        {
            if (report == null)
            {
                return Array.Empty<BuildStepInfo>();
            }

            var stepsValue = GetPropertyValue(report, "steps");
            if (!(stepsValue is IEnumerable steps))
            {
                return Array.Empty<BuildStepInfo>();
            }

            var collected = new List<BuildStepInfo>();
            foreach (var step in steps)
            {
                if (step == null)
                {
                    continue;
                }

                var duration = GetTimeSpanProperty(step, "duration");
                var durationMs = duration.HasValue ? (long)Math.Round(duration.Value.TotalMilliseconds) : 0L;
                var name = GetStringProperty(step, "name");
                if (string.IsNullOrWhiteSpace(name) && durationMs <= 0)
                {
                    continue;
                }

                collected.Add(new BuildStepInfo
                {
                    name = string.IsNullOrWhiteSpace(name) ? "Unnamed Step" : name,
                    depth = GetIntProperty(step, "depth"),
                    durationMs = Math.Max(0L, durationMs),
                    messageCount = CountEnumerable(GetPropertyValue(step, "messages"))
                });
            }

            return collected.ToArray();
        }

        public static AssetSceneUsage[] CollectSceneUsage(BuildReport report, AssetBreakdown assetBreakdown)
        {
            if (report == null || assetBreakdown?.topAssets == null || assetBreakdown.topAssets.Length == 0)
            {
                return Array.Empty<AssetSceneUsage>();
            }

            var heavyAssets = assetBreakdown.topAssets
                .Where(asset => !string.IsNullOrWhiteSpace(asset.path))
                .GroupBy(asset => NormalizePath(asset.path))
                .ToDictionary(group => group.Key, group => group.OrderByDescending(asset => asset.size).First(), StringComparer.OrdinalIgnoreCase);

            if (heavyAssets.Count == 0)
            {
                return Array.Empty<AssetSceneUsage>();
            }

            var scenesUsingAssetsValue = GetPropertyValue(report, "scenesUsingAssets");
            if (!(scenesUsingAssetsValue is IEnumerable sceneGroups))
            {
                return Array.Empty<AssetSceneUsage>();
            }

            var sceneMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var sceneGroup in sceneGroups)
            {
                if (sceneGroup == null)
                {
                    continue;
                }

                var listValue = GetPropertyValue(sceneGroup, "list");
                IEnumerable sceneEntries = listValue as IEnumerable;
                if (sceneEntries == null || sceneEntries is string)
                {
                    sceneEntries = new[] { sceneGroup };
                }

                foreach (var sceneEntry in sceneEntries)
                {
                    if (sceneEntry == null)
                    {
                        continue;
                    }

                    var assetPath = NormalizePath(GetStringProperty(sceneEntry, "assetPath"));
                    if (string.IsNullOrWhiteSpace(assetPath) || !heavyAssets.ContainsKey(assetPath))
                    {
                        continue;
                    }

                    var scenePaths = GetStringArray(sceneEntry, "scenePaths");
                    if (scenePaths.Length == 0)
                    {
                        scenePaths = GetStringArray(sceneEntry, "includedInScenes");
                    }

                    if (scenePaths.Length == 0)
                    {
                        continue;
                    }

                    if (!sceneMap.TryGetValue(assetPath, out var bucket))
                    {
                        bucket = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        sceneMap[assetPath] = bucket;
                    }

                    foreach (var scenePath in scenePaths.Where(path => !string.IsNullOrWhiteSpace(path)))
                    {
                        bucket.Add(scenePath);
                    }
                }
            }

            return heavyAssets.Values
                .Where(asset => sceneMap.ContainsKey(asset.path))
                .Select(asset => new AssetSceneUsage
                {
                    assetPath = asset.path,
                    category = asset.category,
                    sizeBytes = asset.size,
                    scenePaths = sceneMap[asset.path].OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
                })
                .OrderByDescending(usage => usage.sizeBytes)
                .ToArray();
        }

        public static EngineModuleInfo[] CollectEngineModules(BuildReport report)
        {
            if (report == null)
            {
                return Array.Empty<EngineModuleInfo>();
            }

            var strippingInfo = GetPropertyValue(report, "strippingInfo");
            if (strippingInfo == null)
            {
                return Array.Empty<EngineModuleInfo>();
            }

            var includedModulesValue = GetPropertyValue(strippingInfo, "includedModules");
            if (!(includedModulesValue is IEnumerable includedModules))
            {
                return Array.Empty<EngineModuleInfo>();
            }

            var reasonsMethod = strippingInfo.GetType().GetMethod("GetReasonsForIncluding", BindingFlags.Public | BindingFlags.Instance);
            var modules = new List<EngineModuleInfo>();

            foreach (var moduleValue in includedModules)
            {
                var moduleName = moduleValue?.ToString();
                if (string.IsNullOrWhiteSpace(moduleName))
                {
                    continue;
                }

                var reasons = new List<string>();
                if (reasonsMethod != null)
                {
                    try
                    {
                        var reasonsValue = reasonsMethod.Invoke(strippingInfo, new object[] { moduleName });
                        if (reasonsValue is IEnumerable reasonsEnumerable)
                        {
                            foreach (var reason in reasonsEnumerable)
                            {
                                var text = reason?.ToString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    reasons.Add(text.Trim());
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If Unity cannot resolve reasons for a specific module, keep the module entry without reasons.
                    }
                }

                modules.Add(new EngineModuleInfo
                {
                    name = moduleName,
                    reasons = reasons.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToArray()
                });
            }

            return modules.OrderBy(module => module.name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static AndroidPackageInsight CollectAndroidPackageInsight(BuildReport report)
        {
            if (report == null || report.summary.platform != UnityEditor.BuildTarget.Android)
            {
                return null;
            }

            var insight = new AndroidPackageInsight();
            var sdkAccumulators = AndroidSdkPatterns.ToDictionary(
                pattern => pattern.Name,
                pattern => new AndroidSdkAccumulator(pattern.Name, pattern.Patterns),
                StringComparer.OrdinalIgnoreCase);

            var outputPath = report.summary.outputPath;
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                if (File.Exists(outputPath) && (outputPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) || outputPath.EndsWith(".aab", StringComparison.OrdinalIgnoreCase)))
                {
                    CollectAndroidInsightFromArchive(outputPath, insight, sdkAccumulators);
                }
                else if (Directory.Exists(outputPath))
                {
                    CollectAndroidInsightFromDirectory(outputPath, insight, sdkAccumulators);
                }
            }

#if UNITY_2022_2_OR_NEWER
            try
            {
                var buildFiles = report.GetFiles();
                if (buildFiles != null)
                {
                    foreach (var buildFile in buildFiles)
                    {
                        if (string.IsNullOrWhiteSpace(buildFile.path) || buildFile.size <= 0)
                        {
                            continue;
                        }

                        RegisterSdkEvidence(buildFile.path, (long)buildFile.size, sdkAccumulators);
                    }
                }
            }
            catch
            {
                // Best-effort heuristics only.
            }
#endif

            insight.sdkInsights = sdkAccumulators.Values
                .Where(accumulator => accumulator.FileCount > 0)
                .OrderByDescending(accumulator => accumulator.SizeBytes)
                .Select(accumulator => accumulator.ToInsight())
                .ToArray();

            if (insight.nativeLibrariesSize == 0 &&
                insight.dexCodeSize == 0 &&
                insight.androidResourcesSize == 0 &&
                insight.unityDataSize == 0 &&
                insight.streamingAssetsSize == 0 &&
                insight.manifestSize == 0 &&
                insight.sdkInsights.Length == 0)
            {
                return null;
            }

            return insight;
        }

        private static void CollectAndroidInsightFromArchive(
            string archivePath,
            AndroidPackageInsight insight,
            Dictionary<string, AndroidSdkAccumulator> sdkAccumulators)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var size = entry.CompressedLength > 0 ? entry.CompressedLength : entry.Length;
                    RegisterAndroidPackageCategory(entry.FullName, size, insight);
                    RegisterSdkEvidence(entry.FullName, size, sdkAccumulators);
                }
            }
        }

        private static void CollectAndroidInsightFromDirectory(
            string directoryPath,
            AndroidPackageInsight insight,
            Dictionary<string, AndroidSdkAccumulator> sdkAccumulators)
        {
            foreach (var filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = filePath.Substring(directoryPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var size = new FileInfo(filePath).Length;
                RegisterAndroidPackageCategory(relativePath, size, insight);
                RegisterSdkEvidence(relativePath, size, sdkAccumulators);
            }
        }

        private static void RegisterAndroidPackageCategory(string path, long size, AndroidPackageInsight insight)
        {
            var lowerPath = path.Replace('\\', '/').ToLowerInvariant();

            if (lowerPath.StartsWith("lib/") || lowerPath.Contains("/lib/") || lowerPath.Contains("/jnilibs/"))
            {
                insight.nativeLibrariesSize += size;
            }
            else if (((lowerPath.StartsWith("classes") || lowerPath.Contains("/dex/classes")) && lowerPath.EndsWith(".dex")) || lowerPath.EndsWith(".jar"))
            {
                insight.dexCodeSize += size;
            }
            else if (lowerPath.StartsWith("res/") || lowerPath.Contains("/res/") ||
                lowerPath == "resources.arsc" || lowerPath.EndsWith("/resources.arsc"))
            {
                insight.androidResourcesSize += size;
            }
            else if (lowerPath.StartsWith("assets/bin/data/") || lowerPath.Contains("/assets/bin/data/"))
            {
                insight.unityDataSize += size;
            }
            else if ((lowerPath.StartsWith("assets/") || lowerPath.Contains("/assets/")) &&
                !lowerPath.StartsWith("assets/bin/") &&
                !lowerPath.Contains("/assets/bin/"))
            {
                insight.streamingAssetsSize += size;
            }

            if (lowerPath.EndsWith("androidmanifest.xml") || lowerPath == "androidmanifest.xml")
            {
                insight.manifestSize += size;
            }
        }

        private static void RegisterSdkEvidence(
            string evidencePath,
            long size,
            Dictionary<string, AndroidSdkAccumulator> sdkAccumulators)
        {
            if (string.IsNullOrWhiteSpace(evidencePath))
            {
                return;
            }

            var normalizedPath = evidencePath.Replace('\\', '/').ToLowerInvariant();
            foreach (var pattern in AndroidSdkPatterns)
            {
                if (pattern.Patterns.Any(token => normalizedPath.Contains(token)))
                {
                    sdkAccumulators[pattern.Name].Register(normalizedPath, size, evidencePath);
                }
            }
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property != null ? property.GetValue(instance, null) : null;
        }

        private static string GetStringProperty(object instance, string propertyName)
        {
            return GetPropertyValue(instance, propertyName)?.ToString();
        }

        private static int GetIntProperty(object instance, string propertyName)
        {
            var value = GetPropertyValue(instance, propertyName);
            return value is int intValue ? intValue : 0;
        }

        private static TimeSpan? GetTimeSpanProperty(object instance, string propertyName)
        {
            var value = GetPropertyValue(instance, propertyName);
            return value is TimeSpan timeSpan ? timeSpan : (TimeSpan?)null;
        }

        private static string[] GetStringArray(object instance, string propertyName)
        {
            var value = GetPropertyValue(instance, propertyName);
            if (!(value is IEnumerable enumerable))
            {
                return Array.Empty<string>();
            }

            return enumerable
                .Cast<object>()
                .Select(item => item?.ToString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static int CountEnumerable(object enumerableValue)
        {
            if (!(enumerableValue is IEnumerable enumerable))
            {
                return 0;
            }

            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }

            return count;
        }

        private sealed class AndroidSdkAccumulator
        {
            private readonly HashSet<string> seenEvidence = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public AndroidSdkAccumulator(string name, IEnumerable<string> patterns)
            {
                Name = name;
                Patterns = patterns.ToArray();
            }

            public string Name { get; }
            public string[] Patterns { get; }
            public long SizeBytes { get; private set; }
            public int FileCount => seenEvidence.Count;
            public IReadOnlyCollection<string> Evidence => seenEvidence;

            public void Register(string normalizedPath, long size, string originalPath)
            {
                if (!seenEvidence.Add(normalizedPath))
                {
                    return;
                }

                SizeBytes += Math.Max(0L, size);
                if (!string.IsNullOrWhiteSpace(originalPath))
                {
                    // Preserve a readable form for the UI, but cap later.
                }
            }

            public AndroidSdkInsight ToInsight()
            {
                return new AndroidSdkInsight
                {
                    name = Name,
                    sizeBytes = SizeBytes,
                    fileCount = FileCount,
                    evidence = seenEvidence.Take(4).ToArray()
                };
            }
        }

        private readonly struct AndroidSdkPattern
        {
            public AndroidSdkPattern(string name, params string[] patterns)
            {
                Name = name;
                Patterns = patterns;
            }

            public string Name { get; }
            public string[] Patterns { get; }
        }
    }
}

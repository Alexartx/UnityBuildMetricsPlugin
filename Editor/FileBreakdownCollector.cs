using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildMetrics.Editor
{
    /// <summary>
    /// Collects file size breakdown from Unity's BuildReport.
    /// Categorizes files into: scripts, resources, streaming assets, plugins, scenes, shaders, other.
    /// </summary>
    public static class FileBreakdownCollector
    {
        /// <summary>
        /// Collect file breakdown from the build report.
        /// Returns null if the report doesn't contain file information.
        /// </summary>
        public static FileBreakdown Collect(BuildReport report)
        {
            if (report == null)
            {
                return null;
            }

            try
            {
                var categories = new Dictionary<string, FileCategoryData>
                {
                    ["scripts"] = new FileCategoryData { size = 0, count = 0 },
                    ["resources"] = new FileCategoryData { size = 0, count = 0 },
                    ["streamingAssets"] = new FileCategoryData { size = 0, count = 0 },
                    ["plugins"] = new FileCategoryData { size = 0, count = 0 },
                    ["scenes"] = new FileCategoryData { size = 0, count = 0 },
                    ["shaders"] = new FileCategoryData { size = 0, count = 0 },
                    ["other"] = new FileCategoryData { size = 0, count = 0 }
                };

                // Track all files for top files list
                var allFiles = new List<TopFile>();

                // Try PackedAssets first (works for iOS, Standalone)
                var packedAssets = report.packedAssets;
                if (packedAssets != null && packedAssets.Length > 0)
                {
                    // Use packedAssets API
                    CollectFromPackedAssets(packedAssets, categories, allFiles);
                }
                else
                {
                    // Fallback: Use GetFiles() API (Unity 2022.2+, works for Android, WebGL, all platforms)
                    UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} PackedAssets not available, trying GetFiles() API");

#if UNITY_2022_2_OR_NEWER
                    var buildFiles = report.GetFiles();
                    if (buildFiles != null && buildFiles.Length > 0)
                    {
                        CollectFromBuildFiles(buildFiles, categories, allFiles);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} No file data available in build report");
                        return null;
                    }
#else
                    UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} File breakdown requires Unity 2022.2+ for this platform");
                    return null;
#endif
                }

                // Verify we collected some files
                if (allFiles.Count == 0)
                {
                    UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} No files collected from build report");
                    return null;
                }

                // Get top 20 largest files
                var topFiles = allFiles
                    .OrderByDescending(f => f.size)
                    .Take(20)
                    .ToArray();

                var breakdown = new FileBreakdown
                {
                    scripts = categories["scripts"],
                    resources = categories["resources"],
                    streamingAssets = categories["streamingAssets"],
                    plugins = categories["plugins"],
                    scenes = categories["scenes"],
                    shaders = categories["shaders"],
                    other = categories["other"],
                    topFiles = topFiles
                };

                var totalSize = categories.Values.Sum(c => c.size);
                var totalCount = categories.Values.Sum(c => c.count);
                UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected file breakdown: {totalCount} files, {totalSize / 1024 / 1024:F2} MB (top {topFiles.Length} tracked)");

                return breakdown;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect file breakdown: {ex.Message}");
                return null;
            }
        }

        private static void CollectFromPackedAssets(
            PackedAssets[] packedAssets,
            Dictionary<string, FileCategoryData> categories,
            List<TopFile> allFiles)
        {
            foreach (var packedAsset in packedAssets)
            {
                foreach (var content in packedAsset.contents)
                {
                    var category = CategorizeAsset(content.sourceAssetPath);
                    var data = categories[category];
                    data.size += (long)content.packedSize;
                    data.count++;
                    categories[category] = data;

                    // Track file for top files list
                    allFiles.Add(new TopFile
                    {
                        path = content.sourceAssetPath,
                        size = (long)content.packedSize,
                        category = category
                    });
                }
            }
            UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected {allFiles.Count} files from PackedAssets API");
        }

#if UNITY_2022_2_OR_NEWER
        private static void CollectFromBuildFiles(
            BuildFile[] buildFiles,
            Dictionary<string, FileCategoryData> categories,
            List<TopFile> allFiles)
        {
            foreach (var file in buildFiles)
            {
                // Skip files without a path (can happen for some internal Unity files)
                if (string.IsNullOrEmpty(file.path))
                    continue;

                // Filter out build artifacts and intermediate files
                if (ShouldSkipFile(file.path))
                    continue;

                var category = CategorizeAsset(file.path);
                var data = categories[category];
                data.size += (long)file.size;
                data.count++;
                categories[category] = data;

                // Track file for top files list with cleaned path
                allFiles.Add(new TopFile
                {
                    path = CleanFilePath(file.path),
                    size = (long)file.size,
                    category = category
                });
            }
            UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected {allFiles.Count} files from GetFiles() API (after filtering)");
        }

        private static bool ShouldSkipFile(string path)
        {
            var lower = path.ToLowerInvariant();

            // Skip IL2CPP source/backup files (NOT in final APK)
            if (lower.Contains("/il2cppbackup/") || lower.Contains("\\il2cppbackup\\"))
                return true;
            if (lower.Contains("/il2cppoutput/") || lower.Contains("\\il2cppoutput\\"))
                return true;

            // Skip debug symbols (NOT in final APK)
            if (lower.Contains("/symbols/") || lower.Contains("\\symbols\\"))
                return true;

            // Skip temp files
            if (lower.Contains("/temp/") || lower.Contains("\\temp\\"))
                return true;

            // Skip the final build artifact itself
            if (lower.EndsWith(".apk") || lower.EndsWith(".aab") || lower.EndsWith(".ipa"))
                return true;

            // IMPORTANT: Don't skip files in Gradle/unityLibrary/src/main - these ARE in the APK
            // Keep everything else (especially files in /assets/, /jniLibs/, etc.)
            return false;
        }

        private static string CleanFilePath(string path)
        {
            // Try to extract meaningful relative path from absolute build path

            // For Android: Extract from "src/main/" onwards (includes assets/, jniLibs/, etc.)
            var srcMainIndex = path.IndexOf("/src/main/", StringComparison.OrdinalIgnoreCase);
            if (srcMainIndex >= 0)
                return path.Substring(srcMainIndex + 10); // Skip "/src/main/"

            // Try "assets/" directly
            var assetsIndex = path.IndexOf("/assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                return path.Substring(assetsIndex + 1);

            // Try "jniLibs/" directly
            var jniLibsIndex = path.IndexOf("/jniLibs/", StringComparison.OrdinalIgnoreCase);
            if (jniLibsIndex >= 0)
                return path.Substring(jniLibsIndex + 1);

            // Try to find Unity source "Assets/" folder
            var unityAssetsIndex = path.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (unityAssetsIndex >= 0)
                return path.Substring(unityAssetsIndex + 1);

            // Try "bin/Data/" (common in Unity builds)
            var binDataIndex = path.IndexOf("/bin/Data/", StringComparison.OrdinalIgnoreCase);
            if (binDataIndex >= 0)
                return path.Substring(binDataIndex + 10);

            // Try "libs/" (common for JAR files)
            var libsIndex = path.IndexOf("/libs/", StringComparison.OrdinalIgnoreCase);
            if (libsIndex >= 0)
                return path.Substring(libsIndex + 1);

            // Last resort: Return the last 3 path segments to preserve context
            var separator = path.Contains('/') ? '/' : '\\';
            var segments = path.Split(separator);
            if (segments.Length > 3)
            {
                return string.Join("/", segments.Skip(segments.Length - 3));
            }

            return path;
        }
#endif

        private static string CategorizeAsset(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "other";
            }

            var lowerPath = path.ToLowerInvariant();

            // Scripts (C#, JS, Boo)
            if (lowerPath.EndsWith(".cs") || lowerPath.EndsWith(".js") || lowerPath.EndsWith(".boo") ||
                lowerPath.Contains("/scripts/") || lowerPath.Contains("\\scripts\\"))
            {
                return "scripts";
            }

            // Resources
            if (lowerPath.Contains("/resources/") || lowerPath.Contains("\\resources\\"))
            {
                return "resources";
            }

            // Streaming Assets
            if (lowerPath.Contains("/streamingassets/") || lowerPath.Contains("\\streamingassets\\"))
            {
                return "streamingAssets";
            }

            // Plugins
            if (lowerPath.Contains("/plugins/") || lowerPath.Contains("\\plugins\\") ||
                lowerPath.EndsWith(".dll") || lowerPath.EndsWith(".so") || lowerPath.EndsWith(".bundle"))
            {
                return "plugins";
            }

            // Scenes
            if (lowerPath.EndsWith(".unity"))
            {
                return "scenes";
            }

            // Shaders
            if (lowerPath.EndsWith(".shader") || lowerPath.EndsWith(".cginc") || lowerPath.EndsWith(".shadergraph") ||
                lowerPath.Contains("/shaders/") || lowerPath.Contains("\\shaders\\"))
            {
                return "shaders";
            }

            // Other (textures, audio, models, etc.)
            return "other";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildMetrics.Editor
{
    /// <summary>
    /// Collects file size breakdown from Unity's BuildReport.
    /// Categorizes files into: scripts, resources, streaming assets, plugins, scenes, shaders, other.
    /// For accurate results, analyzes the actual build output (APK, IPA, etc.) instead of Unity's incomplete APIs.
    /// </summary>
    public static class FileBreakdownCollector
    {
        public static FileBreakdown Collect(BuildReport report)
        {
            if (report == null)
                return null;

            try
            {
                var platform    = report.summary.platform;
                var outputPath  = report.summary.outputPath;

                var categories = new Dictionary<string, FileCategoryData>
                {
                    ["scripts"]         = new FileCategoryData { size = 0, count = 0 },
                    ["resources"]       = new FileCategoryData { size = 0, count = 0 },
                    ["streamingAssets"] = new FileCategoryData { size = 0, count = 0 },
                    ["plugins"]         = new FileCategoryData { size = 0, count = 0 },
                    ["scenes"]          = new FileCategoryData { size = 0, count = 0 },
                    ["shaders"]         = new FileCategoryData { size = 0, count = 0 },
                    ["other"]           = new FileCategoryData { size = 0, count = 0 }
                };

                // Try parsing actual build output first (most accurate)
                if (TryParseBuildOutput(outputPath, platform, categories))
                    return BuildFileBreakdownResult(categories, platform, null);

                var otherSubcategories = new Dictionary<string, FileCategorySubData>
                {
                    ["spriteAtlases"]      = new FileCategorySubData { size = 0, count = 0 },
                    ["textures"]           = new FileCategorySubData { size = 0, count = 0 },
                    ["meshes"]             = new FileCategorySubData { size = 0, count = 0 },
                    ["audio"]              = new FileCategorySubData { size = 0, count = 0 },
                    ["assetBundles"]       = new FileCategorySubData { size = 0, count = 0 },
                    ["unityRuntime"]       = new FileCategorySubData { size = 0, count = 0 },
                    ["fonts"]              = new FileCategorySubData { size = 0, count = 0 },
                    ["iosAssetCatalogs"]   = new FileCategorySubData { size = 0, count = 0 },
                    ["iosAppResources"]    = new FileCategorySubData { size = 0, count = 0 },
                    ["iosSystem"]          = new FileCategorySubData { size = 0, count = 0 },
                    ["androidAddressables"]= new FileCategorySubData { size = 0, count = 0 },
                    ["androidUnityData"]   = new FileCategorySubData { size = 0, count = 0 },
                    ["androidResources"]   = new FileCategorySubData { size = 0, count = 0 },
                    ["androidCode"]        = new FileCategorySubData { size = 0, count = 0 },
                    ["androidSystem"]      = new FileCategorySubData { size = 0, count = 0 },
                    ["webglData"]          = new FileCategorySubData { size = 0, count = 0 },
                    ["webglWasm"]          = new FileCategorySubData { size = 0, count = 0 },
                    ["webglJs"]            = new FileCategorySubData { size = 0, count = 0 },
                    ["other"]              = new FileCategorySubData { size = 0, count = 0 }
                };

                // FALLBACK: Try PackedAssets API (works for iOS, Standalone but incomplete)
                var packedAssets = report.packedAssets;
                if (packedAssets != null && packedAssets.Length > 0)
                {
                    CollectFromPackedAssets(packedAssets, categories, otherSubcategories, platform);
                }
                else
                {
                    // Fallback: Use GetFiles() API (Unity 2022.2+, works for Android, WebGL, all platforms)
#if UNITY_2022_2_OR_NEWER
                    var buildFiles = report.GetFiles();
                    if (buildFiles != null && buildFiles.Length > 0)
                    {
                        CollectFromBuildFiles(buildFiles, categories, otherSubcategories, platform);
                    }
                    else
                    {
                        return null;
                    }
#else
                    return null;
#endif
                }

                return BuildFileBreakdownResult(categories, platform, otherSubcategories);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect file breakdown: {ex.Message}");
                return null;
            }
        }

        private static FileBreakdown BuildFileBreakdownResult(
            Dictionary<string, FileCategoryData> categories,
            BuildTarget platform,
            Dictionary<string, FileCategorySubData> otherSubcategories = null)
        {
            if (otherSubcategories != null)
            {
                categories["other"].breakdown = new OtherBreakdown
                {
                    spriteAtlases      = otherSubcategories["spriteAtlases"],
                    textures           = otherSubcategories["textures"],
                    meshes             = otherSubcategories["meshes"],
                    audio              = otherSubcategories["audio"],
                    assetBundles       = otherSubcategories["assetBundles"],
                    unityRuntime       = otherSubcategories["unityRuntime"],
                    fonts              = otherSubcategories["fonts"],
                    iosAssetCatalogs   = otherSubcategories["iosAssetCatalogs"],
                    iosAppResources    = otherSubcategories["iosAppResources"],
                    iosSystem          = otherSubcategories["iosSystem"],
                    androidAddressables= otherSubcategories["androidAddressables"],
                    androidUnityData   = otherSubcategories["androidUnityData"],
                    androidResources   = otherSubcategories["androidResources"],
                    androidCode        = otherSubcategories["androidCode"],
                    androidSystem      = otherSubcategories["androidSystem"],
                    webglData          = otherSubcategories["webglData"],
                    webglWasm          = otherSubcategories["webglWasm"],
                    webglJs            = otherSubcategories["webglJs"],
                    other              = otherSubcategories["other"]
                };
            }

            return new FileBreakdown
            {
                scripts         = categories["scripts"],
                resources       = categories["resources"],
                streamingAssets = categories["streamingAssets"],
                plugins         = categories["plugins"],
                scenes          = categories["scenes"],
                shaders         = categories["shaders"],
                other           = categories["other"]
            };
        }

        private static bool TryParseBuildOutput(
            string outputPath,
            BuildTarget platform,
            Dictionary<string, FileCategoryData> categories)
        {
            if (string.IsNullOrEmpty(outputPath) || !File.Exists(outputPath) && !Directory.Exists(outputPath))
                return false;

            try
            {
                switch (platform)
                {
                    case BuildTarget.Android:
                        return AndroidOutputParser.Parse(outputPath, categories);
                    case BuildTarget.iOS:
                        return IosOutputParser.Parse(outputPath, categories);
                    case BuildTarget.WebGL:
                        return WebGlOutputParser.Parse(outputPath, categories);
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                    case BuildTarget.StandaloneOSX:
                    case BuildTarget.StandaloneLinux64:
                        return StandaloneOutputParser.Parse(outputPath, categories);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to parse build output: {ex.Message}");
                return false;
            }
        }

        private static void CollectFromPackedAssets(
            PackedAssets[] packedAssets,
            Dictionary<string, FileCategoryData> categories,
            Dictionary<string, FileCategorySubData> otherSubcategories,
            BuildTarget platform)
        {
            foreach (var packedAsset in packedAssets)
            {
                foreach (var content in packedAsset.contents)
                {
                    var category = AssetCategorizer.CategorizeAsset(content.sourceAssetPath);
                    var data = categories[category];
                    data.size += (long)content.packedSize;
                    data.count++;
                    categories[category] = data;

                    if (category == "other")
                    {
                        var subcategory = AssetCategorizer.CategorizeOtherFile(content.sourceAssetPath, platform);
                        var subData = otherSubcategories[subcategory];
                        subData.size += (long)content.packedSize;
                        subData.count++;
                        otherSubcategories[subcategory] = subData;
                    }
                }
            }
        }

#if UNITY_2022_2_OR_NEWER
        private static void CollectFromBuildFiles(
            BuildFile[] buildFiles,
            Dictionary<string, FileCategoryData> categories,
            Dictionary<string, FileCategorySubData> otherSubcategories,
            BuildTarget platform)
        {
            foreach (var file in buildFiles)
            {
                // Skip files without a path (can happen for some internal Unity files)
                if (string.IsNullOrEmpty(file.path))
                    continue;

                if (ShouldSkipFile(file.path))
                    continue;

                var category = AssetCategorizer.CategorizeAsset(file.path);
                var data = categories[category];
                data.size += (long)file.size;
                data.count++;
                categories[category] = data;

                if (category == "other")
                {
                    var subcategory = AssetCategorizer.CategorizeOtherFile(file.path, platform);
                    var subData = otherSubcategories[subcategory];
                    subData.size += (long)file.size;
                    subData.count++;
                    otherSubcategories[subcategory] = subData;
                }
            }
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
#endif

        // ─── Asset Breakdown ────────────────────────────────────────────────────────

        /// <summary>
        /// Collect asset breakdown - only files from Assets/** categorized by asset type.
        /// Falls back to Editor.log parsing if BuildReport APIs fail.
        /// Falls back to cached data if Editor.log is unavailable or from different project.
        /// Returns null if no project assets found.
        /// </summary>
        public static AssetBreakdown CollectAssetBreakdown(BuildReport report)
        {
            if (report == null)
                return null;

            try
            {
                var categories = CreateAssetCategories();
                var allAssets  = new List<TopFile>();

                var packedAssets = report.packedAssets;
                if (packedAssets != null && packedAssets.Length > 0)
                {
                    CollectAssetsFromPackedAssets(packedAssets, categories, allAssets);
                }
                else
                {
#if UNITY_2022_2_OR_NEWER
                    var buildFiles = report.GetFiles();
                    if (buildFiles != null && buildFiles.Length > 0)
                        CollectAssetsFromBuildFiles(buildFiles, categories, allAssets);
#endif
                }

                if (allAssets.Count == 0)
                {
                    EditorLogParser.CollectAssetsFromEditorLog(categories, allAssets);

                    if (allAssets.Count == 0)
                    {
                        var cachedBreakdown = ReadAssetBreakdownFromCache();
                        if (cachedBreakdown != null)
                        {
                            Debug.Log($"{BuildMetricsConstants.LogPrefix} Using cached asset breakdown from previous build");
                            return cachedBreakdown;
                        }

                        return new AssetBreakdown
                        {
                            hasAssets    = false,
                            totalAssetsSize = 0,
                            totalAssets  = 0,
                            topAssets    = new TopFile[0],
                            topFolders   = new TopFolder[0]
                        };
                    }
                }

                // Deduplicate assets by path, keeping the largest size entry for each asset
                // (Unity reports the same asset multiple times: original + compressed + mipmaps)
                var deduplicatedAssets = allAssets
                    .GroupBy(a => a.path)
                    .Select(g => g.OrderByDescending(a => a.size).First())
                    .ToList();

                var topAssets  = deduplicatedAssets.OrderByDescending(f => f.size).Take(20).ToArray();
                var topFolders = BuildTopFolders(deduplicatedAssets);

                // Rebuild category totals from the deduplicated asset list so the cards, totals,
                // and top assets all use the same source of truth.
                categories = CreateAssetCategories();
                foreach (var asset in deduplicatedAssets)
                {
                    var category = AssetCategorizer.CategorizeAssetByType(asset.path);
                    var data = categories[category];
                    data.size += asset.size;
                    data.count++;
                    categories[category] = data;
                }

                var breakdown = new AssetBreakdown
                {
                    hasAssets       = true,
                    totalAssetsSize = deduplicatedAssets.Sum(a => a.size),
                    totalAssets     = deduplicatedAssets.Count,
                    textures        = categories["textures"],
                    spriteAtlases   = categories["spriteAtlases"],
                    audio           = categories["audio"],
                    models          = categories["models"],
                    animations      = categories["animations"],
                    prefabs         = categories["prefabs"],
                    scenes          = categories["scenes"],
                    scripts         = categories["scripts"],
                    shaders         = categories["shaders"],
                    materials       = categories["materials"],
                    fonts           = categories["fonts"],
                    videos          = categories["videos"],
                    otherAssets     = categories["otherAssets"],
                    topAssets       = topAssets,
                    topFolders      = topFolders
                };

                SaveAssetBreakdownToCache(breakdown);
                return breakdown;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect asset breakdown: {ex.Message}");
                return null;
            }
        }

        private static Dictionary<string, AssetCategoryData> CreateAssetCategories()
        {
            return new Dictionary<string, AssetCategoryData>
            {
                ["textures"]     = new AssetCategoryData { size = 0, count = 0 },
                ["spriteAtlases"]= new AssetCategoryData { size = 0, count = 0 },
                ["audio"]        = new AssetCategoryData { size = 0, count = 0 },
                ["models"]       = new AssetCategoryData { size = 0, count = 0 },
                ["animations"]   = new AssetCategoryData { size = 0, count = 0 },
                ["prefabs"]      = new AssetCategoryData { size = 0, count = 0 },
                ["scenes"]       = new AssetCategoryData { size = 0, count = 0 },
                ["scripts"]      = new AssetCategoryData { size = 0, count = 0 },
                ["shaders"]      = new AssetCategoryData { size = 0, count = 0 },
                ["materials"]    = new AssetCategoryData { size = 0, count = 0 },
                ["fonts"]        = new AssetCategoryData { size = 0, count = 0 },
                ["videos"]       = new AssetCategoryData { size = 0, count = 0 },
                ["otherAssets"]  = new AssetCategoryData { size = 0, count = 0 }
            };
        }

        private static TopFolder[] BuildTopFolders(List<TopFile> deduplicatedAssets)
        {
            return deduplicatedAssets
                .Select(asset => new { Folder = GetAssetFolderKey(asset.path), Asset = asset })
                .Where(item => !string.IsNullOrEmpty(item.Folder))
                .GroupBy(item => item.Folder)
                .Select(group => new TopFolder
                {
                    path  = group.Key,
                    size  = group.Sum(item => item.Asset.size),
                    count = group.Count()
                })
                .OrderByDescending(folder => folder.size)
                .Take(10)
                .ToArray();
        }

        private static string GetAssetFolderKey(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            var normalizedPath = assetPath.Replace('\\', '/');
            var segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || !string.Equals(segments[0], "Assets", StringComparison.OrdinalIgnoreCase))
                return null;

            var directorySegments = segments.Take(Math.Max(segments.Length - 1, 1)).ToArray();
            if (directorySegments.Length <= 1)
                return "Assets";

            var depth = Math.Min(directorySegments.Length, 3);
            return string.Join("/", directorySegments.Take(depth));
        }

        private static void CollectAssetsFromPackedAssets(
            PackedAssets[] packedAssets,
            Dictionary<string, AssetCategoryData> categories,
            List<TopFile> allAssets)
        {
            foreach (var packedAsset in packedAssets)
            {
                foreach (var content in packedAsset.contents)
                {
                    // Only include files from Assets/** (case-sensitive to exclude build output like assets/bin/Data/)
                    if (!content.sourceAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
                        continue;

                    var category = AssetCategorizer.CategorizeAssetByType(content.sourceAssetPath);
                    var data = categories[category];
                    data.size += (long)content.packedSize;
                    data.count++;
                    categories[category] = data;

                    allAssets.Add(new TopFile
                    {
                        path     = content.sourceAssetPath,
                        size     = (long)content.packedSize,
                        category = category
                    });
                }
            }
        }

#if UNITY_2022_2_OR_NEWER
        private static void CollectAssetsFromBuildFiles(
            BuildFile[] buildFiles,
            Dictionary<string, AssetCategoryData> categories,
            List<TopFile> allAssets)
        {
            foreach (var file in buildFiles)
            {
                if (string.IsNullOrEmpty(file.path))
                    continue;

                // Only include files from Assets/** (case-sensitive to exclude build output like assets/bin/Data/)
                if (!file.path.StartsWith("Assets/", StringComparison.Ordinal) &&
                    !file.path.Contains("/Assets/", StringComparison.Ordinal) &&
                    !file.path.Contains("\\Assets\\", StringComparison.Ordinal))
                    continue;

                var category = AssetCategorizer.CategorizeAssetByType(file.path);
                var data = categories[category];
                data.size += (long)file.size;
                data.count++;
                categories[category] = data;

                allAssets.Add(new TopFile
                {
                    path     = ExtractAssetsPath(file.path),
                    size     = (long)file.size,
                    category = category
                });
            }
        }

        private static string ExtractAssetsPath(string fullPath)
        {
            var assetsIndex = fullPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                return fullPath.Substring(assetsIndex + 1);

            assetsIndex = fullPath.IndexOf("\\Assets\\", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                return fullPath.Substring(assetsIndex + 1);

            return fullPath;
        }
#endif

        // ─── Asset Breakdown Cache ───────────────────────────────────────────────────

        [System.Serializable]
        private class AssetBreakdownCache
        {
            public string projectPath;
            public AssetBreakdown breakdown;
        }

        private static string GetCacheFilePath()
        {
            var projectPath = Application.dataPath;
            var projectRoot = Path.GetDirectoryName(projectPath);
            var buildReportsDir = Path.Combine(projectRoot, "BuildReports");

            if (!Directory.Exists(buildReportsDir))
                Directory.CreateDirectory(buildReportsDir);

            return Path.Combine(buildReportsDir, "asset_breakdown_cache.json");
        }

        private static void SaveAssetBreakdownToCache(AssetBreakdown breakdown)
        {
            try
            {
                var cache = new AssetBreakdownCache
                {
                    projectPath = Application.dataPath,
                    breakdown   = breakdown
                };

                File.WriteAllText(GetCacheFilePath(), JsonUtility.ToJson(cache, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to save asset breakdown cache: {ex.Message}");
            }
        }

        private static AssetBreakdown ReadAssetBreakdownFromCache()
        {
            try
            {
                var cachePath = GetCacheFilePath();

                if (!File.Exists(cachePath))
                    return null;

                var cache = JsonUtility.FromJson<AssetBreakdownCache>(File.ReadAllText(cachePath));

                if (cache?.breakdown == null)
                    return null;

                if (cache.projectPath != Application.dataPath)
                {
                    Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Asset breakdown cache is from a different project, ignoring");
                    return null;
                }

                return cache.breakdown;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to read asset breakdown cache: {ex.Message}");
                return null;
            }
        }
    }
}

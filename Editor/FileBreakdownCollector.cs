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

                // Try PackedAssets first (works for iOS, Standalone)
                var packedAssets = report.packedAssets;
                if (packedAssets != null && packedAssets.Length > 0)
                {
                    // Use packedAssets API
                    CollectFromPackedAssets(packedAssets, categories);
                }
                else
                {
                    // Fallback: Use GetFiles() API (Unity 2022.2+, works for Android, WebGL, all platforms)
                    UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} PackedAssets not available, trying GetFiles() API");

#if UNITY_2022_2_OR_NEWER
                    var buildFiles = report.GetFiles();
                    if (buildFiles != null && buildFiles.Length > 0)
                    {
                        CollectFromBuildFiles(buildFiles, categories);
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

                var breakdown = new FileBreakdown
                {
                    scripts = categories["scripts"],
                    resources = categories["resources"],
                    streamingAssets = categories["streamingAssets"],
                    plugins = categories["plugins"],
                    scenes = categories["scenes"],
                    shaders = categories["shaders"],
                    other = categories["other"]
                };

                var totalSize = categories.Values.Sum(c => c.size);
                var totalCount = categories.Values.Sum(c => c.count);
                UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected file breakdown: {totalCount} files, {totalSize / 1024 / 1024:F2} MB");

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
            Dictionary<string, FileCategoryData> categories)
        {
            int fileCount = 0;
            foreach (var packedAsset in packedAssets)
            {
                foreach (var content in packedAsset.contents)
                {
                    var category = CategorizeAsset(content.sourceAssetPath);
                    var data = categories[category];
                    data.size += (long)content.packedSize;
                    data.count++;
                    categories[category] = data;
                    fileCount++;
                }
            }
            UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected {fileCount} files from PackedAssets API");
        }

#if UNITY_2022_2_OR_NEWER
        private static void CollectFromBuildFiles(
            BuildFile[] buildFiles,
            Dictionary<string, FileCategoryData> categories)
        {
            int fileCount = 0;
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
                fileCount++;
            }
            UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected {fileCount} files from GetFiles() API (after filtering)");
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

        /// <summary>
        /// Collect asset breakdown - only files from Assets/** categorized by asset type.
        /// Returns null if no project assets found.
        /// </summary>
        public static AssetBreakdown CollectAssetBreakdown(BuildReport report)
        {
            if (report == null)
            {
                return null;
            }

            try
            {
                var categories = new Dictionary<string, AssetCategoryData>
                {
                    ["textures"] = new AssetCategoryData { size = 0, count = 0 },
                    ["audio"] = new AssetCategoryData { size = 0, count = 0 },
                    ["models"] = new AssetCategoryData { size = 0, count = 0 },
                    ["animations"] = new AssetCategoryData { size = 0, count = 0 },
                    ["prefabs"] = new AssetCategoryData { size = 0, count = 0 },
                    ["scenes"] = new AssetCategoryData { size = 0, count = 0 },
                    ["scripts"] = new AssetCategoryData { size = 0, count = 0 },
                    ["shaders"] = new AssetCategoryData { size = 0, count = 0 },
                    ["materials"] = new AssetCategoryData { size = 0, count = 0 },
                    ["fonts"] = new AssetCategoryData { size = 0, count = 0 },
                    ["videos"] = new AssetCategoryData { size = 0, count = 0 },
                    ["otherAssets"] = new AssetCategoryData { size = 0, count = 0 }
                };

                var allAssets = new List<TopFile>();

                // Try PackedAssets first
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
                    {
                        CollectAssetsFromBuildFiles(buildFiles, categories, allAssets);
                    }
#endif
                }

                // If no assets found, return minimal breakdown
                if (allAssets.Count == 0)
                {
                    UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} No Assets/** files found in build");
                    return new AssetBreakdown
                    {
                        hasAssets = false,
                        totalAssetsSize = 0,
                        totalAssets = 0,
                        topAssets = new TopFile[0]
                    };
                }

                // Get top 20 largest assets
                var topAssets = allAssets
                    .OrderByDescending(f => f.size)
                    .Take(20)
                    .ToArray();

                var breakdown = new AssetBreakdown
                {
                    hasAssets = true,
                    totalAssetsSize = allAssets.Sum(a => a.size),
                    totalAssets = allAssets.Count,
                    textures = categories["textures"],
                    audio = categories["audio"],
                    models = categories["models"],
                    animations = categories["animations"],
                    prefabs = categories["prefabs"],
                    scenes = categories["scenes"],
                    scripts = categories["scripts"],
                    shaders = categories["shaders"],
                    materials = categories["materials"],
                    fonts = categories["fonts"],
                    videos = categories["videos"],
                    otherAssets = categories["otherAssets"],
                    topAssets = topAssets
                };

                UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Collected asset breakdown: {allAssets.Count} assets from Assets/**, {breakdown.totalAssetsSize / 1024 / 1024:F2} MB total");

                return breakdown;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect asset breakdown: {ex.Message}");
                return null;
            }
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
                    // Only include files from Assets/**
                    if (!content.sourceAssetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var category = CategorizeAssetByType(content.sourceAssetPath);
                    var data = categories[category];
                    data.size += (long)content.packedSize;
                    data.count++;
                    categories[category] = data;

                    allAssets.Add(new TopFile
                    {
                        path = content.sourceAssetPath,
                        size = (long)content.packedSize,
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

                // Only include files that have "Assets/" in the path
                if (!file.path.Contains("/Assets/", StringComparison.OrdinalIgnoreCase) &&
                    !file.path.Contains("\\Assets\\", StringComparison.OrdinalIgnoreCase))
                    continue;

                var category = CategorizeAssetByType(file.path);
                var data = categories[category];
                data.size += (long)file.size;
                data.count++;
                categories[category] = data;

                allAssets.Add(new TopFile
                {
                    path = ExtractAssetsPath(file.path),
                    size = (long)file.size,
                    category = category
                });
            }
        }

        private static string ExtractAssetsPath(string fullPath)
        {
            var assetsIndex = fullPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                return fullPath.Substring(assetsIndex + 1); // "Assets/..."

            assetsIndex = fullPath.IndexOf("\\Assets\\", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                return fullPath.Substring(assetsIndex + 1); // "Assets\..."

            return fullPath;
        }
#endif

        private static string CategorizeAssetByType(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "otherAssets";

            var lowerPath = path.ToLowerInvariant();

            // Textures
            if (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg") ||
                lowerPath.EndsWith(".tga") || lowerPath.EndsWith(".psd") || lowerPath.EndsWith(".tif") ||
                lowerPath.EndsWith(".tiff") || lowerPath.EndsWith(".gif") || lowerPath.EndsWith(".bmp") ||
                lowerPath.EndsWith(".exr") || lowerPath.EndsWith(".hdr"))
            {
                return "textures";
            }

            // Audio
            if (lowerPath.EndsWith(".mp3") || lowerPath.EndsWith(".wav") || lowerPath.EndsWith(".ogg") ||
                lowerPath.EndsWith(".aiff") || lowerPath.EndsWith(".aif") || lowerPath.EndsWith(".mod") ||
                lowerPath.EndsWith(".it") || lowerPath.EndsWith(".s3m") || lowerPath.EndsWith(".xm"))
            {
                return "audio";
            }

            // Models
            if (lowerPath.EndsWith(".fbx") || lowerPath.EndsWith(".dae") || lowerPath.EndsWith(".3ds") ||
                lowerPath.EndsWith(".dxf") || lowerPath.EndsWith(".obj") || lowerPath.EndsWith(".skp") ||
                lowerPath.EndsWith(".blend") || lowerPath.EndsWith(".mb") || lowerPath.EndsWith(".ma"))
            {
                return "models";
            }

            // Animations
            if (lowerPath.EndsWith(".anim") || lowerPath.EndsWith(".controller") ||
                lowerPath.EndsWith(".overridecontroller"))
            {
                return "animations";
            }

            // Prefabs
            if (lowerPath.EndsWith(".prefab"))
            {
                return "prefabs";
            }

            // Scenes
            if (lowerPath.EndsWith(".unity"))
            {
                return "scenes";
            }

            // Scripts
            if (lowerPath.EndsWith(".cs") || lowerPath.EndsWith(".js") || lowerPath.EndsWith(".boo"))
            {
                return "scripts";
            }

            // Shaders
            if (lowerPath.EndsWith(".shader") || lowerPath.EndsWith(".cginc") || lowerPath.EndsWith(".hlsl") ||
                lowerPath.EndsWith(".compute") || lowerPath.EndsWith(".shadergraph") || lowerPath.EndsWith(".shadersubgraph"))
            {
                return "shaders";
            }

            // Materials
            if (lowerPath.EndsWith(".mat"))
            {
                return "materials";
            }

            // Fonts
            if (lowerPath.EndsWith(".ttf") || lowerPath.EndsWith(".otf") ||
                (lowerPath.EndsWith(".asset") && lowerPath.Contains("textmesh")))
            {
                return "fonts";
            }

            // Videos
            if (lowerPath.EndsWith(".mp4") || lowerPath.EndsWith(".mov") || lowerPath.EndsWith(".avi") ||
                lowerPath.EndsWith(".webm") || lowerPath.EndsWith(".ogv"))
            {
                return "videos";
            }

            // Other assets
            return "otherAssets";
        }
    }
}

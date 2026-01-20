using System;
using System.Collections.Generic;
using System.IO;
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
#if UNITY_2022_2_OR_NEWER
                    var buildFiles = report.GetFiles();
                    if (buildFiles != null && buildFiles.Length > 0)
                    {
                        CollectFromBuildFiles(buildFiles, categories);
                    }
                    else
                    {
                        return null;
                    }
#else
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
            foreach (var packedAsset in packedAssets)
            {
                foreach (var content in packedAsset.contents)
                {
                    var category = CategorizeAsset(content.sourceAssetPath);
                    var data = categories[category];
                    data.size += (long)content.packedSize;
                    data.count++;
                    categories[category] = data;
                }
            }
        }

#if UNITY_2022_2_OR_NEWER
        private static void CollectFromBuildFiles(
            BuildFile[] buildFiles,
            Dictionary<string, FileCategoryData> categories)
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
        /// Falls back to Editor.log parsing if BuildReport APIs fail.
        /// Falls back to cached data if Editor.log is unavailable or from different project.
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

                // If no assets found from build report, try parsing Editor.log
                if (allAssets.Count == 0)
                {
                    CollectAssetsFromEditorLog(categories, allAssets);

                    // If still no assets, try reading from cache
                    if (allAssets.Count == 0)
                    {
                        var cachedBreakdown = ReadAssetBreakdownFromCache();
                        if (cachedBreakdown != null)
                        {
                            UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Using cached asset breakdown from previous build");
                            return cachedBreakdown;
                        }

                        return new AssetBreakdown
                        {
                            hasAssets = false,
                            totalAssetsSize = 0,
                            totalAssets = 0,
                            topAssets = new TopFile[0]
                        };
                    }
                }

                // Deduplicate assets by path, keeping the largest size entry for each asset
                // (Unity reports same asset multiple times: original + compressed + mipmaps)
                var deduplicatedAssets = allAssets
                    .GroupBy(a => a.path)
                    .Select(g => g.OrderByDescending(a => a.size).First())
                    .ToList();

                // Get top 20 largest unique assets
                var topAssets = deduplicatedAssets
                    .OrderByDescending(f => f.size)
                    .Take(20)
                    .ToArray();

                var breakdown = new AssetBreakdown
                {
                    hasAssets = true,
                    totalAssetsSize = deduplicatedAssets.Sum(a => a.size),
                    totalAssets = deduplicatedAssets.Count,
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

                // Save successful breakdown to cache for future use
                SaveAssetBreakdownToCache(breakdown);

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
                    // Only include files from Assets/** (case-sensitive to exclude build output like assets/bin/Data/)
                    if (!content.sourceAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
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

                // Only include files from Assets/** (case-sensitive to exclude build output like assets/bin/Data/)
                if (!file.path.StartsWith("Assets/", StringComparison.Ordinal) &&
                    !file.path.Contains("/Assets/", StringComparison.Ordinal) &&
                    !file.path.Contains("\\Assets\\", StringComparison.Ordinal))
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

        /// <summary>
        /// Parse Editor.log to extract asset breakdown when BuildReport APIs fail (e.g., IL2CPP builds).
        /// Unity writes detailed asset info to Editor.log after every build.
        /// NOTE: All Unity projects share the same Editor.log, so we validate the data belongs to THIS project.
        /// </summary>
        private static void CollectAssetsFromEditorLog(
            Dictionary<string, AssetCategoryData> categories,
            List<TopFile> allAssets)
        {
            try
            {
                var editorLogPath = GetEditorLogPath();

                if (string.IsNullOrEmpty(editorLogPath) || !File.Exists(editorLogPath))
                {
                    return;
                }

                // Get current project identifiers for validation
                var currentProjectPath = UnityEngine.Application.dataPath; // Ends with "/Assets"
                var projectRoot = System.IO.Path.GetDirectoryName(currentProjectPath);
                var projectName = System.IO.Path.GetFileName(projectRoot);

                // Read the last portion of the log (build info is at the end)
                var logLines = File.ReadAllLines(editorLogPath);

                // Find the "Used Assets and files from the Resources folder" section
                // Unity only writes this on clean builds, not incremental builds
                // Search backwards through ENTIRE log to find most recent clean build
                int assetsFound = 0;
                int assetsSectionStartLine = -1;

                // Search backwards to find the LAST occurrence of "Used Assets and files from the Resources folder"
                for (int i = logLines.Length - 1; i >= 0; i--)
                {
                    if (logLines[i].Contains("Used Assets and files from the Resources folder"))
                    {
                        assetsSectionStartLine = i;
                        break;
                    }
                }

                if (assetsSectionStartLine == -1)
                {
                    UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Editor.log: No 'Used Assets' section found. This is normal for incremental builds.");
                    return;
                }

                // CRITICAL: Validate this section belongs to the current project
                // Search backwards from asset section to find project markers (within 1000 lines)
                bool projectMatches = false;
                int searchStart = Math.Max(0, assetsSectionStartLine - 1000);

                for (int i = assetsSectionStartLine - 1; i >= searchStart; i--)
                {
                    var line = logLines[i];

                    // Check for project path markers (Unity logs these during build)
                    if (line.Contains(projectRoot) ||
                        line.Contains(currentProjectPath) ||
                        (line.Contains("COMMAND LINE ARGUMENTS") && i + 10 < logLines.Length &&
                         string.Join("\n", logLines, i, Math.Min(10, logLines.Length - i)).Contains(projectRoot)))
                    {
                        projectMatches = true;
                        break;
                    }
                }

                if (!projectMatches)
                {
                    UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Editor.log contains data from a different project. Will use cached breakdown if available. " +
                        "For accurate data, do a clean build (delete Library folder) or restart Unity.");
                    return;
                }

                // Project validated - parse assets from this section
                UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Editor.log validated for current project, parsing asset breakdown...");

                // Parse assets starting from the line after the header
                for (int i = assetsSectionStartLine + 1; i < logLines.Length; i++)
                {
                    var line = logLines[i];

                    // End of assets section (empty line or dashed line)
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("---"))
                    {
                        break;
                    }

                    // Parse asset lines (format: " 1.2 mb   0.5% Assets/Textures/hero.png")
                    // Only include Assets/ paths (skip Built-in, Resources/, Packages/)
                    if (line.Contains("Assets/") && !line.Contains("Built-in"))
                    {
                        var assetInfo = ParseEditorLogAssetLine(line);
                        if (assetInfo != null)
                        {
                            var category = CategorizeAssetByType(assetInfo.Value.path);
                            var data = categories[category];
                            data.size += assetInfo.Value.size;
                            data.count++;
                            categories[category] = data;

                            allAssets.Add(new TopFile
                            {
                                path = assetInfo.Value.path,
                                size = assetInfo.Value.size,
                                category = category
                            });

                            assetsFound++;
                        }
                    }
                }

                if (assetsFound > 0)
                {
                    UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Successfully collected {assetsFound} assets from Editor.log");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect assets: {ex.Message}");
            }
        }

        private static string GetEditorLogPath()
        {
#if UNITY_EDITOR_OSX
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library/Logs/Unity/Editor.log");
#elif UNITY_EDITOR_WIN
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unity/Editor/Editor.log");
#elif UNITY_EDITOR_LINUX
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ".config/unity3d/Editor.log");
#else
            return null;
#endif
        }

        private static (string path, long size)? ParseEditorLogAssetLine(string line)
        {
            try
            {
                // Format: " 1.2 mb   0.5% Assets/Textures/hero.png"
                // or:     " 0.5 kb   0.0% Assets/Scripts/Player.cs"

                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                    return null;

                // Find the Assets/ part
                string assetPath = null;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        // Join remaining parts in case path has spaces
                        assetPath = string.Join(" ", parts, i, parts.Length - i);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(assetPath))
                    return null;

                // Parse size (first value, e.g., "1.2")
                if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float sizeValue))
                    return null;

                // Parse unit (second value, e.g., "mb", "kb", "gb")
                // Trim to handle extra spaces/tabs between columns
                var unit = parts[1].Trim().ToLowerInvariant();

                long sizeBytes = 0;

                switch (unit)
                {
                    case "b":
                    case "bytes":
                        sizeBytes = (long)sizeValue;
                        break;
                    case "kb":
                        sizeBytes = (long)(sizeValue * 1024);
                        break;
                    case "mb":
                        sizeBytes = (long)(sizeValue * 1024 * 1024);
                        break;
                    case "gb":
                        sizeBytes = (long)(sizeValue * 1024 * 1024 * 1024);
                        break;
                    default:
                        return null;
                }

                return (assetPath, sizeBytes);
            }
            catch
            {
                return null;
            }
        }

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

        #region Asset Breakdown Cache

        [System.Serializable]
        private class AssetBreakdownCache
        {
            public string projectPath;
            public AssetBreakdown breakdown;
        }

        private static string GetCacheFilePath()
        {
            var projectPath = UnityEngine.Application.dataPath; // Ends with "/Assets"
            var projectRoot = System.IO.Path.GetDirectoryName(projectPath);
            var buildReportsDir = System.IO.Path.Combine(projectRoot, "BuildReports");

            if (!Directory.Exists(buildReportsDir))
            {
                Directory.CreateDirectory(buildReportsDir);
            }

            return System.IO.Path.Combine(buildReportsDir, "asset_breakdown_cache.json");
        }

        private static void SaveAssetBreakdownToCache(AssetBreakdown breakdown)
        {
            try
            {
                var cache = new AssetBreakdownCache
                {
                    projectPath = UnityEngine.Application.dataPath,
                    breakdown = breakdown
                };

                var json = UnityEngine.JsonUtility.ToJson(cache, true);
                var cachePath = GetCacheFilePath();
                File.WriteAllText(cachePath, json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to save asset breakdown cache: {ex.Message}");
            }
        }

        private static AssetBreakdown ReadAssetBreakdownFromCache()
        {
            try
            {
                var cachePath = GetCacheFilePath();

                if (!File.Exists(cachePath))
                {
                    return null;
                }

                var json = File.ReadAllText(cachePath);
                var cache = UnityEngine.JsonUtility.FromJson<AssetBreakdownCache>(json);

                if (cache == null || cache.breakdown == null)
                {
                    return null;
                }

                // Validate cache is for the current project
                var currentProjectPath = UnityEngine.Application.dataPath;
                if (cache.projectPath != currentProjectPath)
                {
                    UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Asset breakdown cache is from a different project, ignoring");
                    return null;
                }

                return cache.breakdown;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to read asset breakdown cache: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}

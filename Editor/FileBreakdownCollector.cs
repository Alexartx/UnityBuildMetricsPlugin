using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
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
        /// For accurate results, analyzes the actual build output (APK, IPA, etc.) instead of Unity's incomplete APIs.
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
                var platform = report.summary.platform;
                var outputPath = report.summary.outputPath;

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

                // NEW: Try parsing actual build output first (most accurate)
                if (TryParseBuildOutput(outputPath, platform, categories))
                {
                    return BuildFileBreakdownResult(categories, platform, null);
                }

                // Track "other" subcategories for detailed breakdown
                var otherSubcategories = new Dictionary<string, FileCategorySubData>
                {
                    ["spriteAtlases"] = new FileCategorySubData { size = 0, count = 0 },
                    ["textures"] = new FileCategorySubData { size = 0, count = 0 },
                    ["meshes"] = new FileCategorySubData { size = 0, count = 0 },
                    ["audio"] = new FileCategorySubData { size = 0, count = 0 },
                    ["assetBundles"] = new FileCategorySubData { size = 0, count = 0 },
                    ["unityRuntime"] = new FileCategorySubData { size = 0, count = 0 },
                    ["fonts"] = new FileCategorySubData { size = 0, count = 0 },

                    // iOS-specific
                    ["iosAssetCatalogs"] = new FileCategorySubData { size = 0, count = 0 },
                    ["iosAppResources"] = new FileCategorySubData { size = 0, count = 0 },
                    ["iosSystem"] = new FileCategorySubData { size = 0, count = 0 },

                    // Android-specific
                    ["androidAddressables"] = new FileCategorySubData { size = 0, count = 0 },
                    ["androidUnityData"] = new FileCategorySubData { size = 0, count = 0 },
                    ["androidResources"] = new FileCategorySubData { size = 0, count = 0 },
                    ["androidCode"] = new FileCategorySubData { size = 0, count = 0 },
                    ["androidSystem"] = new FileCategorySubData { size = 0, count = 0 },

                    // WebGL-specific
                    ["webglData"] = new FileCategorySubData { size = 0, count = 0 },
                    ["webglWasm"] = new FileCategorySubData { size = 0, count = 0 },
                    ["webglJs"] = new FileCategorySubData { size = 0, count = 0 },

                    ["other"] = new FileCategorySubData { size = 0, count = 0 }
                };

                // FALLBACK: Try PackedAssets API (works for iOS, Standalone but incomplete)
                var packedAssets = report.packedAssets;
                if (packedAssets != null && packedAssets.Length > 0)
                {
                    // Use packedAssets API
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
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect file breakdown: {ex.Message}");
                return null;
            }
        }

        private static FileBreakdown BuildFileBreakdownResult(
            Dictionary<string, FileCategoryData> categories,
            UnityEditor.BuildTarget platform,
            Dictionary<string, FileCategorySubData> otherSubcategories = null)
        {
            // Add detailed breakdown to "other" category if provided
            if (otherSubcategories != null)
            {
                categories["other"].breakdown = new OtherBreakdown
                {
                    spriteAtlases = otherSubcategories["spriteAtlases"],
                    textures = otherSubcategories["textures"],
                    meshes = otherSubcategories["meshes"],
                    audio = otherSubcategories["audio"],
                    assetBundles = otherSubcategories["assetBundles"],
                    unityRuntime = otherSubcategories["unityRuntime"],
                    fonts = otherSubcategories["fonts"],

                    // iOS-specific
                    iosAssetCatalogs = otherSubcategories["iosAssetCatalogs"],
                    iosAppResources = otherSubcategories["iosAppResources"],
                    iosSystem = otherSubcategories["iosSystem"],

                    // Android-specific
                    androidAddressables = otherSubcategories["androidAddressables"],
                    androidUnityData = otherSubcategories["androidUnityData"],
                    androidResources = otherSubcategories["androidResources"],
                    androidCode = otherSubcategories["androidCode"],
                    androidSystem = otherSubcategories["androidSystem"],

                    // WebGL-specific
                    webglData = otherSubcategories["webglData"],
                    webglWasm = otherSubcategories["webglWasm"],
                    webglJs = otherSubcategories["webglJs"],

                    other = otherSubcategories["other"]
                };

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

        /// <summary>
        /// Parse actual build output for accurate file breakdown.
        /// Unzips and analyzes APK, IPA, or directory contents.
        /// </summary>
        private static bool TryParseBuildOutput(
            string outputPath,
            UnityEditor.BuildTarget platform,
            Dictionary<string, FileCategoryData> categories)
        {
            if (string.IsNullOrEmpty(outputPath) || !File.Exists(outputPath) && !Directory.Exists(outputPath))
            {
                return false;
            }

            try
            {
                switch (platform)
                {
                    case UnityEditor.BuildTarget.Android:
                        return ParseAndroidAPK(outputPath, categories);

                    case UnityEditor.BuildTarget.iOS:
                        return ParseIOSBuild(outputPath, categories);

                    case UnityEditor.BuildTarget.WebGL:
                        return ParseWebGLBuild(outputPath, categories);

                    case UnityEditor.BuildTarget.StandaloneWindows:
                    case UnityEditor.BuildTarget.StandaloneWindows64:
                    case UnityEditor.BuildTarget.StandaloneOSX:
                    case UnityEditor.BuildTarget.StandaloneLinux64:
                        return ParseStandaloneBuild(outputPath, categories);

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

        /// <summary>
        /// Parse Android APK file contents for accurate breakdown.
        /// APK is a ZIP archive - we can extract and analyze all files.
        /// </summary>
        private static bool ParseAndroidAPK(string apkPath, Dictionary<string, FileCategoryData> categories)
        {
            if (!apkPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                // Might be AAB or directory
                if (Directory.Exists(apkPath))
                {
                    return ParseDirectoryContents(apkPath, categories, CategorizeAndroidFile);
                }
                return false;
            }

            try
            {
                using (var archive = System.IO.Compression.ZipFile.OpenRead(apkPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("/")) continue; // Skip directories

                        var category = CategorizeAndroidFile(entry.FullName);
                        var data = categories[category];
                        // Use CompressedLength for accurate size (uncompressed would be 10-20x larger)
                        data.size += entry.CompressedLength;
                        data.count++;
                        categories[category] = data;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to parse APK: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Categorize files inside Android APK.
        /// </summary>
        private static string CategorizeAndroidFile(string path)
        {
            var lower = path.ToLowerInvariant();

            // Native libraries (.so files) - usually 20-30 MB
            if (lower.StartsWith("lib/") || lower.Contains("/lib/"))
            {
                return "plugins"; // Native plugins
            }

            // DEX files (compiled Java/Kotlin code) - usually 5-10 MB
            if (lower.StartsWith("classes") && lower.EndsWith(".dex"))
            {
                return "scripts"; // Compiled scripts
            }

            // Unity data files (scenes, resources, assets)
            if (lower.StartsWith("assets/bin/data/"))
            {
                var fileName = System.IO.Path.GetFileName(lower);

                // Resources: resources.assets, resources.resource, resources.resS, resources-*.split*
                if (fileName.StartsWith("resources"))
                {
                    return "resources";
                }

                // Scenes: sharedassets*.*, level*.*, maindata
                if (fileName.StartsWith("sharedassets") || fileName.StartsWith("level") || fileName == "maindata")
                {
                    return "scenes";
                }

                // Shaders: resources/unity_builtin_extra or shader-related
                if (lower.Contains("shader") || lower.Contains("unity_builtin_extra"))
                {
                    return "shaders";
                }

                // Everything else in bin/Data (globalgamemanagers, etc.)
                return "other"; // Unity runtime data
            }

            // Streaming assets (user-added files, not processed by Unity)
            if (lower.StartsWith("assets/") && !lower.StartsWith("assets/bin/"))
            {
                return "streamingAssets";
            }

            // Android resources (icons, layouts, etc.)
            if (lower.StartsWith("res/") || lower == "resources.arsc")
            {
                return "other"; // Android system resources
            }

            // Shaders
            if (lower.Contains("shader"))
            {
                return "shaders";
            }

            // Everything else (manifest, signatures, etc.)
            return "other";
        }

        /// <summary>
        /// Parse iOS build (either .app directory or .ipa file).
        /// </summary>
        private static bool ParseIOSBuild(string outputPath, Dictionary<string, FileCategoryData> categories)
        {
            // iOS builds are directories (.app) or IPA files
            if (outputPath.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase))
            {
                // IPA is a ZIP archive
                try
                {
                    using (var archive = System.IO.Compression.ZipFile.OpenRead(outputPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.EndsWith("/")) continue;

                            var category = CategorizeIOSFile(entry.FullName);
                            var data = categories[category];
                            // Use CompressedLength for accurate size
                            data.size += entry.CompressedLength;
                            data.count++;
                            categories[category] = data;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to parse IPA: {ex.Message}");
                    return false;
                }
            }
            else if (Directory.Exists(outputPath))
            {
                // .app directory or Xcode project
                return ParseDirectoryContents(outputPath, categories, CategorizeIOSFile);
            }

            return false;
        }

        private static string CategorizeIOSFile(string path)
        {
            var lower = path.ToLowerInvariant();

            // Native frameworks and libraries
            if (lower.Contains("/frameworks/") || lower.EndsWith(".dylib") || lower.EndsWith(".framework"))
            {
                return "plugins";
            }

            // Unity data files
            if (lower.Contains("/data/") && (lower.Contains("sharedassets") || lower.Contains("level")))
            {
                return "scenes";
            }

            if (lower.Contains("/data/") && (lower.Contains("resources") || lower.EndsWith(".resource")))
            {
                return "resources";
            }

            // Streaming assets
            if (lower.Contains("/data/raw/"))
            {
                return "streamingAssets";
            }

            // Shaders
            if (lower.Contains("shader"))
            {
                return "shaders";
            }

            // Asset catalogs and app resources
            if (lower.Contains("assets.car") || lower.EndsWith(".nib") || lower.EndsWith(".storyboardc"))
            {
                return "other";
            }

            return "other";
        }

        /// <summary>
        /// Parse WebGL build directory.
        /// </summary>
        private static bool ParseWebGLBuild(string outputPath, Dictionary<string, FileCategoryData> categories)
        {
            if (!Directory.Exists(outputPath))
            {
                return false;
            }

            return ParseDirectoryContents(outputPath, categories, CategorizeWebGLFile);
        }

        private static string CategorizeWebGLFile(string path)
        {
            var lower = path.ToLowerInvariant();

            // WASM binary (compiled code)
            if (lower.EndsWith(".wasm"))
            {
                return "scripts";
            }

            // JavaScript framework
            if (lower.EndsWith(".js"))
            {
                return "scripts";
            }

            // Data file (contains assets)
            if (lower.EndsWith(".data"))
            {
                return "other"; // Mixed content
            }

            // Symbols/debugging
            if (lower.EndsWith(".symbols.json"))
            {
                return "other";
            }

            return "other";
        }

        /// <summary>
        /// Parse standalone build (Windows/Mac/Linux).
        /// </summary>
        private static bool ParseStandaloneBuild(string outputPath, Dictionary<string, FileCategoryData> categories)
        {
            string dataPath = null;

            if (File.Exists(outputPath))
            {
                // Executable file - find associated Data folder
                var dir = Path.GetDirectoryName(outputPath);
                var exeName = Path.GetFileNameWithoutExtension(outputPath);
                dataPath = Path.Combine(dir, exeName + "_Data");
            }
            else if (Directory.Exists(outputPath))
            {
                // Directory (Mac .app bundle)
                if (outputPath.EndsWith(".app"))
                {
                    dataPath = Path.Combine(outputPath, "Contents", "Data");
                }
                else
                {
                    dataPath = outputPath;
                }
            }

            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
            {
                return false;
            }

            return ParseDirectoryContents(dataPath, categories, CategorizeStandaloneFile);
        }

        private static string CategorizeStandaloneFile(string path)
        {
            var lower = path.ToLowerInvariant();

            // Plugins/DLLs
            if (lower.Contains("/plugins/") || lower.EndsWith(".dll") || lower.EndsWith(".so") || lower.EndsWith(".bundle"))
            {
                return "plugins";
            }

            // Managed assemblies
            if (lower.Contains("/managed/") && lower.EndsWith(".dll"))
            {
                return "scripts";
            }

            // Unity data files
            if (lower.Contains("sharedassets") || lower.Contains("level"))
            {
                return "scenes";
            }

            if (lower.Contains("resources") || lower.EndsWith(".resource") || lower.EndsWith(".ress"))
            {
                return "resources";
            }

            // Streaming assets
            if (lower.Contains("/streamingassets/"))
            {
                return "streamingAssets";
            }

            // Shaders
            if (lower.Contains("shader"))
            {
                return "shaders";
            }

            return "other";
        }

        /// <summary>
        /// Generic directory parsing helper.
        /// </summary>
        private static bool ParseDirectoryContents(
            string dirPath,
            Dictionary<string, FileCategoryData> categories,
            Func<string, string> categorizer)
        {
            try
            {
                var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var relativePath = filePath.Substring(dirPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    var category = categorizer(relativePath);
                    var data = categories[category];
                    data.size += fileInfo.Length;
                    data.count++;
                    categories[category] = data;
                }

                return files.Length > 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to parse directory: {ex.Message}");
                return false;
            }
        }

        private static void CollectFromPackedAssets(
            PackedAssets[] packedAssets,
            Dictionary<string, FileCategoryData> categories,
            Dictionary<string, FileCategorySubData> otherSubcategories,
            UnityEditor.BuildTarget platform)
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

                    // If categorized as "other", subcategorize it
                    if (category == "other")
                    {
                        var subcategory = CategorizeOtherFile(content.sourceAssetPath, platform);
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
            UnityEditor.BuildTarget platform)
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

                // If categorized as "other", subcategorize it
                if (category == "other")
                {
                    var subcategory = CategorizeOtherFile(file.path, platform);
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
        /// Categorize files within "other" category for detailed breakdown.
        /// Platform-aware categorization for iOS, Android, and WebGL.
        /// </summary>
        private static string CategorizeOtherFile(string path, UnityEditor.BuildTarget platform)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "other";

            var lowerPath = path.ToLowerInvariant();

            // Platform-specific categorization
            if (platform == UnityEditor.BuildTarget.iOS)
            {
                // iOS Asset Catalogs (actionable - user can optimize)
                if (lowerPath.Contains("assets.car") || lowerPath.Contains("/assets.car"))
                    return "iosAssetCatalogs";

                // iOS App Resources (actionable - UI images, storyboards, nibs)
                if (lowerPath.EndsWith(".storyboardc") || lowerPath.EndsWith(".nib") ||
                    lowerPath.EndsWith(".storyboard") || lowerPath.Contains("/base.lproj/") ||
                    (lowerPath.Contains(".app/") && (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg"))))
                    return "iosAppResources";

                // iOS System files (NOT actionable - collapse by default)
                if (lowerPath.Contains("/frameworks/") || lowerPath.Contains(".framework/") ||
                    lowerPath.Contains("/swiftsupport/") || lowerPath.Contains("/plugins/") ||
                    lowerPath.Contains("_codesignature/") || lowerPath.Contains("/meta-inf/") ||
                    lowerPath.EndsWith(".dylib") || lowerPath.Contains("/extensions/"))
                    return "iosSystem";
            }
            else if (platform == UnityEditor.BuildTarget.Android)
            {
                // Android Addressables/Bundles (actionable)
                if (lowerPath.Contains("/assets/aa/") || lowerPath.Contains("\\assets\\aa\\") ||
                    (lowerPath.Contains("/assets/") && lowerPath.EndsWith(".bundle")))
                    return "androidAddressables";

                // Android Unity Data (actionable - scenes, resources)
                if (lowerPath.Contains("/assets/bin/data/") || lowerPath.Contains("\\assets\\bin\\data\\") ||
                    lowerPath.Contains("sharedassets") || lowerPath.EndsWith(".resS"))
                    return "androidUnityData";

                // Android Resources (actionable - icons, splash, localization)
                if (lowerPath.Contains("/res/") || lowerPath.Contains("\\res\\") ||
                    lowerPath.Contains("resources.arsc"))
                    return "androidResources";

                // Android Code (semi-actionable - IL2CPP stripping)
                if (lowerPath.Contains("classes") && lowerPath.EndsWith(".dex"))
                    return "androidCode";

                // Android System (NOT actionable - native libs)
                if (lowerPath.Contains("/lib/") || lowerPath.Contains("/jnilibs/") ||
                    lowerPath.Contains("\\lib\\") || lowerPath.Contains("\\jnilibs\\"))
                    return "androidSystem";
            }
            else if (platform == UnityEditor.BuildTarget.WebGL)
            {
                // WebGL Data file (actionable)
                if (lowerPath.EndsWith(".data"))
                    return "webglData";

                // WebGL WASM (semi-actionable via code stripping)
                if (lowerPath.EndsWith(".wasm"))
                    return "webglWasm";

                // WebGL JS (semi-actionable)
                if (lowerPath.EndsWith(".js"))
                    return "webglJs";
            }

            // Cross-platform categories (work for all platforms)

            // Sprite Atlases
            if (lowerPath.EndsWith(".spriteatlas") || lowerPath.EndsWith(".spriteatlasv2"))
                return "spriteAtlases";

            // Textures (compressed formats in build)
            if (lowerPath.EndsWith(".pvrtc") || lowerPath.EndsWith(".etc") || lowerPath.EndsWith(".etc2") ||
                lowerPath.EndsWith(".astc") || lowerPath.EndsWith(".dds") || lowerPath.EndsWith(".ktx") ||
                lowerPath.Contains("texture") || lowerPath.Contains(".png") || lowerPath.Contains(".jpg"))
                return "textures";

            // Meshes (compiled)
            if (lowerPath.Contains("mesh") || lowerPath.EndsWith(".mesh"))
                return "meshes";

            // Audio (in build)
            if (lowerPath.EndsWith(".mp3") || lowerPath.EndsWith(".ogg") || lowerPath.EndsWith(".wav") ||
                lowerPath.EndsWith(".m4a") || lowerPath.EndsWith(".aac"))
                return "audio";

            // Asset Bundles (Addressables) - fallback if not caught by platform-specific
            if (lowerPath.EndsWith(".bundle") || lowerPath.Contains("assetbundle") ||
                lowerPath.Contains("/aa/") || lowerPath.Contains("\\aa\\"))
                return "assetBundles";

            // Unity Runtime Files
            if (lowerPath.Contains("sharedassets") || lowerPath.Contains("globalgamemanagers") ||
                lowerPath.Contains("level") || lowerPath.EndsWith(".resource") ||
                lowerPath.EndsWith(".assets") || lowerPath.EndsWith(".resS"))
                return "unityRuntime";

            // Fonts
            if (lowerPath.EndsWith(".ttf") || lowerPath.EndsWith(".otf") ||
                lowerPath.Contains("font"))
                return "fonts";

            // Truly unknown
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

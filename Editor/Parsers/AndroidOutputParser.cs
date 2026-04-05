using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BuildMetrics.Editor
{
    internal static class AndroidOutputParser
    {
        internal static bool Parse(string apkPath, Dictionary<string, FileCategoryData> categories)
        {
            if (!apkPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) &&
                !apkPath.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(apkPath))
                    return BuildDirectoryParser.ParseDirectoryContents(apkPath, categories, CategorizeFile);
                return false;
            }

            try
            {
                using (var archive = System.IO.Compression.ZipFile.OpenRead(apkPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("/")) continue;

                        var category = CategorizeFile(entry.FullName);
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

        private static string CategorizeFile(string path)
        {
            var lower = path.ToLowerInvariant();

            // Native libraries (.so files)
            if (lower.StartsWith("lib/") || lower.Contains("/lib/"))
                return "plugins";

            // DEX files (compiled Java/Kotlin code)
            if ((lower.StartsWith("classes") || lower.Contains("/dex/classes")) && lower.EndsWith(".dex"))
                return "scripts";

            // Unity data files (scenes, resources, assets)
            if (lower.StartsWith("assets/bin/data/") || lower.Contains("/assets/bin/data/"))
            {
                var fileName = Path.GetFileName(lower);

                if (fileName.StartsWith("resources"))
                    return "resources";

                if (fileName.StartsWith("sharedassets") || fileName.StartsWith("level") || fileName == "maindata")
                    return "scenes";

                if (lower.Contains("shader") || lower.Contains("unity_builtin_extra"))
                    return "shaders";

                return "other";
            }

            // Streaming assets (user-added files, not processed by Unity)
            if ((lower.StartsWith("assets/") || lower.Contains("/assets/")) &&
                !lower.StartsWith("assets/bin/") &&
                !lower.Contains("/assets/bin/"))
                return "streamingAssets";

            // Android resources (icons, layouts, etc.)
            if (lower.StartsWith("res/") || lower.Contains("/res/") ||
                lower == "resources.arsc" || lower.EndsWith("/resources.arsc"))
                return "other";

            if (lower.Contains("shader"))
                return "shaders";

            return "other";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BuildMetrics.Editor
{
    internal static class IosOutputParser
    {
        internal static bool Parse(string outputPath, Dictionary<string, FileCategoryData> categories)
        {
            // IPA is a ZIP archive
            if (outputPath.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var archive = System.IO.Compression.ZipFile.OpenRead(outputPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.EndsWith("/")) continue;

                            var category = CategorizeFile(entry.FullName);
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

            if (Directory.Exists(outputPath))
                return BuildDirectoryParser.ParseDirectoryContents(outputPath, categories, CategorizeFile);

            return false;
        }

        private static string CategorizeFile(string path)
        {
            var lower = path.ToLowerInvariant();

            if (lower.Contains("/frameworks/") || lower.EndsWith(".dylib") || lower.EndsWith(".framework"))
                return "plugins";

            if (lower.Contains("/data/") && (lower.Contains("sharedassets") || lower.Contains("level")))
                return "scenes";

            if (lower.Contains("/data/") && (lower.Contains("resources") || lower.EndsWith(".resource")))
                return "resources";

            if (lower.Contains("/data/raw/"))
                return "streamingAssets";

            if (lower.Contains("shader"))
                return "shaders";

            return "other";
        }
    }
}

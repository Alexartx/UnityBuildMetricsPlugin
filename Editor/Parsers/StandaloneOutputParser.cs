using System.Collections.Generic;
using System.IO;

namespace BuildMetrics.Editor
{
    internal static class StandaloneOutputParser
    {
        internal static bool Parse(string outputPath, Dictionary<string, FileCategoryData> categories)
        {
            string dataPath = null;

            if (File.Exists(outputPath))
            {
                var dir = Path.GetDirectoryName(outputPath);
                var exeName = Path.GetFileNameWithoutExtension(outputPath);
                dataPath = Path.Combine(dir, exeName + "_Data");
            }
            else if (Directory.Exists(outputPath))
            {
                if (outputPath.EndsWith(".app"))
                {
                    // Parse the full app bundle so the breakdown covers the executable, runtime files,
                    // and Data folder instead of leaving most of the app size as "untracked".
                    return BuildDirectoryParser.ParseDirectoryContents(outputPath, categories, CategorizeFile);
                }
                dataPath = outputPath;
            }

            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
                return false;

            return BuildDirectoryParser.ParseDirectoryContents(dataPath, categories, CategorizeFile);
        }

        private static string CategorizeFile(string path)
        {
            var lower = path.Replace('\\', '/').ToLowerInvariant();

            if ((lower.StartsWith("managed/") || lower.Contains("/managed/")) && lower.EndsWith(".dll"))
                return "scripts";

            if (lower.Contains("/plugins/") || lower.EndsWith(".dll") || lower.EndsWith(".so") || lower.EndsWith(".bundle"))
                return "plugins";

            if (lower.Contains("sharedassets") || lower.Contains("level"))
                return "scenes";

            // Streaming assets must win before the generic "resources" check because
            // macOS app bundles place them under Contents/Resources/Data/StreamingAssets.
            if (lower.StartsWith("streamingassets/") || lower.Contains("/streamingassets/"))
                return "streamingAssets";

            if (lower.Contains("resources") || lower.EndsWith(".resource") || lower.EndsWith(".ress"))
                return "resources";

            if (lower.Contains("shader"))
                return "shaders";

            return "other";
        }
    }
}

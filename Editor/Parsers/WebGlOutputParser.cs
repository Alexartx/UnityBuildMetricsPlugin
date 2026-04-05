using System.Collections.Generic;
using System.IO;

namespace BuildMetrics.Editor
{
    internal static class WebGlOutputParser
    {
        internal static bool Parse(string outputPath, Dictionary<string, FileCategoryData> categories)
        {
            if (!Directory.Exists(outputPath))
                return false;

            return BuildDirectoryParser.ParseDirectoryContents(outputPath, categories, CategorizeFile);
        }

        private static string CategorizeFile(string path)
        {
            var lower = path.ToLowerInvariant();

            if (lower.StartsWith("streamingassets/") ||
                lower.Contains("/streamingassets/") ||
                lower.Contains("\\streamingassets\\"))
                return "streamingAssets";

            if (lower.EndsWith(".wasm") || lower.EndsWith(".js"))
                return "scripts";

            return "other";
        }
    }
}

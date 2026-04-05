using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BuildMetrics.Editor
{
    internal static class BuildDirectoryParser
    {
        internal static bool ParseDirectoryContents(
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
                    var relativePath = filePath.Substring(dirPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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
    }
}

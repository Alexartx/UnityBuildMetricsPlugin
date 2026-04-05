using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BuildMetrics.Editor
{
    /// <summary>
    /// Parses Editor.log to extract asset breakdown when BuildReport APIs fail (e.g., IL2CPP builds).
    /// Unity writes detailed asset info to Editor.log after every build.
    /// NOTE: All Unity projects share the same Editor.log, so we validate the data belongs to THIS project.
    /// </summary>
    internal static class EditorLogParser
    {
        internal static void CollectAssetsFromEditorLog(
            Dictionary<string, AssetCategoryData> categories,
            List<TopFile> allAssets)
        {
            try
            {
                var editorLogPath = GetEditorLogPath();

                if (string.IsNullOrEmpty(editorLogPath) || !File.Exists(editorLogPath))
                    return;

                var currentProjectPath = Application.dataPath;
                var projectRoot = Path.GetDirectoryName(currentProjectPath);

                // Read the last portion of the log (build info is at the end)
                // NOTE: ReadAllLines can be large for busy projects; stream if perf becomes an issue.
                var logLines = File.ReadAllLines(editorLogPath);

                // Unity only writes "Used Assets" on clean builds, not incremental builds.
                // Search backwards through the ENTIRE log to find the most recent clean build.
                int assetsSectionStartLine = -1;
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
                    Debug.Log($"{BuildMetricsConstants.LogPrefix} Editor.log: No 'Used Assets' section found. This is normal for incremental builds.");
                    return;
                }

                // CRITICAL: Validate this section belongs to the current project.
                // Search backwards from the asset section to find project markers (within 1000 lines).
                bool projectMatches = false;
                int searchStart = Math.Max(0, assetsSectionStartLine - 1000);

                for (int i = assetsSectionStartLine - 1; i >= searchStart; i--)
                {
                    var line = logLines[i];
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
                    Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Editor.log contains data from a different project. Will use cached breakdown if available. " +
                        "For accurate data, do a clean build (delete Library folder) or restart Unity.");
                    return;
                }

                Debug.Log($"{BuildMetricsConstants.LogPrefix} Editor.log validated for current project, parsing asset breakdown...");

                int assetsFound = 0;
                for (int i = assetsSectionStartLine + 1; i < logLines.Length; i++)
                {
                    var line = logLines[i];

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("---"))
                        break;

                    // Parse asset lines (format: " 1.2 mb   0.5% Assets/Textures/hero.png")
                    if (line.Contains("Assets/") && !line.Contains("Built-in"))
                    {
                        var assetInfo = ParseAssetLine(line);
                        if (assetInfo != null)
                        {
                            var category = AssetCategorizer.CategorizeAssetByType(assetInfo.Value.path);
                            var data = categories[category];
                            data.size += assetInfo.Value.size;
                            data.count++;
                            categories[category] = data;

                            allAssets.Add(new TopFile
                            {
                                path     = assetInfo.Value.path,
                                size     = assetInfo.Value.size,
                                category = category
                            });

                            assetsFound++;
                        }
                    }
                }

                if (assetsFound > 0)
                    Debug.Log($"{BuildMetricsConstants.LogPrefix} Successfully collected {assetsFound} assets from Editor.log");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect assets: {ex.Message}");
            }
        }

        private static string GetEditorLogPath()
        {
#if UNITY_EDITOR_OSX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library/Logs/Unity/Editor.log");
#elif UNITY_EDITOR_WIN
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unity/Editor/Editor.log");
#elif UNITY_EDITOR_LINUX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ".config/unity3d/Editor.log");
#else
            return null;
#endif
        }

        private static (string path, long size)? ParseAssetLine(string line)
        {
            try
            {
                // Format: " 1.2 mb   0.5% Assets/Textures/hero.png"
                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                    return null;

                string assetPath = null;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        assetPath = string.Join(" ", parts, i, parts.Length - i);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(assetPath))
                    return null;

                if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float sizeValue))
                    return null;

                var unit = parts[1].Trim().ToLowerInvariant();
                long sizeBytes;

                switch (unit)
                {
                    case "b":
                    case "bytes": sizeBytes = (long)sizeValue; break;
                    case "kb":    sizeBytes = (long)(sizeValue * 1024); break;
                    case "mb":    sizeBytes = (long)(sizeValue * 1024 * 1024); break;
                    case "gb":    sizeBytes = (long)(sizeValue * 1024 * 1024 * 1024); break;
                    default:      return null;
                }

                return (assetPath, sizeBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}

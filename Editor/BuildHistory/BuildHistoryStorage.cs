using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BuildMetrics.Editor
{
    /// <summary>
    /// Persists build history to Library/BuildMetrics/history.json
    /// (project-specific, gitignored by Unity, no EditorPrefs size limit).
    /// </summary>
    public static class BuildHistoryStorage
    {
        // ── File storage ─────────────────────────────────────────────────────
        private const string HistoryFileName    = "history.json";
        private const string HistoryTempSuffix  = ".tmp";   // written first, renamed atomically
        private const string StorageFolder      = "BuildMetrics";

        private static BuildHistoryData cachedHistory;

        private static string GetOrCreateHistoryFilePath()
        {
            // Application.dataPath ends in "/Assets"; Library is one level up
            var libraryDir = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                "Library",
                StorageFolder);
            Directory.CreateDirectory(libraryDir);
            return Path.Combine(libraryDir, HistoryFileName);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public static BuildHistoryData Load()
        {
            if (cachedHistory != null)
                return cachedHistory;

            cachedHistory = LoadFromFile();
            return cachedHistory;
        }

        public static void Save(BuildHistoryData history)
        {
            try
            {
                cachedHistory = history;

                var historyPath = GetOrCreateHistoryFilePath();
                var tmpPath     = historyPath + HistoryTempSuffix;
                var json        = JsonUtility.ToJson(history, prettyPrint: false);

                // Write to a temp file first so a crash mid-write never corrupts the real file.
                File.WriteAllText(tmpPath, json, Encoding.UTF8);

                // File.Replace atomically swaps tmp → history.json (POSIX rename semantics).
                // Use File.Move on first run when no destination file exists yet.
                if (File.Exists(historyPath))
                    File.Replace(tmpPath, historyPath, destinationBackupFileName: null);
                else
                    File.Move(tmpPath, historyPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{BuildMetricsConstants.LogPrefix} Failed to save build history: {ex.Message}");
            }
        }

        public static void AddBuild(BuildRecord record)
        {
            var history = Load();
            history.AddBuild(record);
            Save(history);
        }

        public static void RemoveBuild(string guid)
        {
            var history = Load();
            history.RemoveBuild(guid);
            Save(history);
        }

        public static void ClearCache()
        {
            cachedHistory = null;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private static BuildHistoryData LoadFromFile()
        {
            var path = GetOrCreateHistoryFilePath();

            // ── Attempt to load from file ────────────────────────────────────
            if (File.Exists(path))
            {
                try
                {
                    var json    = File.ReadAllText(path, Encoding.UTF8);
                    var history = JsonUtility.FromJson<BuildHistoryData>(json);
                    if (history?.builds != null)
                    {
                        if (history.profiles == null)
                        {
                            history.profiles = new System.Collections.Generic.List<BaselineBudgetProfile>();
                        }
                        return history;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"{BuildMetricsConstants.LogPrefix} Could not read history file, starting fresh. ({ex.Message})");
                }
            }

            return new BuildHistoryData();
        }
    }
}

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
        private const string HistoryFileName = "history.json";
        private const string StorageFolder   = "BuildMetrics";

        private static BuildHistoryData cachedHistory;

        /// <summary>Absolute path to Library/BuildMetrics/history.json</summary>
        private static string HistoryFilePath
        {
            get
            {
                // Application.dataPath ends in "/Assets"; Library is one level up
                var libraryDir = Path.Combine(
                    Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                    "Library",
                    StorageFolder);
                Directory.CreateDirectory(libraryDir);
                return Path.Combine(libraryDir, HistoryFileName);
            }
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
                var json = JsonUtility.ToJson(history, prettyPrint: false);
                File.WriteAllText(HistoryFilePath, json, Encoding.UTF8);
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
            var path = HistoryFilePath;

            // ── Attempt to load from file ────────────────────────────────────
            if (File.Exists(path))
            {
                try
                {
                    var json    = File.ReadAllText(path, Encoding.UTF8);
                    var history = JsonUtility.FromJson<BuildHistoryData>(json);
                    if (history?.builds != null)
                        return history;
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

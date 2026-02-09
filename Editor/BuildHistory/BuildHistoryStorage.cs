using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildHistoryStorage
    {
        private const string HistoryKeyBase = "BuildMetrics_History_v1";
        private static BuildHistoryData cachedHistory;

        /// <summary>
        /// Get project-specific EditorPrefs key to isolate build history per-project.
        /// Uses hash of project path to create unique key for each Unity project.
        /// </summary>
        private static string GetProjectSpecificKey(string baseKey)
        {
            var projectPath = UnityEngine.Application.dataPath; // Ends with "/Assets"

            // Create short hash of project path (8 characters is enough for uniqueness)
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(projectPath));
                var hashString = BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
                return $"{baseKey}.{hashString}";
            }
        }

        /// <summary>
        /// Project-specific history key. Each Unity project stores its own build history separately.
        /// </summary>
        private static string HistoryKey => GetProjectSpecificKey(HistoryKeyBase);

        public static BuildHistoryData Load()
        {
            if (cachedHistory != null)
            {
                return cachedHistory;
            }

            try
            {
                var json = EditorPrefs.GetString(HistoryKey, string.Empty);
                if (string.IsNullOrEmpty(json))
                {
                    cachedHistory = new BuildHistoryData();
                    return cachedHistory;
                }

                cachedHistory = JsonUtility.FromJson<BuildHistoryData>(json);
                if (cachedHistory == null || cachedHistory.builds == null)
                {
                    cachedHistory = new BuildHistoryData();
                }

                return cachedHistory;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to load build history: {ex.Message}");
                cachedHistory = new BuildHistoryData();
                return cachedHistory;
            }
        }

        public static void Save(BuildHistoryData history)
        {
            try
            {
                cachedHistory = history;
                var json = JsonUtility.ToJson(history, false);
                EditorPrefs.SetString(HistoryKey, json);
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
    }
}

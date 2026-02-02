using System;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildHistoryStorage
    {
        private const string HistoryKey = "BuildMetrics_History_v1";
        private static BuildHistoryData cachedHistory;

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

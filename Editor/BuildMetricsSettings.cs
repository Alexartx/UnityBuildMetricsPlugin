using System;
using UnityEditor;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsSettings
    {
        private const string ApiKeyPref = "BuildMetrics.ApiKey";
        private const string ApiUrlPref = "BuildMetrics.ApiUrl";
        private const string AutoUploadPref = "BuildMetrics.AutoUpload";

        // Environment variable names
        private const string ApiKeyEnvVar = "BUILD_METRICS_API_KEY";
        private const string ApiUrlEnvVar = "BUILD_METRICS_API_URL";

        /// <summary>
        /// API Key with environment variable override support.
        /// Priority: Environment Variable > EditorPrefs
        /// This allows teams to share a key via environment variable while keeping per-developer keys secure.
        /// </summary>
        public static string ApiKey
        {
            get
            {
                // Check environment variable first (for CI/CD and team sharing)
                var envKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
                if (!string.IsNullOrEmpty(envKey))
                {
                    return envKey;
                }

                // Fall back to EditorPrefs (per-developer, secure)
                return EditorPrefs.GetString(ApiKeyPref, string.Empty);
            }
            set => EditorPrefs.SetString(ApiKeyPref, value ?? string.Empty);
        }

        /// <summary>
        /// API URL with environment variable override support.
        /// Priority: Environment Variable > EditorPrefs > Default
        /// </summary>
        public static string ApiUrl => EditorPrefs.GetString(ApiUrlPref, "https://buildmetrics-api.onrender.com/api/builds");

        public static bool AutoUpload
        {
            get => EditorPrefs.GetBool(AutoUploadPref, true);
            set => EditorPrefs.SetBool(AutoUploadPref, value);
        }

        /// <summary>
        /// Check if API key is coming from environment variable
        /// </summary>
        public static bool IsUsingEnvironmentApiKey()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ApiKeyEnvVar));
        }
    }
}

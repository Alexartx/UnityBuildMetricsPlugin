using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsSettings
    {
        private const string ApiKeyPrefBase = "BuildMetrics.ApiKey";
        private const string ApiUrlPref = "BuildMetrics.ApiUrl";
        private const string AutoUploadPrefBase = "BuildMetrics.AutoUpload";

        // Environment variable names
        private const string ApiKeyEnvVar = "BUILD_METRICS_API_KEY";
        private const string ApiUrlEnvVar = "BUILD_METRICS_API_URL";

        /// <summary>
        /// Get project-specific EditorPrefs key to isolate settings per-project.
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
        /// API Key with environment variable override support.
        /// Priority: Environment Variable > EditorPrefs (project-specific)
        /// This allows teams to share a key via environment variable while keeping per-developer keys secure.
        /// Each Unity project stores its own API key separately.
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

                // Fall back to project-specific EditorPrefs (per-project, per-developer, secure)
                var projectKey = GetProjectSpecificKey(ApiKeyPrefBase);
                return EditorPrefs.GetString(projectKey, string.Empty);
            }
            set
            {
                var projectKey = GetProjectSpecificKey(ApiKeyPrefBase);
                EditorPrefs.SetString(projectKey, value ?? string.Empty);
            }
        }

        /// <summary>
        /// API URL with environment variable override support.
        /// Priority: Environment Variable > EditorPrefs > Default
        /// Note: API URL is global (not project-specific) since it's not sensitive data.
        /// </summary>
        public static string ApiUrl => EditorPrefs.GetString(ApiUrlPref, "https://buildmetrics-api.onrender.com/api/builds");

        /// <summary>
        /// Auto-upload setting (project-specific).
        /// Each project can have different auto-upload preferences.
        /// </summary>
        public static bool AutoUpload
        {
            get
            {
                var projectKey = GetProjectSpecificKey(AutoUploadPrefBase);
                return EditorPrefs.GetBool(projectKey, true);
            }
            set
            {
                var projectKey = GetProjectSpecificKey(AutoUploadPrefBase);
                EditorPrefs.SetBool(projectKey, value);
            }
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

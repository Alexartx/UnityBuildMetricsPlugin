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
        /// API Key with multiple sources support.
        /// Priority: Command Line Arguments > Environment Variable > EditorPrefs (project-specific)
        /// This allows flexible configuration across different CI/CD systems:
        /// - Command line args: GameCI and other CI systems that support custom parameters
        /// - Environment variables: GitLab CI, Jenkins, and custom CI setups
        /// - EditorPrefs: Local development with Setup Wizard
        /// Each Unity project stores its own API key separately.
        /// </summary>
        public static string ApiKey
        {
            get
            {
                // Priority 1: Check command line arguments (for GameCI and CI/CD systems)
                var cmdLineKey = GetCommandLineArgValue("-BUILD_METRICS_API_KEY");
                if (!string.IsNullOrEmpty(cmdLineKey))
                {
                    return cmdLineKey;
                }

                // Priority 2: Check environment variable (for GitLab CI, Jenkins, etc.)
                var envKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
                if (!string.IsNullOrEmpty(envKey))
                {
                    return envKey;
                }

                // Priority 3: Fall back to project-specific EditorPrefs (local development)
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

        /// <summary>
        /// Check if API key is coming from command line arguments
        /// </summary>
        public static bool IsUsingCommandLineApiKey()
        {
            return !string.IsNullOrEmpty(GetCommandLineArgValue("-BUILD_METRICS_API_KEY"));
        }

        /// <summary>
        /// Get value of a command line argument by key.
        /// Used to read API key from CI/CD systems like GameCI that pass values via customParameters.
        /// </summary>
        /// <param name="key">The argument key to look for (e.g., "-BUILD_METRICS_API_KEY")</param>
        /// <returns>The value following the key, or null if not found</returns>
        private static string GetCommandLineArgValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == key && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}

using UnityEditor;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsSettings
    {
        private const string ApiKeyPref = "BuildMetrics.ApiKey";
        private const string ApiUrlPref = "BuildMetrics.ApiUrl";
        private const string AutoUploadPref = "BuildMetrics.AutoUpload";

        public static string ApiKey
        {
            get => EditorPrefs.GetString(ApiKeyPref, string.Empty);
            set => EditorPrefs.SetString(ApiKeyPref, value ?? string.Empty);
        }

        public static string ApiUrl
        {
            get => EditorPrefs.GetString(ApiUrlPref, "https://buildmetrics-api.onrender.com/api/builds");
            set => EditorPrefs.SetString(ApiUrlPref, value ?? string.Empty);
        }

        public static bool AutoUpload
        {
            get => EditorPrefs.GetBool(AutoUploadPref, true);
            set => EditorPrefs.SetBool(AutoUploadPref, value);
        }
    }
}

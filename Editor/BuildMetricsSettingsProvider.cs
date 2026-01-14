using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Preferences/Build Metrics", SettingsScope.User)
            {
                label = "Build Metrics",
                guiHandler = _ =>
                {
                    EditorGUILayout.LabelField("Build Metrics Uploader", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    BuildMetricsSettings.ApiUrl = EditorGUILayout.TextField("API URL", BuildMetricsSettings.ApiUrl);
                    BuildMetricsSettings.ApiKey = EditorGUILayout.PasswordField("API Key", BuildMetricsSettings.ApiKey);
                    BuildMetricsSettings.AutoUpload = EditorGUILayout.Toggle("Auto Upload", BuildMetricsSettings.AutoUpload);

                    EditorGUILayout.HelpBox(
                        "Paste your API key once. Successful builds will auto-upload metrics.",
                        MessageType.Info);
                }
            };

            return provider;
        }
    }
}

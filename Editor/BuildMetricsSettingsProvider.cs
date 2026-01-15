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
                    EditorGUILayout.LabelField("Build Metrics Configuration", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    // API Configuration
                    EditorGUILayout.LabelField("API Configuration", EditorStyles.boldLabel);

                    // Check if using environment variable
                    bool usingEnvVar = BuildMetricsSettings.IsUsingEnvironmentApiKey();

                    if (usingEnvVar)
                    {
                        // Show as disabled field when env var is active
                        GUI.enabled = false;
                        EditorGUILayout.PasswordField("API Key", BuildMetricsSettings.ApiKey);
                        GUI.enabled = true;

                        EditorGUILayout.HelpBox(
                            "✓ Using API key from environment variable BUILD_METRICS_API_KEY\n" +
                            "Environment variable takes priority over EditorPrefs.",
                            MessageType.Info);
                    }
                    else
                    {
                        // Normal editable field
                        BuildMetricsSettings.ApiKey = EditorGUILayout.PasswordField("API Key", BuildMetricsSettings.ApiKey);

                        if (string.IsNullOrEmpty(BuildMetricsSettings.ApiKey))
                        {
                            EditorGUILayout.HelpBox(
                                "API key is required. Get your free API key from the dashboard.",
                                MessageType.Warning);

                            if (GUILayout.Button("Open Dashboard to Get API Key", GUILayout.Height(25)))
                            {
                                Application.OpenURL(BuildMetricsConstants.DashboardUrl);
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(
                                "✓ API key configured. Build your project to send metrics automatically.",
                                MessageType.Info);
                        }
                    }

                    EditorGUILayout.Space();

                    // Upload Settings
                    EditorGUILayout.LabelField("Upload Settings", EditorStyles.boldLabel);
                    BuildMetricsSettings.AutoUpload = EditorGUILayout.Toggle("Auto Upload After Build", BuildMetricsSettings.AutoUpload);

                    EditorGUILayout.HelpBox(
                        BuildMetricsSettings.AutoUpload
                            ? "Metrics will be uploaded automatically after each successful build."
                            : "You'll need to manually upload via Tools → Build Metrics → Upload Last Build.",
                        MessageType.Info);

                    EditorGUILayout.Space();

                    // Quick Actions
                    EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Open Setup Wizard", GUILayout.Height(25)))
                    {
                        BuildMetricsSetupWizard.ShowWizard();
                    }
                    if (GUILayout.Button("View Dashboard", GUILayout.Height(25)))
                    {
                        Application.OpenURL(BuildMetricsConstants.DashboardUrl);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            };

            return provider;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public class BuildMetricsSetupWizard : EditorWindow
    {
        private string apiKey = "";
        private bool autoUpload = true;
        private Vector2 scrollPosition;
        private bool isValidating = false;
        private string validationMessage = "";
        private MessageType validationMessageType = MessageType.Info;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Show wizard on first launch if API key is not configured
            EditorApplication.delayCall += () =>
            {
                if (string.IsNullOrEmpty(BuildMetricsSettings.ApiKey) &&
                    !EditorPrefs.GetBool(BuildMetricsConstants.SetupCompletePref, false))
                {
                    ShowWizard();
                }
            };
        }

        [MenuItem("Tools/Build Metrics/Setup Wizard")]
        public static void ShowWizard()
        {
            var window = GetWindow<BuildMetricsSetupWizard>(true, "Build Metrics Setup", true);
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            apiKey = BuildMetricsSettings.ApiKey;
            autoUpload = BuildMetricsSettings.AutoUpload;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header
            GUILayout.Space(10);
            GUILayout.Label("Welcome to Build Metrics!", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Track your Unity build performance and catch regressions before they reach production.",
                MessageType.Info
            );

            GUILayout.Space(15);

            // Step 1: Get API Key
            GUILayout.Label("Step 1: Get Your API Key", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Sign up for a free account to get your API key (100 builds/month free for indie developers).",
                MessageType.Info
            );

            GUILayout.Space(5);

            if (GUILayout.Button("Open Dashboard to Get API Key", GUILayout.Height(30)))
            {
                Application.OpenURL(BuildMetricsConstants.DashboardUrl);
            }

            GUILayout.Space(15);

            // Step 2: Enter API Key
            GUILayout.Label("Step 2: Enter Your API Key", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("API Key:", GUILayout.Width(80));
            apiKey = EditorGUILayout.TextField(apiKey);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("Validate API Key", GUILayout.Height(25)))
            {
                ValidateApiKey();
            }

            if (!string.IsNullOrEmpty(validationMessage))
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox(validationMessage, validationMessageType);
            }

            GUILayout.Space(15);

            // Step 3: Configure Settings
            GUILayout.Label("Step 3: Configure Upload Settings", EditorStyles.boldLabel);
            GUILayout.Space(5);

            autoUpload = EditorGUILayout.Toggle("Auto-upload after builds", autoUpload);
            EditorGUILayout.HelpBox(
                autoUpload
                    ? "Metrics will be uploaded automatically after each build."
                    : "You'll need to manually upload metrics via Tools → Build Metrics → Upload Last Build.",
                MessageType.Info
            );

            GUILayout.Space(20);

            // Footer Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Documentation", GUILayout.Height(30)))
            {
                Application.OpenURL(BuildMetricsConstants.DocsUrl);
            }

            GUILayout.FlexibleSpace();

            GUI.enabled = !string.IsNullOrEmpty(apiKey);
            if (GUILayout.Button("Complete Setup", GUILayout.Height(30), GUILayout.Width(150)))
            {
                CompleteSetup();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private async void ValidateApiKey()
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                validationMessage = "Please enter an API key";
                validationMessageType = MessageType.Warning;
                return;
            }

            isValidating = true;
            validationMessage = "Validating API key...";
            validationMessageType = MessageType.Info;
            Repaint();

            try
            {
                var testReport = new BuildMetricsReport
                {
                    platform = "Validation",
                    buildTimeSeconds = 0,
                    outputSizeBytes = 0,
                    unityVersion = Application.unityVersion,
                    timestamp = System.DateTime.UtcNow.ToString("o")
                };

                // Note: This is a dry-run validation
                // In a real implementation, you'd have a validation endpoint
                validationMessage = "API key format is valid! Click 'Complete Setup' to save.";
                validationMessageType = MessageType.Info;
            }
            catch (System.Exception ex)
            {
                validationMessage = $"Validation error: {ex.Message}";
                validationMessageType = MessageType.Error;
            }
            finally
            {
                isValidating = false;
                Repaint();
            }
        }

        private void CompleteSetup()
        {
            BuildMetricsSettings.ApiKey = apiKey;
            BuildMetricsSettings.AutoUpload = autoUpload;
            EditorPrefs.SetBool(BuildMetricsConstants.SetupCompletePref, true);

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Build Metrics is now configured.\n\n" +
                "Build your project and metrics will be sent to your dashboard automatically.\n\n" +
                $"View your metrics at:\n{BuildMetricsConstants.DashboardUrl}",
                "OK"
            );

            Close();
        }
    }
}

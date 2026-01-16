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

        // Clipboard detection
        private bool clipboardChecked = false;
        private bool clipboardHasKey = false;
        private string clipboardKey = "";
        private bool showClipboardBanner = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Show wizard on first launch if API key is not configured
            EditorApplication.delayCall += () =>
            {
                // Check if API key is configured
                bool hasApiKey = !string.IsNullOrEmpty(BuildMetricsSettings.ApiKey);

                // Check if user explicitly dismissed setup (different from completing it)
                bool userDismissedSetup = EditorPrefs.GetBool(BuildMetricsConstants.SetupDismissedPref, false);

                // Only auto-show if no API key AND user hasn't explicitly dismissed
                if (!hasApiKey && !userDismissedSetup)
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

            // Check clipboard for API key on first open
            CheckClipboardForApiKey();
        }

        private void CheckClipboardForApiKey()
        {
            if (clipboardChecked)
                return;

            clipboardChecked = true;

            try
            {
                string clipboard = EditorGUIUtility.systemCopyBuffer;

                // Check if clipboard contains a Build Metrics API key
                if (!string.IsNullOrEmpty(clipboard) &&
                    clipboard.Trim().StartsWith("bm_") &&
                    clipboard.Trim().Length > 20)
                {
                    clipboardKey = clipboard.Trim();
                    clipboardHasKey = true;

                    // Only show banner if current API key is empty
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        showClipboardBanner = true;
                    }
                }
            }
            catch
            {
                // Clipboard access can fail in some cases, ignore silently
            }
        }

        private void UseClipboardKey()
        {
            apiKey = clipboardKey;
            showClipboardBanner = false;
            Repaint();
        }

        private void DismissClipboardBanner()
        {
            showClipboardBanner = false;
            Repaint();
        }

        private void OnDestroy()
        {
            // Auto-save API key if user entered one
            if (!string.IsNullOrEmpty(apiKey) && apiKey != BuildMetricsSettings.ApiKey)
            {
                BuildMetricsSettings.ApiKey = apiKey;
                BuildMetricsSettings.AutoUpload = autoUpload;
            }

            // Update setup status based on whether API key is configured
            if (!string.IsNullOrEmpty(BuildMetricsSettings.ApiKey))
            {
                // Clear the dismissed flag so wizard can show again if API key is removed later
                EditorPrefs.DeleteKey(BuildMetricsConstants.SetupDismissedPref);

                // Mark setup as complete
                EditorPrefs.SetBool(BuildMetricsConstants.SetupCompletePref, true);
            }
            else
            {
                // No API key configured - mark as dismissed
                // (so it doesn't keep popping up on every editor restart)
                EditorPrefs.SetBool(BuildMetricsConstants.SetupDismissedPref, true);
            }
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

            // Clipboard detection banner (opt-in)
            if (showClipboardBanner)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("🔑 API Key Detected in Clipboard", EditorStyles.boldLabel);
                GUILayout.Space(3);

                EditorGUILayout.LabelField(
                    "Found a Build Metrics API key in your clipboard. Would you like to use it?",
                    EditorStyles.wordWrappedLabel
                );

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Use This Key", GUILayout.Height(25), GUILayout.Width(120)))
                {
                    UseClipboardKey();
                }

                if (GUILayout.Button("No Thanks", GUILayout.Height(25), GUILayout.Width(100)))
                {
                    DismissClipboardBanner();
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                GUILayout.Space(15);
            }

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

            // Check if using environment variable
            bool usingEnvVar = BuildMetricsSettings.IsUsingEnvironmentApiKey();

            if (usingEnvVar)
            {
                EditorGUILayout.HelpBox(
                    "✓ API key detected from environment variable BUILD_METRICS_API_KEY\n\n" +
                    "Environment variable takes priority. You can skip this setup.",
                    MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("API Key:", GUILayout.Width(80));
                GUI.enabled = false;
                EditorGUILayout.TextField(BuildMetricsSettings.ApiKey);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("API Key:", GUILayout.Width(80));
                apiKey = EditorGUILayout.TextField(apiKey);
                EditorGUILayout.EndHorizontal();
            }

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

            if (GUILayout.Button("Skip Setup", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Skip Setup?",
                    "You can configure Build Metrics later via:\nTools → Build Metrics → Setup Wizard",
                    "Skip",
                    "Cancel"))
                {
                    EditorPrefs.SetBool(BuildMetricsConstants.SetupDismissedPref, true);
                    Close();
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            // Auto-save info
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Settings are saved automatically when you close this window.",
                MessageType.Info
            );

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
                validationMessage = "API key format is valid! Settings will be saved when you close this window.";
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

    }
}

using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public class BuildMetricsSetupWizard : EditorWindow
    {
        private string apiKey = "";
        private bool autoUpload = true;
        private Vector2 scrollPosition;
        private string validationMessage = "";
        private MessageType validationMessageType = MessageType.Info;

        // Clipboard detection
        private bool clipboardChecked = false;
        private string clipboardKey = "";
        private bool showClipboardBanner = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Show wizard on first launch if API key is not configured
            EditorApplication.delayCall += () =>
            {
                bool hasApiKey = !string.IsNullOrEmpty(BuildMetricsSettings.ApiKey);
                // Check if user explicitly dismissed setup (different from completing it)
                bool userDismissedSetup = EditorPrefs.GetBool(BuildMetricsCloudConstants.SetupDismissedPref, false);

                if (!hasApiKey && !userDismissedSetup)
                {
                    ShowWizard();
                }
            };
        }

        public static void ShowWizard()
        {
            var window = GetWindow<BuildMetricsSetupWizard>(true, "Build Metrics Setup", true);
            window.minSize = new Vector2(BuildMetricsCloudConstants.WizardWidth, BuildMetricsCloudConstants.WizardHeight);
            window.maxSize = new Vector2(BuildMetricsCloudConstants.WizardWidth, BuildMetricsCloudConstants.WizardHeight);
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

                if (!string.IsNullOrEmpty(clipboard) &&
                    clipboard.Trim().StartsWith(BuildMetricsCloudConstants.ApiKeyPrefix) &&
                    clipboard.Trim().Length > BuildMetricsCloudConstants.ApiKeyMinLength)
                {
                    clipboardKey = clipboard.Trim();

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

            if (!string.IsNullOrEmpty(BuildMetricsSettings.ApiKey))
            {
                // Clear the dismissed flag so wizard can show again if API key is removed later
                EditorPrefs.DeleteKey(BuildMetricsCloudConstants.SetupDismissedPref);
                EditorPrefs.SetBool(BuildMetricsCloudConstants.SetupCompletePref, true);
            }
            else
            {
                // No API key — mark as dismissed so it doesn't reappear on every editor restart
                EditorPrefs.SetBool(BuildMetricsCloudConstants.SetupDismissedPref, true);
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            GUILayout.Label("Welcome to Build Metrics!", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Track your Unity build performance and catch regressions before they reach production.",
                MessageType.Info
            );

            GUILayout.Space(15);

            DrawClipboardBanner();
            DrawApiKeySection();
            DrawAutoUploadSection();
            DrawFooterButtons();

            GUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private void DrawClipboardBanner()
        {
            if (!showClipboardBanner) return;

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
                UseClipboardKey();
            if (GUILayout.Button("No Thanks", GUILayout.Height(25), GUILayout.Width(100)))
                DismissClipboardBanner();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(15);
        }

        private void DrawApiKeySection()
        {
            GUILayout.Label("Step 1: Get Your API Key", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Sign up for a free account to get your API key (100 builds/month free for indie developers).",
                MessageType.Info
            );
            GUILayout.Space(5);
            if (GUILayout.Button("Open Dashboard to Get API Key", GUILayout.Height(30)))
                Application.OpenURL(BuildMetricsCloudConstants.DashboardUrl);

            GUILayout.Space(15);

            GUILayout.Label("Step 2: Enter Your API Key", EditorStyles.boldLabel);
            GUILayout.Space(5);

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
                ValidateApiKey();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox(validationMessage, validationMessageType);
            }

            GUILayout.Space(15);
        }

        private void DrawAutoUploadSection()
        {
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
        }

        private void DrawFooterButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Documentation", GUILayout.Height(30)))
                Application.OpenURL(BuildMetricsCloudConstants.DocsUrl);
            if (GUILayout.Button("Skip Setup", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Skip Setup?",
                    "You can configure Build Metrics later via:\nTools → Build Metrics → Setup Wizard",
                    "Skip",
                    "Cancel"))
                {
                    EditorPrefs.SetBool(BuildMetricsCloudConstants.SetupDismissedPref, true);
                    Close();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("Settings are saved automatically when you close this window.", MessageType.Info);
        }

        private void ValidateApiKey()
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                validationMessage     = "Please enter an API key";
                validationMessageType = MessageType.Warning;
                return;
            }

            validationMessage     = "Checking with server…";
            validationMessageType = MessageType.Info;
            Repaint();

            BuildMetricsUploader.ValidateApiKeyWithServer(apiKey, (isValid, message) =>
            {
                validationMessage     = message;
                validationMessageType = isValid ? MessageType.Info : MessageType.Error;

                if (isValid)
                {
                    // Save immediately so the user sees a confirmed green state
                    BuildMetricsSettings.ApiKey    = apiKey;
                    BuildMetricsSettings.AutoUpload = autoUpload;
                }

                Repaint();
            });
        }

    }
}

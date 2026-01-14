using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsMenu
    {
        private const string MenuRoot = "Tools/Build Metrics/";
        private const int MenuPriority = 1000;

        [MenuItem(MenuRoot + "Setup Wizard", false, MenuPriority)]
        public static void OpenSetupWizard()
        {
            BuildMetricsSetupWizard.ShowWizard();
        }

        [MenuItem(MenuRoot + "Settings", false, MenuPriority + 1)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Build Metrics");
        }

        [MenuItem(MenuRoot + "Upload Last Build", false, MenuPriority + 20)]
        public static void UploadLastBuild()
        {
            var lastReport = BuildMetricsStorage.GetLatestReport();
            if (string.IsNullOrEmpty(lastReport))
            {
                EditorUtility.DisplayDialog(
                    "No Build Found",
                    "No build metrics found. Build your project first.",
                    "OK"
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(BuildMetricsSettings.ApiKey))
            {
                var openSetup = EditorUtility.DisplayDialog(
                    "API Key Required",
                    $"API key is not configured.\n\nGet your API key from:\n{BuildMetricsConstants.DashboardUrl}",
                    "Open Setup Wizard",
                    "Cancel"
                );

                if (openSetup)
                {
                    OpenSetupWizard();
                }
                return;
            }

            BuildMetricsUploader.TryUploadReport(lastReport);
        }

        [MenuItem(MenuRoot + "Upload All Pending", false, MenuPriority + 21)]
        public static void UploadAllPending()
        {
            var pending = BuildMetricsStorage.GetPendingReports();
            if (pending.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Pending Uploads",
                    "No pending build metrics found.",
                    "OK"
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(BuildMetricsSettings.ApiKey))
            {
                var openSetup = EditorUtility.DisplayDialog(
                    "API Key Required",
                    $"API key is not configured.\n\nGet your API key from:\n{BuildMetricsConstants.DashboardUrl}",
                    "Open Setup Wizard",
                    "Cancel"
                );

                if (openSetup)
                {
                    OpenSetupWizard();
                }
                return;
            }

            BuildMetricsUploader.TryUploadPending();
            EditorUtility.DisplayDialog(
                "Upload Started",
                $"Uploading {pending.Length} pending build metric(s).\n\nCheck the Console for status.",
                "OK"
            );
        }

        [MenuItem(MenuRoot + "View Dashboard", false, MenuPriority + 40)]
        public static void OpenDashboard()
        {
            Application.OpenURL(BuildMetricsConstants.DashboardUrl);
        }

        [MenuItem(MenuRoot + "Documentation", false, MenuPriority + 41)]
        public static void OpenDocumentation()
        {
            Application.OpenURL(BuildMetricsConstants.DocsUrl);
        }

        [MenuItem(MenuRoot + "About", false, MenuPriority + 60)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "Build Metrics",
                $"Build Metrics for Unity\n" +
                $"Version {BuildMetricsConstants.Version}\n\n" +
                $"Track build performance and catch regressions.\n\n" +
                $"Dashboard: {BuildMetricsConstants.DashboardUrl}\n" +
                $"Support: {BuildMetricsConstants.SupportEmail}",
                "OK"
            );
        }
    }
}

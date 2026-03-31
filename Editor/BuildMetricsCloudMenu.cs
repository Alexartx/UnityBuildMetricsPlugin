using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    internal static class BuildMetricsCloudMenu
    {
        private const string MenuRoot = "Tools/Build Metrics/";
        private const int MenuPriority = 1010;

        [MenuItem(MenuRoot + "Setup Wizard", false, MenuPriority + 10)]
        public static void OpenSetupWizard()
        {
            BuildMetricsSetupWizard.ShowWizard();
        }

        [MenuItem(MenuRoot + "Settings", false, MenuPriority + 11)]
        public static void OpenSettings()
        {
            SettingsService.OpenUserPreferences("Preferences/Build Metrics");
        }

        [MenuItem(MenuRoot + "Upload Last Build", false, MenuPriority + 12)]
        public static void UploadLastBuild()
        {
            var latestReport = BuildMetricsStorage.GetLatestReport();
            if (string.IsNullOrWhiteSpace(latestReport))
            {
                EditorUtility.DisplayDialog(
                    "No Build Report Found",
                    "Build a project first so Build Metrics can capture a report to upload.",
                    "OK");
                return;
            }

            BuildMetricsUploader.TryUploadReport(latestReport);
        }

        [MenuItem(MenuRoot + "Upload All Pending", false, MenuPriority + 13)]
        public static void UploadAllPending()
        {
            var pendingReports = BuildMetricsCloudStorage.GetPendingReports();
            if (pendingReports.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Pending Reports",
                    "There are no queued reports waiting to be uploaded.",
                    "OK");
                return;
            }

            BuildMetricsUploader.TryUploadPending();
        }

        [MenuItem(MenuRoot + "View Dashboard", false, MenuPriority + 14)]
        public static void OpenDashboard()
        {
            Application.OpenURL(BuildMetricsCloudConstants.DashboardUrl);
        }
    }
}

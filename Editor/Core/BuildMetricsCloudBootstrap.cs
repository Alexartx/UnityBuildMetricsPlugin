using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    [InitializeOnLoad]
    internal static class BuildMetricsCloudBootstrap
    {
        private static readonly BuildMetricsCloudTopPanel TopPanel = new BuildMetricsCloudTopPanel();
        private static bool missingSetupWarningShown;

        static BuildMetricsCloudBootstrap()
        {
            BuildMetricsExtensions.RegisterTopPanelExtension(TopPanel);
            BuildMetricsExtensions.ReportCaptured -= OnReportCaptured;
            BuildMetricsExtensions.ReportCaptured += OnReportCaptured;

            BuildMetricsStatus.OnStatusChanged -= RepaintHistoryWindows;
            BuildMetricsStatus.OnStatusChanged += RepaintHistoryWindows;

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            BuildMetricsExtensions.ReportCaptured -= OnReportCaptured;
            BuildMetricsStatus.OnStatusChanged -= RepaintHistoryWindows;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        private static void OnReportCaptured(BuildMetricsCapturedReport capturedReport)
        {
            if (capturedReport == null || string.IsNullOrWhiteSpace(capturedReport.ReportPath))
            {
                return;
            }

            if (!BuildMetricsSettings.AutoUpload)
            {
                return;
            }

            if (!BuildMetricsUploader.ValidateApiKeyFormat(BuildMetricsSettings.ApiKey, out var validationError))
            {
                BuildMetricsCloudStorage.Enqueue(capturedReport.ReportPath);
                BuildMetricsStatus.SetFailed(validationError);

                if (!missingSetupWarningShown)
                {
                    Debug.LogWarning(
                        $"{BuildMetricsConstants.LogPrefix} Cloud upload is enabled, but setup is incomplete. " +
                        "Open Tools/Build Metrics/Setup Wizard to configure an API key.");
                    missingSetupWarningShown = true;
                }

                return;
            }

            missingSetupWarningShown = false;
            BuildMetricsUploader.TryUploadPending();
            BuildMetricsUploader.TryUploadReport(capturedReport.ReportPath);
        }

        private static void RepaintHistoryWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BuildMetricsWindow>())
            {
                if (window != null)
                {
                    window.Repaint();
                }
            }
        }
    }
}

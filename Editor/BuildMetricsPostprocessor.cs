using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public class BuildMetricsPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Build report was null.");
                return;
            }

            var summary = report.summary;
            if (summary.result == BuildResult.Failed || summary.result == BuildResult.Cancelled)
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Build result was {summary.result}. Skipping export.");
                return;
            }

            try
            {
                var reportData = BuildReportFactory.Create(report);
                var reportPath = BuildMetricsStorage.WriteReport(reportData);
                Debug.Log($"{BuildMetricsConstants.LogPrefix} Build completed! Platform: {summary.platform}, " +
                    $"Time: {summary.totalTime.TotalSeconds:F1}s, Size: {reportData.outputSizeBytes / 1024 / 1024:F2} MB");

                var buildRecord = BuildRecord.FromMetricsReport(reportData);
                BuildHistoryStorage.AddBuild(buildRecord);

                BuildMetricsUploader.TryUploadPending();

                if (BuildMetricsSettings.AutoUpload && !string.IsNullOrWhiteSpace(BuildMetricsSettings.ApiKey))
                {
                    BuildMetricsUploader.TryUploadReport(reportPath);
                }
                else if (string.IsNullOrWhiteSpace(BuildMetricsSettings.ApiKey))
                {
                    Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Build metrics saved locally but not uploaded. " +
                        $"Configure your API key: Tools → Build Metrics → Setup Wizard");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{BuildMetricsConstants.LogPrefix} Failed to export build metrics: {ex.Message}");
            }
        }
    }
}

using System;
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
                Debug.Log($"{BuildMetricsConstants.LogPrefix} Build metrics saved locally at: {reportPath}");

                BuildMetricsExtensions.NotifyReportCaptured(new BuildMetricsCapturedReport(reportPath, reportData, buildRecord));
            }
            catch (Exception ex)
            {
                Debug.LogError($"{BuildMetricsConstants.LogPrefix} Failed to export build metrics: {ex.Message}");
            }
        }
    }
}

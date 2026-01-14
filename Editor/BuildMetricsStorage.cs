using System.IO;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsStorage
    {
        private const string ReportFileName = "build_metrics.json";

        public static string ReportsDirectory => Path.Combine(ProjectRoot, "BuildReports");
        public static string PendingDirectory => Path.Combine(ReportsDirectory, "pending");
        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string WriteReport(BuildMetricsReport report)
        {
            Directory.CreateDirectory(ReportsDirectory);
            var path = Path.Combine(ReportsDirectory, ReportFileName);
            var json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json);
            return path;
        }

        public static string Enqueue(string reportPath)
        {
            Directory.CreateDirectory(PendingDirectory);
            var fileName = Path.GetFileName(reportPath);
            var queuedPath = Path.Combine(PendingDirectory, fileName);
            File.Copy(reportPath, queuedPath, true);
            return queuedPath;
        }

        public static string[] GetPendingReports()
        {
            if (!Directory.Exists(PendingDirectory))
            {
                return new string[0];
            }

            return Directory.GetFiles(PendingDirectory, "*.json");
        }

        public static string GetLatestReport()
        {
            var path = Path.Combine(ReportsDirectory, ReportFileName);
            return File.Exists(path) ? path : null;
        }
    }
}

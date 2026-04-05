using System;
using System.IO;

namespace BuildMetrics.Editor
{
    internal static class BuildMetricsCloudStorage
    {
        public static string PendingDirectory => Path.Combine(BuildMetricsStorage.ReportsDirectory, "pending");

        public static string Enqueue(string reportPath)
        {
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
            {
                return null;
            }

            Directory.CreateDirectory(PendingDirectory);

            var fileName = Path.GetFileName(reportPath);
            var queuedPath = Path.Combine(PendingDirectory, fileName);
            if (string.Equals(Path.GetFullPath(reportPath), Path.GetFullPath(queuedPath), StringComparison.OrdinalIgnoreCase))
            {
                return queuedPath;
            }

            File.Copy(reportPath, queuedPath, true);
            return queuedPath;
        }

        public static string[] GetPendingReports()
        {
            if (!Directory.Exists(PendingDirectory))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(PendingDirectory, "*.json");
        }
    }
}

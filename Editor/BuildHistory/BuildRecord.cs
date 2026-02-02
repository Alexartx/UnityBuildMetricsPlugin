using System;

namespace BuildMetrics.Editor
{
    [Serializable]
    public class BuildRecord
    {
        public string guid;
        public long timestampUnix;
        public string platform;
        public long sizeBytes;
        public int timeSeconds;
        public string unityVersion;
        public string buildName;
        public string buildNumber;
        public string artifactType;
        public bool developmentBuild;
        public string scriptingBackend;
        public GitInfo git;
        public FileBreakdown fileBreakdown;
        public AssetBreakdown assetBreakdown;

        public DateTime Timestamp => DateTimeOffset.FromUnixTimeSeconds(timestampUnix).LocalDateTime;

        public string TimeAgo
        {
            get
            {
                var elapsed = DateTime.Now - Timestamp;
                if (elapsed.TotalMinutes < 1) return "just now";
                if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
                if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
                if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
                return Timestamp.ToString("MMM dd");
            }
        }

        public static BuildRecord FromMetricsReport(BuildMetricsReport report)
        {
            return new BuildRecord
            {
                guid = report.buildGuid,
                timestampUnix = DateTimeOffset.Parse(report.timestamp).ToUnixTimeSeconds(),
                platform = report.platform,
                sizeBytes = report.outputSizeBytes,
                timeSeconds = report.buildTimeSeconds,
                unityVersion = report.unityVersion,
                buildName = report.buildName,
                buildNumber = report.buildNumber,
                artifactType = report.artifactType,
                developmentBuild = report.developmentBuild,
                scriptingBackend = report.scriptingBackend,
                git = report.git,
                fileBreakdown = report.fileBreakdown,
                assetBreakdown = report.assetBreakdown
            };
        }
    }
}

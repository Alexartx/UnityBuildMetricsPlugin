using System;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsFormatters
    {
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        public static string FormatTime(int seconds)
        {
            if (seconds < 60) return $"{seconds}s";
            var minutes = seconds / 60;
            var remainingSeconds = seconds % 60;
            return $"{minutes}m {remainingSeconds}s";
        }

        public static string FormatDelta(long delta, bool isSize)
        {
            var prefix = delta > 0 ? "+" : "";
            return isSize ? $"{prefix}{FormatBytes(delta)}" : $"{prefix}{delta}s";
        }

        public static string FormatPercentage(float percentage)
        {
            var prefix = percentage > 0 ? "+" : "";
            return $"{prefix}{percentage:F1}%";
        }
    }
}

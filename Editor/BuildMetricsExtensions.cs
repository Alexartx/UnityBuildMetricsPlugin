using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public sealed class BuildMetricsCapturedReport
    {
        public BuildMetricsCapturedReport(string reportPath, BuildMetricsReport report, BuildRecord buildRecord)
        {
            ReportPath = reportPath;
            Report = report;
            BuildRecord = buildRecord;
        }

        public string ReportPath { get; }
        public BuildMetricsReport Report { get; }
        public BuildRecord BuildRecord { get; }
    }

    public interface IBuildMetricsTopPanelExtension
    {
        int Order { get; }
        void DrawTopPanel(BuildMetricsWindow window);
    }

    public static class BuildMetricsExtensions
    {
        public static event Action<BuildMetricsCapturedReport> ReportCaptured;

        private static readonly List<IBuildMetricsTopPanelExtension> TopPanelExtensions = new List<IBuildMetricsTopPanelExtension>();

        public static void RegisterTopPanelExtension(IBuildMetricsTopPanelExtension extension)
        {
            if (extension == null)
            {
                return;
            }

            if (TopPanelExtensions.Contains(extension))
            {
                return;
            }

            TopPanelExtensions.Add(extension);
            TopPanelExtensions.Sort((left, right) => left.Order.CompareTo(right.Order));
        }

        public static void UnregisterTopPanelExtension(IBuildMetricsTopPanelExtension extension)
        {
            if (extension == null)
            {
                return;
            }

            TopPanelExtensions.Remove(extension);
        }

        public static void DrawTopPanels(BuildMetricsWindow window)
        {
            foreach (var extension in TopPanelExtensions.ToArray())
            {
                try
                {
                    extension.DrawTopPanel(window);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Top-panel extension failed: {ex.Message}");
                }
            }
        }

        public static void NotifyReportCaptured(BuildMetricsCapturedReport capturedReport)
        {
            if (capturedReport == null)
            {
                return;
            }

            var handlers = ReportCaptured;
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in handlers.GetInvocationList().Cast<Action<BuildMetricsCapturedReport>>())
            {
                try
                {
                    handler.Invoke(capturedReport);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} ReportCaptured handler failed: {ex.Message}");
                }
            }
        }
    }
}

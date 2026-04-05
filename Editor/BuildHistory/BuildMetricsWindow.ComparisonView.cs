using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public partial class BuildMetricsWindow
    {
        private void DrawComparisonView()
        {
            if (comparisonBuildA == null || comparisonBuildB == null)
            {
                currentView = ViewMode.List;
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header with dates and commits
            var dateA = comparisonBuildA.Timestamp.ToString("yyyy-MM-dd HH:mm");
            var dateB = comparisonBuildB.Timestamp.ToString("yyyy-MM-dd HH:mm");
            var commitA = (comparisonBuildA.git != null && !string.IsNullOrEmpty(comparisonBuildA.git.commitSha))
                ? comparisonBuildA.git.commitSha.Substring(0, 8) : "-";
            var commitB = (comparisonBuildB.git != null && !string.IsNullOrEmpty(comparisonBuildB.git.commitSha))
                ? comparisonBuildB.git.commitSha.Substring(0, 8) : "-";

            EditorGUILayout.LabelField(
                $"Build A: {dateA} ({commitA})  vs  Build B: {dateB} ({commitB})",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            DrawComparisonTable();

            EditorGUILayout.Space(10);

            DrawDiffSummary();

            EditorGUILayout.Space(10);

            DrawFileBreakdownComparison();

            EditorGUILayout.EndScrollView();
        }

        private void DrawComparisonTable()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var headerStyle = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("", headerStyle, GUILayout.Width(100));
            GUILayout.Label("Build A", headerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label("Build B", headerStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            DrawComparisonRow("Date",
                comparisonBuildA.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                comparisonBuildB.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

            var commitA = (comparisonBuildA.git != null && !string.IsNullOrEmpty(comparisonBuildA.git.commitSha))
                ? comparisonBuildA.git.commitSha.Substring(0, 8) : "-";
            var commitB = (comparisonBuildB.git != null && !string.IsNullOrEmpty(comparisonBuildB.git.commitSha))
                ? comparisonBuildB.git.commitSha.Substring(0, 8) : "-";
            DrawComparisonRow("Commit", commitA, commitB);

            DrawComparisonRow("Platform", comparisonBuildA.platform, comparisonBuildB.platform);

            DrawComparisonRow("Size",
                BuildMetricsFormatters.FormatBytes(comparisonBuildA.sizeBytes),
                BuildMetricsFormatters.FormatBytes(comparisonBuildB.sizeBytes));

            DrawComparisonRow("Time",
                BuildMetricsFormatters.FormatTime(comparisonBuildA.timeSeconds),
                BuildMetricsFormatters.FormatTime(comparisonBuildB.timeSeconds));

            EditorGUILayout.EndVertical();
        }

        private void DrawComparisonRow(string label, string valueA, string valueB)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

            var labelStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField(label, labelStyle, GUILayout.Width(100));

            EditorGUILayout.LabelField(valueA, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(valueB, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiffSummary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Changes Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var sizeDelta = comparisonBuildB.sizeBytes - comparisonBuildA.sizeBytes;
            var sizePercentage = comparisonBuildA.sizeBytes > 0
                ? (sizeDelta / (float)comparisonBuildA.sizeBytes) * 100f : 0f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size:", EditorStyles.boldLabel, GUILayout.Width(80));

            var sizeIcon = sizeDelta < 0 ? "✓" : (sizeDelta > 0 ? "⚠" : "=");
            var sizeColor = sizeDelta < 0 ? SuccessColor : (sizeDelta > 0 ? WarningColor : new Color(0.7f, 0.7f, 0.7f));
            var sizeLabel = sizeDelta < 0
                ? $"{sizeIcon} Smaller by {BuildMetricsFormatters.FormatBytes(-sizeDelta)} ({Math.Abs(sizePercentage):F1}% reduction)"
                : sizeDelta > 0
                    ? $"{sizeIcon} Larger by {BuildMetricsFormatters.FormatBytes(sizeDelta)} ({sizePercentage:F1}% increase)"
                    : $"{sizeIcon} No change";

            var sizeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = sizeColor } };
            EditorGUILayout.LabelField(sizeLabel, sizeStyle);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            var timeDelta = comparisonBuildB.timeSeconds - comparisonBuildA.timeSeconds;
            var timePercentage = comparisonBuildA.timeSeconds > 0
                ? (timeDelta / (float)comparisonBuildA.timeSeconds) * 100f : 0f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time:", EditorStyles.boldLabel, GUILayout.Width(80));

            var timeIcon = timeDelta < 0 ? "✓" : (timeDelta > 0 ? "⚠" : "=");
            var timeColor = timeDelta < 0 ? SuccessColor : (timeDelta > 0 ? WarningColor : new Color(0.7f, 0.7f, 0.7f));
            var timeLabel = timeDelta < 0
                ? $"{timeIcon} Faster by {BuildMetricsFormatters.FormatTime(-timeDelta)} ({Math.Abs(timePercentage):F1}% reduction)"
                : timeDelta > 0
                    ? $"{timeIcon} Slower by {BuildMetricsFormatters.FormatTime(timeDelta)} ({timePercentage:F1}% increase)"
                    : $"{timeIcon} No change";

            var timeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = timeColor } };
            EditorGUILayout.LabelField(timeLabel, timeStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawFileBreakdownComparison()
        {
            if (comparisonBuildA.fileBreakdown == null || comparisonBuildB.fileBreakdown == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("File Breakdown Comparison", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var categoriesA = GetFileCategorySizes(comparisonBuildA.fileBreakdown);
            var categoriesB = GetFileCategorySizes(comparisonBuildB.fileBreakdown);

            var trackedTotalA = categoriesA.Values.Sum();
            var trackedTotalB = categoriesB.Values.Sum();

            var actualTotalA = comparisonBuildA.sizeBytes;
            var actualTotalB = comparisonBuildB.sizeBytes;
            var untrackedA = actualTotalA - trackedTotalA;
            var untrackedB = actualTotalB - trackedTotalB;

            var untrackedPercentA = actualTotalA > 0 ? (untrackedA / (float)actualTotalA) * 100f : 0f;
            var untrackedPercentB = actualTotalB > 0 ? (untrackedB / (float)actualTotalB) * 100f : 0f;
            var maxUntrackedPercent = Math.Max(untrackedPercentA, untrackedPercentB);

            if (maxUntrackedPercent > 20f)
            {
                var warningStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = WarningColor },
                    padding = new RectOffset(10, 10, 5, 5)
                };
                EditorGUILayout.BeginHorizontal(warningStyle);
                EditorGUILayout.LabelField($"⚠ Warning: {maxUntrackedPercent:F1}% of build size is not categorized in file breakdown",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = WarningColor }, wordWrap = true });
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }

            // Header row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var headerStyle = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Category", headerStyle, GUILayout.Width(120));
            GUILayout.Label("Build A", headerStyle, GUILayout.Width(100));
            GUILayout.Label("Build B", headerStyle, GUILayout.Width(100));
            GUILayout.Label("Change", headerStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            var allCategories = categoriesA.Keys.Union(categoriesB.Keys).OrderBy(k => k).ToList();

            foreach (var categoryName in allCategories)
            {
                var sizeA = categoriesA.ContainsKey(categoryName) ? categoriesA[categoryName] : 0;
                var sizeB = categoriesB.ContainsKey(categoryName) ? categoriesB[categoryName] : 0;

                if (sizeA == 0 && sizeB == 0) continue;

                var delta = sizeB - sizeA;
                var percentage = sizeA > 0 ? ((delta / (float)sizeA) * 100f) : (sizeB > 0 ? 100f : 0f);
                var isSignificant = Math.Abs(percentage) > 5f;

                EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

                var categoryStyle = isSignificant
                    ? new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }
                    : EditorStyles.label;
                var displayName = categoryName == "Plugins"
                    ? "Plugins/Unity core"
                    : categoryName == "Scene Data" ? "Scene Data" : categoryName;
                EditorGUILayout.LabelField(displayName, categoryStyle, GUILayout.Width(120));

                var sizeAStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(sizeA > 0 ? BuildMetricsFormatters.FormatBytes(sizeA) : "-", sizeAStyle, GUILayout.Width(100));

                var sizeBStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(sizeB > 0 ? BuildMetricsFormatters.FormatBytes(sizeB) : "-", sizeBStyle, GUILayout.Width(100));

                if (delta == 0)
                {
                    var noChangeStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    EditorGUILayout.LabelField("= No change", noChangeStyle);
                }
                else
                {
                    var icon = delta < 0 ? "✓" : "⚠";
                    var color = delta < 0 ? SuccessColor : WarningColor;
                    var changeLabel = delta < 0
                        ? $"{icon} {BuildMetricsFormatters.FormatBytes(-delta)} smaller ({Math.Abs(percentage):F1}%)"
                        : $"{icon} {BuildMetricsFormatters.FormatBytes(delta)} larger ({percentage:F1}%)";

                    var changeStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = color },
                        fontStyle = isSignificant ? FontStyle.Bold : FontStyle.Normal
                    };
                    EditorGUILayout.LabelField(changeLabel, changeStyle);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Separator line
            GUILayout.Space(5);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), new Color(0.3f, 0.3f, 0.3f));
            GUILayout.Space(5);

            // Untracked row (if significant)
            if (untrackedA > 0 || untrackedB > 0)
            {
                var untrackedDelta = untrackedB - untrackedA;

                EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

                var untrackedStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.8f, 0.6f, 0.2f) }
                };
                EditorGUILayout.LabelField("Untracked", untrackedStyle, GUILayout.Width(120));

                var sizeAStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(untrackedA > 0 ? BuildMetricsFormatters.FormatBytes(untrackedA) : "-", sizeAStyle, GUILayout.Width(100));

                var sizeBStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(untrackedB > 0 ? BuildMetricsFormatters.FormatBytes(untrackedB) : "-", sizeBStyle, GUILayout.Width(100));

                if (untrackedDelta == 0)
                {
                    var noChangeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
                    EditorGUILayout.LabelField("= No change", noChangeStyle);
                }
                else
                {
                    var changeLabel = untrackedDelta < 0
                        ? $"✓ {BuildMetricsFormatters.FormatBytes(-untrackedDelta)} smaller"
                        : $"⚠ {BuildMetricsFormatters.FormatBytes(untrackedDelta)} larger";
                    EditorGUILayout.LabelField(changeLabel, untrackedStyle);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Total row
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal(GUILayout.Height(24));

            var totalStyle = new GUIStyle(EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Total (Actual)", totalStyle, GUILayout.Width(120));

            var totalAStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatBytes(actualTotalA), totalAStyle, GUILayout.Width(100));

            var totalBStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatBytes(actualTotalB), totalBStyle, GUILayout.Width(100));

            var totalDelta = actualTotalB - actualTotalA;
            var totalPercentage = actualTotalA > 0 ? ((totalDelta / (float)actualTotalA) * 100f) : 0f;
            var totalColor = totalDelta < 0 ? SuccessColor : (totalDelta > 0 ? WarningColor : new Color(0.7f, 0.7f, 0.7f));
            var totalChangeLabel = totalDelta == 0 ? "= No change" :
                totalDelta < 0 ? $"✓ {BuildMetricsFormatters.FormatBytes(-totalDelta)} smaller ({Math.Abs(totalPercentage):F1}%)" :
                $"⚠ {BuildMetricsFormatters.FormatBytes(totalDelta)} larger ({totalPercentage:F1}%)";

            var totalChangeStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = totalColor } };
            EditorGUILayout.LabelField(totalChangeLabel, totalChangeStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private List<(string name, long size, Color color)> GetFileCategories(FileBreakdown breakdown)
        {
            return new List<(string name, long size, Color color)>
            {
                ("Scripts", breakdown.scripts?.size ?? 0, new Color(0.3f, 0.6f, 0.9f)),
                ("Resources", breakdown.resources?.size ?? 0, new Color(0.3f, 0.85f, 0.5f)),
                ("Streaming Assets", breakdown.streamingAssets?.size ?? 0, new Color(0.6f, 0.4f, 0.8f)),
                ("Plugins/Unity core", breakdown.plugins?.size ?? 0, new Color(1f, 0.6f, 0.3f)),
                ("Scene Data", breakdown.scenes?.size ?? 0, new Color(1f, 0.4f, 0.6f)),
                ("Shaders", breakdown.shaders?.size ?? 0, new Color(1f, 0.9f, 0.3f)),
                ("Other", breakdown.other?.size ?? 0, new Color(0.5f, 0.5f, 0.5f))
            }.Where(c => c.size > 0).ToList();
        }

        private Dictionary<string, long> GetFileCategorySizes(FileBreakdown breakdown)
        {
            return new Dictionary<string, long>
            {
                ["Scripts"] = breakdown.scripts?.size ?? 0,
                ["Resources"] = breakdown.resources?.size ?? 0,
                ["Streaming Assets"] = breakdown.streamingAssets?.size ?? 0,
                ["Plugins"] = breakdown.plugins?.size ?? 0,
                ["Scene Data"] = breakdown.scenes?.size ?? 0,
                ["Shaders"] = breakdown.shaders?.size ?? 0,
                ["Other"] = breakdown.other?.size ?? 0
            };
        }
    }
}

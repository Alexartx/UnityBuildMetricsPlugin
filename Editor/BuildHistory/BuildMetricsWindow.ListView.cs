using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public partial class BuildMetricsWindow
    {
        private void DrawListView()
        {
            var history = BuildHistoryStorage.Load();

            if (history.builds.Count == 0)
            {
                DrawEmptyState();
                return;
            }

            if (showFilters)
            {
                DrawFilters(history);
            }

            // Draw trend chart at top
            DrawTopTrendChart(history);
            EditorGUILayout.Space(10);

            DrawBuildsHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var filteredBuilds = string.IsNullOrEmpty(searchQuery)
                ? history.GetSortedBuilds(sortField, sortAscending)
                : history.GetSortedBuilds(sortField, sortAscending)
                    .Where(b => b.platform.ToLower().Contains(searchQuery.ToLower()));

            if (!string.IsNullOrEmpty(platformFilter) && platformFilter != "All")
            {
                filteredBuilds = filteredBuilds.Where(b => b.platform == platformFilter);
            }
            if (!string.IsNullOrEmpty(artifactFilter) && artifactFilter != "All")
            {
                filteredBuilds = filteredBuilds.Where(b => b.artifactType == artifactFilter);
            }

            foreach (var build in filteredBuilds)
            {
                DrawBuildListItem(build, history);
            }

            EditorGUILayout.EndScrollView();

            DrawListViewFooter(history);
        }

        private void DrawFilters(BuildHistoryData history)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            searchQuery = EditorGUILayout.TextField(searchQuery);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform:", GUILayout.Width(60));
            var platforms = history.GetUniquePlatforms().ToArray();
            var currentIndex = System.Array.IndexOf(platforms, platformFilter);
            var newIndex = EditorGUILayout.Popup(currentIndex >= 0 ? currentIndex : 0, platforms);
            platformFilter = platforms[newIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Artifact:", GUILayout.Width(60));
            var artifactOptions = new List<string> { "All" };
            var resolvedPlatform = platformFilter == "All" ? null : platformFilter;
            artifactOptions.AddRange(history.builds
                .Where(b => string.IsNullOrEmpty(resolvedPlatform) || b.platform == resolvedPlatform)
                .Select(b => b.artifactType)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .OrderBy(a => a));
            if (!artifactOptions.Contains(artifactFilter))
            {
                artifactFilter = "All";
            }
            var artifactIndex = Math.Max(0, artifactOptions.IndexOf(artifactFilter));
            var newArtifactIndex = EditorGUILayout.Popup(artifactIndex, artifactOptions.ToArray());
            artifactFilter = artifactOptions[newArtifactIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawBuildsHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var selectStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Select", selectStyle, GUILayout.Width(50));

            if (DrawSortableHeader("Date", 160, SortField.Date, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Date);
            }

            var commitStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Commit", commitStyle, GUILayout.Width(100));

            if (DrawSortableHeader("Platform", 100, SortField.Platform, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Platform);
            }

            GUILayout.Label("Artifact", EditorStyles.toolbarButton, GUILayout.Width(90));

            if (DrawSortableHeader("Size", 100, SortField.Size, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Size);
            }

            if (DrawSortableHeader("Time", 80, SortField.Time, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Time);
            }

            GUILayout.FlexibleSpace();

            var actionsStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Actions", actionsStyle, GUILayout.Width(310));

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawSortableHeader(string label, float width, SortField field, TextAnchor alignment, bool sortable = true)
        {
            var suffix = sortField == field ? (sortAscending ? " ▲" : " ▼") : "";
            var style = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = alignment
            };
            var clicked = GUILayout.Button(label + suffix, style, GUILayout.Width(width));
            return sortable && clicked;
        }

        private void ToggleSort(SortField field)
        {
            if (sortField == field)
            {
                sortAscending = !sortAscending;
            }
            else
            {
                sortField = field;
                sortAscending = false;
            }
        }

        private void DrawBuildListItem(BuildRecord build, BuildHistoryData history)
        {
            var isSelected = comparisonBuildA == build || comparisonBuildB == build;
            var isBaseline = history.GetBaseline(build)?.guid == build.guid;

            if (isSelected)
            {
                var highlightStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.7f, 0.3f)) }
                };
                EditorGUILayout.BeginHorizontal(highlightStyle, GUILayout.Height(28));
            }
            else
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(28));
            }

            // Select checkbox
            var checkboxRect = GUILayoutUtility.GetRect(50, 28);
            var checkboxCenter = new Rect(checkboxRect.x + 15, checkboxRect.y + 5, 20, 18);
            var newSelected = EditorGUI.Toggle(checkboxCenter, isSelected);

            if (newSelected != isSelected)
            {
                if (newSelected)
                {
                    if (comparisonBuildA == null) comparisonBuildA = build;
                    else if (comparisonBuildB == null) comparisonBuildB = build;
                    else
                    {
                        comparisonBuildA = comparisonBuildB;
                        comparisonBuildB = build;
                    }
                }
                else
                {
                    if (comparisonBuildA == build) comparisonBuildA = null;
                    if (comparisonBuildB == build) comparisonBuildB = null;
                }
            }

            // Date column (wider, accurate timestamp)
            var dateStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            var dateStr = build.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            EditorGUILayout.LabelField(dateStr, dateStyle, GUILayout.Width(160));

            // Commit column (short SHA or '-', selectable for copying)
            var commitStr = (build.git != null && !string.IsNullOrEmpty(build.git.commitSha))
                ? build.git.commitSha.Substring(0, 8)
                : "-";
            var commitStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 6, 0)
            };
            EditorGUILayout.SelectableLabel(commitStr, commitStyle, GUILayout.Width(80), GUILayout.Height(18));

            // Platform
            var platformStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(build.platform, platformStyle, GUILayout.Width(100));

            // Artifact
            var artifactStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(build.artifactType) ? "-" : build.artifactType, artifactStyle, GUILayout.Width(90));

            // Size
            var sizeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatBytes(build.sizeBytes), sizeStyle, GUILayout.Width(100));

            // Time
            var timeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatTime(build.timeSeconds), timeStyle, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("View", GUILayout.Width(60)))
            {
                selectedBuild = build;
                selectedSceneUsageAssetPath = null;
                currentView = ViewMode.Detail;
            }

            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(isBaseline);
            if (GUILayout.Button(isBaseline ? "Baseline" : "Set Base", GUILayout.Width(80)))
            {
                history.SetBaseline(build);
                BuildHistoryStorage.Save(history);
                Repaint();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            if (GUILayout.Button("Export", GUILayout.Width(60)))
            {
                ExportBuild(build);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Build", $"Delete build from {build.TimeAgo}?", "Delete", "Cancel"))
                {
                    BuildHistoryStorage.RemoveBuild(build.guid);
                    if (comparisonBuildA == build) comparisonBuildA = null;
                    if (comparisonBuildB == build) comparisonBuildB = null;
                    Repaint();
                }
            }

            GUILayout.Space(5);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTopTrendChart(BuildHistoryData history)
        {
            if (history.builds.Count < 2) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Build Trends", EditorStyles.boldLabel);

            // Initialize chart platform based on current editor target if possible.
            if (!chartPlatformInitialized)
            {
                var currentPlatform = EditorUserBuildSettings.activeBuildTarget.ToString();
                if (history.builds.Any(b => b.platform == currentPlatform))
                {
                    chartPlatformFilter = currentPlatform;
                }
                chartPlatformInitialized = true;
            }

            // Trend filters (independent from list filters)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform:", GUILayout.Width(60));
            var platformOptions = new List<string> { "Auto" };
            platformOptions.AddRange(history.builds.Select(b => b.platform).Distinct().OrderBy(p => p));
            var platformIndex = Math.Max(0, platformOptions.IndexOf(chartPlatformFilter));
            var newPlatformIndex = EditorGUILayout.Popup(platformIndex, platformOptions.ToArray(), GUILayout.Width(140));
            chartPlatformFilter = platformOptions[newPlatformIndex];

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Artifact:", GUILayout.Width(60));

            var resolvedPlatform = chartPlatformFilter == "Auto"
                ? history.builds.FirstOrDefault()?.platform
                : chartPlatformFilter;

            var artifactOptions = new List<string> { "All" };
            if (!string.IsNullOrEmpty(resolvedPlatform))
            {
                artifactOptions.AddRange(history.builds
                    .Where(b => b.platform == resolvedPlatform)
                    .Select(b => b.artifactType)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct()
                    .OrderBy(a => a));
            }

            if (!artifactOptions.Contains(chartArtifactFilter))
            {
                chartArtifactFilter = "All";
            }

            if (artifactOptions.Count <= 2)
            {
                var label = artifactOptions.Count == 2 ? artifactOptions[1] : "All";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(140));
                chartArtifactFilter = artifactOptions.Count == 2 ? artifactOptions[1] : "All";
            }
            else
            {
                var artifactIndex = Math.Max(0, artifactOptions.IndexOf(chartArtifactFilter));
                var newArtifactIndex = EditorGUILayout.Popup(artifactIndex, artifactOptions.ToArray(), GUILayout.Width(140));
                chartArtifactFilter = artifactOptions[newArtifactIndex];
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            if (string.IsNullOrEmpty(resolvedPlatform))
            {
                EditorGUILayout.LabelField("No builds yet for trends.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            // Keep list filters aligned with trend filters.
            platformFilter = chartPlatformFilter == "Auto" ? "All" : resolvedPlatform;
            artifactFilter = chartArtifactFilter;

            // Get builds to show: most recent ChartMaxPoints, displayed oldest→newest left→right
            var buildsToShow = history.builds
                .Where(b => b.platform == resolvedPlatform)
                .Where(b => chartArtifactFilter == "All" || b.artifactType == chartArtifactFilter)
                .OrderByDescending(b => b.timestampUnix)
                .Take(BuildHistoryData.ChartMaxPoints)
                .OrderBy(b => b.timestampUnix)
                .ToList();

            if (buildsToShow.Count >= 2)
            {
                EditorGUILayout.BeginHorizontal();

                // Build Size Chart
                EditorGUILayout.BeginVertical();
                var sizeValues = buildsToShow.Select(b => (float)b.sizeBytes).ToList();
                var minSize = sizeValues.Min();
                var maxSize = sizeValues.Max();
                var latestSize = sizeValues.Last();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Build Size", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                var sizeStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField($"Latest: {BuildMetricsFormatters.FormatBytes((long)latestSize)}", sizeStyle);
                EditorGUILayout.EndHorizontal();

                var sizeChartRect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
                ChartRenderer.DrawLineChart(sizeChartRect, sizeValues, "", AccentColor);

                // Y-axis labels
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Min: {BuildMetricsFormatters.FormatBytes((long)minSize)}", EditorStyles.miniLabel, GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Max: {BuildMetricsFormatters.FormatBytes((long)maxSize)}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                GUILayout.Space(20);

                // Build Time Chart
                EditorGUILayout.BeginVertical();
                var timeValues = buildsToShow.Select(b => (float)b.timeSeconds).ToList();
                var minTime = timeValues.Min();
                var maxTime = timeValues.Max();
                var latestTime = timeValues.Last();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Build Time", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                var timeStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField($"Latest: {BuildMetricsFormatters.FormatTime((int)latestTime)}", timeStyle);
                EditorGUILayout.EndHorizontal();

                var timeChartRect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
                ChartRenderer.DrawLineChart(timeChartRect, timeValues, "", SuccessColor);

                // Y-axis labels
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Min: {BuildMetricsFormatters.FormatTime((int)minTime)}", EditorStyles.miniLabel, GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Max: {BuildMetricsFormatters.FormatTime((int)maxTime)}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var noDataStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                EditorGUILayout.LabelField("Not enough data to show trend (need at least 2 builds)", noDataStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawListViewFooter(BuildHistoryData history)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"Local builds: {history.builds.Count} / {BuildHistoryData.MaxBuilds}",
                EditorStyles.boldLabel,
                GUILayout.Width(180));
            EditorGUILayout.EndHorizontal();

            if (history.builds.Count >= BuildHistoryData.MaxBuilds)
            {
                EditorGUILayout.HelpBox(
                    $"Build history keeps the most recent {BuildHistoryData.MaxBuilds} builds locally. Older entries are trimmed automatically.",
                    MessageType.Info);
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("No Build History Yet", titleStyle);
            EditorGUILayout.Space(10);

            var descStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            EditorGUILayout.LabelField("Build your project to start tracking build metrics.", descStyle);
            EditorGUILayout.LabelField("Recent builds will be saved automatically.", descStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        private void ExportBuild(BuildRecord build)
        {
            var defaultName = $"build_{build.platform}_{build.Timestamp:yyyyMMdd_HHmmss}.json";
            var path = EditorUtility.SaveFilePanel("Export Build Report", "", defaultName, "json");

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var json = JsonUtility.ToJson(build, true);
                File.WriteAllText(path, json);
                EditorUtility.DisplayDialog("Success", $"Build report exported to:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to export build report:\n{ex.Message}", "OK");
            }
        }
    }
}

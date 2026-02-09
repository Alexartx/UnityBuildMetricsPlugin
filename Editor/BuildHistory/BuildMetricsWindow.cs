using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public class BuildMetricsWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private ViewMode currentView = ViewMode.List;
        private BuildRecord selectedBuild;
        private BuildRecord comparisonBuildA;
        private BuildRecord comparisonBuildB;
        private SortField sortField = SortField.Date;
        private bool sortAscending = false;
        private string platformFilter = "All";
        private string searchQuery = "";
        private string chartPlatformFilter = "Auto";
        private string chartArtifactFilter = "All";
        private bool showFilters = false;
        private string artifactFilter = "All";

        private bool chartPlatformInitialized = false;

        private static readonly Color AccentColor = new Color(0f, 0.85f, 1f);
        private static readonly Color SuccessColor = new Color(0.3f, 0.85f, 0.4f);
        private static readonly Color WarningColor = new Color(1f, 0.6f, 0f);
        private static readonly Color ErrorColor = new Color(0.95f, 0.27f, 0.21f);

        [MenuItem("Tools/Build Metrics/Build History")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildMetricsWindow>("Build Metrics");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            switch (currentView)
            {
                case ViewMode.List:
                    DrawListView();
                    break;
                case ViewMode.Detail:
                    DrawDetailView();
                    break;
                case ViewMode.Comparison:
                    DrawComparisonView();
                    break;
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (currentView != ViewMode.List)
            {
                if (GUILayout.Button("← Back", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    currentView = ViewMode.List;
                    selectedBuild = null;
                }
            }
            else
            {
                GUILayout.Space(75);
            }

            GUILayout.FlexibleSpace();

            if (currentView == ViewMode.List)
            {
                showFilters = GUILayout.Toggle(showFilters, "Filters", EditorStyles.toolbarButton, GUILayout.Width(60));

                var compareEnabled = comparisonBuildA != null && comparisonBuildB != null;
                EditorGUI.BeginDisabledGroup(!compareEnabled);

                if (GUILayout.Button("Compare", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    if (compareEnabled)
                    {
                        currentView = ViewMode.Comparison;
                    }
                }

                EditorGUI.EndDisabledGroup();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                BuildHistoryStorage.ClearCache();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

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
                DrawBuildListItem(build);
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
            GUILayout.Label("Actions", actionsStyle, GUILayout.Width(220));

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

        private void DrawBuildListItem(BuildRecord build)
        {
            var isSelected = comparisonBuildA == build || comparisonBuildB == build;

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
                currentView = ViewMode.Detail;
            }

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

            // Get builds to show (never mix platforms)
            var buildsToShow = history.builds
                .Where(b => b.platform == resolvedPlatform)
                .Where(b => chartArtifactFilter == "All" || b.artifactType == chartArtifactFilter)
                .OrderBy(b => b.timestampUnix)
                .Take(10)
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

            EditorGUILayout.LabelField($"Total Builds: {history.builds.Count}/{BuildHistoryData.MaxBuilds}", EditorStyles.boldLabel);

            if (history.builds.Count >= BuildHistoryData.MaxBuilds)
            {
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(EditorStyles.label) { normal = { textColor = WarningColor } };
                EditorGUILayout.LabelField("⚠ History full. Oldest build will be deleted on next build.", style);
            }

            EditorGUILayout.EndHorizontal();
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

        private void DrawDetailView()
        {
            if (selectedBuild == null)
            {
                currentView = ViewMode.List;
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawBuildSummary(selectedBuild);
            EditorGUILayout.Space(10);

            if (selectedBuild.fileBreakdown != null)
            {
                DrawFileBreakdown(selectedBuild);
                EditorGUILayout.Space(10);
            }

            if (selectedBuild.assetBreakdown != null && selectedBuild.assetBreakdown.hasAssets)
            {
                DrawAssetBreakdown(selectedBuild);
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBuildSummary(BuildRecord build)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Build Summary", EditorStyles.boldLabel);

            DrawInfoRow("Platform", build.platform);
            DrawInfoRow("Artifact", string.IsNullOrWhiteSpace(build.artifactType) ? "-" : build.artifactType);
            DrawInfoRow("Size", BuildMetricsFormatters.FormatBytes(build.sizeBytes));
            DrawInfoRow("Time", BuildMetricsFormatters.FormatTime(build.timeSeconds));
            DrawInfoRow("Unity Version", build.unityVersion);
            DrawInfoRow("Build Date", build.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            DrawInfoRow("Scripting Backend", build.scriptingBackend);

            if (build.git != null && !string.IsNullOrEmpty(build.git.commitSha))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Git Information", EditorStyles.miniBoldLabel);
                DrawInfoRow("Commit", build.git.commitSha.Substring(0, 8));
                DrawInfoRow("Branch", build.git.branch);
                if (!string.IsNullOrEmpty(build.git.commitMessage))
                {
                    EditorGUILayout.LabelField("Message:", EditorStyles.miniLabel);
                    EditorGUILayout.TextArea(build.git.commitMessage, EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField(label + ":", labelStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawFileBreakdown(BuildRecord build)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Breakdown", EditorStyles.boldLabel);

            var categories = GetFileCategories(build.fileBreakdown);
            var total = categories.Sum(c => c.Item2);

            foreach (var category in categories.OrderByDescending(c => c.Item2))
            {
                var percentage = total > 0 ? (category.Item2 / (float)total) * 100f : 0f;
                DrawCategoryBar(category.Item1, category.Item2, percentage, category.Item3);
            }

            EditorGUILayout.Space(10);

            var chartRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            DrawAssetPieChart(chartRect, build);

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetBreakdown(BuildRecord build)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Asset Breakdown (Top 10)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (build.assetBreakdown.topAssets != null && build.assetBreakdown.topAssets.Length > 0)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField("Asset Name", EditorStyles.toolbarButton, GUILayout.Width(300));
                var sizeHeaderStyle = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField("Size", sizeHeaderStyle, GUILayout.Width(100));
                var categoryHeaderStyle = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleCenter };
                EditorGUILayout.LabelField("Type", categoryHeaderStyle, GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                foreach (var asset in build.assetBreakdown.topAssets.Take(10))
                {
                    DrawAssetItem(asset);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetItem(TopFile asset)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

            var fileName = Path.GetFileName(asset.path);
            var tooltip = asset.path;
            EditorGUILayout.LabelField(new GUIContent(fileName, tooltip), GUILayout.Width(300));

            var sizeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatBytes(asset.size), sizeStyle, GUILayout.Width(100));

            var categoryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            EditorGUILayout.LabelField(asset.category, categoryStyle, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Show", GUILayout.Width(60)))
            {
                ShowAssetInProject(asset.path);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowAssetInProject(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void DrawAssetPieChart(Rect rect, BuildRecord build)
        {
            if (build.assetBreakdown == null || !build.assetBreakdown.hasAssets) return;

            var data = new Dictionary<string, long>
            {
                ["Textures"] = build.assetBreakdown.textures?.size ?? 0,
                ["Audio"] = build.assetBreakdown.audio?.size ?? 0,
                ["Models"] = build.assetBreakdown.models?.size ?? 0,
                ["Prefabs"] = build.assetBreakdown.prefabs?.size ?? 0,
                ["Scripts"] = build.assetBreakdown.scripts?.size ?? 0,
                ["Other"] = build.assetBreakdown.otherAssets?.size ?? 0
            };

            var colors = new Dictionary<string, Color>
            {
                ["Textures"] = new Color(0.6f, 0.4f, 0.8f),
                ["Audio"] = new Color(0.3f, 0.6f, 0.9f),
                ["Models"] = new Color(0.3f, 0.8f, 0.5f),
                ["Prefabs"] = new Color(1f, 0.6f, 0.3f),
                ["Scripts"] = new Color(0.4f, 0.8f, 0.4f),
                ["Other"] = new Color(0.5f, 0.5f, 0.5f)
            };

            ChartRenderer.DrawPieChart(rect, data, colors);
        }

        private void DrawCategoryBar(string name, long size, float percentage, Color color)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

            EditorGUILayout.LabelField(name, GUILayout.Width(130));

            var barRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));

            var fillRect = new Rect(barRect.x, barRect.y, barRect.width * (percentage / 100f), barRect.height);
            EditorGUI.DrawRect(fillRect, color);

            var sizeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatBytes(size), sizeStyle, GUILayout.Width(90));

            var percentStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            EditorGUILayout.LabelField($"{percentage:F1}%", percentStyle, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

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

            // Side-by-side comparison table
            DrawComparisonTable();

            EditorGUILayout.Space(10);

            // Visual diff summary
            DrawDiffSummary();

            EditorGUILayout.Space(10);

            // File breakdown changes
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

            // Date row
            DrawComparisonRow("Date",
                comparisonBuildA.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                comparisonBuildB.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

            // Commit row
            var commitA = (comparisonBuildA.git != null && !string.IsNullOrEmpty(comparisonBuildA.git.commitSha))
                ? comparisonBuildA.git.commitSha.Substring(0, 8) : "-";
            var commitB = (comparisonBuildB.git != null && !string.IsNullOrEmpty(comparisonBuildB.git.commitSha))
                ? comparisonBuildB.git.commitSha.Substring(0, 8) : "-";
            DrawComparisonRow("Commit", commitA, commitB);

            // Platform row
            DrawComparisonRow("Platform", comparisonBuildA.platform, comparisonBuildB.platform);

            // Size row
            DrawComparisonRow("Size",
                BuildMetricsFormatters.FormatBytes(comparisonBuildA.sizeBytes),
                BuildMetricsFormatters.FormatBytes(comparisonBuildB.sizeBytes));

            // Time row
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

            // Size comparison
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

            // Time comparison
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

            // Calculate tracked totals
            var trackedTotalA = categoriesA.Values.Sum();
            var trackedTotalB = categoriesB.Values.Sum();

            // Calculate untracked (total build - categorized files)
            var actualTotalA = comparisonBuildA.sizeBytes;
            var actualTotalB = comparisonBuildB.sizeBytes;
            var untrackedA = actualTotalA - trackedTotalA;
            var untrackedB = actualTotalB - trackedTotalB;

            // Show warning if significant amount is untracked
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

            // Get all unique category names, ordered
            var allCategories = categoriesA.Keys.Union(categoriesB.Keys).OrderBy(k => k).ToList();

            foreach (var categoryName in allCategories)
            {
                var sizeA = categoriesA.ContainsKey(categoryName) ? categoriesA[categoryName] : 0;
                var sizeB = categoriesB.ContainsKey(categoryName) ? categoriesB[categoryName] : 0;

                // Skip categories that don't exist in either build
                if (sizeA == 0 && sizeB == 0) continue;

                var delta = sizeB - sizeA;
                var percentage = sizeA > 0 ? ((delta / (float)sizeA) * 100f) : (sizeB > 0 ? 100f : 0f);
                var isSignificant = Math.Abs(percentage) > 5f;

                EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

                // Category name (bold if significant change)
                var categoryStyle = isSignificant
                    ? new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold }
                    : EditorStyles.label;
                var displayName = categoryName == "Plugins" ? "Plugins/Unity core" : categoryName;
                EditorGUILayout.LabelField(displayName, categoryStyle, GUILayout.Width(120));

                // Build A size
                var sizeAStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(sizeA > 0 ? BuildMetricsFormatters.FormatBytes(sizeA) : "-", sizeAStyle, GUILayout.Width(100));

                // Build B size
                var sizeBStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(sizeB > 0 ? BuildMetricsFormatters.FormatBytes(sizeB) : "-", sizeBStyle, GUILayout.Width(100));

                // Change indicator
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
                var untrackedPercentage = untrackedA > 0 ? ((untrackedDelta / (float)untrackedA) * 100f) : (untrackedB > 0 ? 100f : 0f);

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
                ("Scenes", breakdown.scenes?.size ?? 0, new Color(1f, 0.4f, 0.6f)),
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
                ["Scenes"] = breakdown.scenes?.size ?? 0,
                ["Shaders"] = breakdown.shaders?.size ?? 0,
                ["Other"] = breakdown.other?.size ?? 0
            };
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

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private enum ViewMode
        {
            List,
            Detail,
            Comparison
        }
    }
}

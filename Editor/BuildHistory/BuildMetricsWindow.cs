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
        private bool showFilters = false;

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

            if (DrawSortableHeader("Platform", 100, SortField.Platform, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Platform);
            }

            if (DrawSortableHeader("Size", 100, SortField.Size, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Size);
            }

            if (DrawSortableHeader("Time", 80, SortField.Time, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Time);
            }

            if (DrawSortableHeader("Date", 120, SortField.Date, TextAnchor.MiddleCenter))
            {
                ToggleSort(SortField.Date);
            }

            var trendStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Trend", trendStyle, GUILayout.Width(80));

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

            EditorGUILayout.LabelField(build.platform, GUILayout.Width(80));

            var sizeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatBytes(build.sizeBytes), sizeStyle, GUILayout.Width(100));

            var timeStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(BuildMetricsFormatters.FormatTime(build.timeSeconds), timeStyle, GUILayout.Width(90));

            var dateStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(build.TimeAgo, dateStyle, GUILayout.Width(110));

            var sparklineRect = GUILayoutUtility.GetRect(80, 20);
            DrawBuildSparkline(sparklineRect, build);

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

        private void DrawBuildSparkline(Rect rect, BuildRecord currentBuild)
        {
            var history = BuildHistoryStorage.Load();
            var builds = history.builds.Where(b => b.platform == currentBuild.platform).Take(10).Reverse().ToList();

            if (builds.Count < 2)
            {
                var noDataStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                GUI.Label(rect, "—", noDataStyle);
                return;
            }

            var values = builds.Select(b => (float)b.sizeBytes).ToList();
            ChartRenderer.DrawSparkline(rect, values, AccentColor);
        }

        private void DrawListViewFooter(BuildHistoryData history)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Total Builds: {history.builds.Count}/10", EditorStyles.boldLabel);

            if (history.builds.Count >= 10)
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
            EditorGUILayout.LabelField("Up to 10 builds will be saved automatically.", descStyle);

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
            EditorGUILayout.LabelField("File Breakdown", EditorStyles.boldLabel);

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
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
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

            EditorGUILayout.LabelField($"Comparing: {comparisonBuildA.platform} ({comparisonBuildA.TimeAgo}) vs {comparisonBuildB.platform} ({comparisonBuildB.TimeAgo})",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            DrawComparisonMetric("Size", comparisonBuildA.sizeBytes, comparisonBuildB.sizeBytes, true);
            DrawComparisonMetric("Time", comparisonBuildA.timeSeconds, comparisonBuildB.timeSeconds, false);

            EditorGUILayout.Space(10);
            DrawFileBreakdownComparison();

            EditorGUILayout.Space(10);
            DrawTrendCharts();

            EditorGUILayout.EndScrollView();
        }

        private void DrawComparisonMetric(string label, long valueA, long valueB, bool isSize)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{label} Comparison", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var delta = valueB - valueA;
            var percentage = valueA > 0 ? (delta / (float)valueA) * 100f : 0f;

            var formatA = isSize ? BuildMetricsFormatters.FormatBytes(valueA) : BuildMetricsFormatters.FormatTime((int)valueA);
            var formatB = isSize ? BuildMetricsFormatters.FormatBytes(valueB) : BuildMetricsFormatters.FormatTime((int)valueB);

            EditorGUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField("Build A:", labelStyle, GUILayout.Width(100));
            EditorGUILayout.LabelField(formatA);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Build B:", labelStyle, GUILayout.Width(100));
            EditorGUILayout.LabelField(formatB);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), new Color(0.3f, 0.3f, 0.3f));
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Change:", labelStyle, GUILayout.Width(100));

            var deltaStr = BuildMetricsFormatters.FormatDelta(delta, isSize);
            var percentStr = BuildMetricsFormatters.FormatPercentage(percentage);
            var color = delta > 0 ? WarningColor : SuccessColor;

            var deltaStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = color } };
            EditorGUILayout.LabelField($"{deltaStr} ({percentStr})", deltaStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawFileBreakdownComparison()
        {
            if (comparisonBuildA.fileBreakdown == null || comparisonBuildB.fileBreakdown == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("File Breakdown Changes", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var categoriesA = GetFileCategorySizes(comparisonBuildA.fileBreakdown);
            var categoriesB = GetFileCategorySizes(comparisonBuildB.fileBreakdown);

            var hasChanges = false;
            foreach (var categoryName in categoriesA.Keys.Union(categoriesB.Keys).OrderBy(k => k))
            {
                var sizeA = categoriesA.ContainsKey(categoryName) ? categoriesA[categoryName] : 0;
                var sizeB = categoriesB.ContainsKey(categoryName) ? categoriesB[categoryName] : 0;
                var delta = sizeB - sizeA;

                if (delta != 0)
                {
                    hasChanges = true;
                    EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

                    EditorGUILayout.LabelField(categoryName, GUILayout.Width(150));

                    var deltaStr = BuildMetricsFormatters.FormatDelta(delta, true);
                    var percentage = sizeA > 0 ? ((delta / (float)sizeA) * 100f) : 0f;
                    var percentStr = BuildMetricsFormatters.FormatPercentage(percentage);

                    var color = delta > 0 ? WarningColor : SuccessColor;
                    var icon = delta > 0 ? "⚠" : "✓";
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };

                    EditorGUILayout.LabelField($"{icon} {deltaStr} ({percentStr})", style);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }

            if (!hasChanges)
            {
                var noChangeStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
                EditorGUILayout.LabelField("No changes detected", noChangeStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTrendCharts()
        {
            var history = BuildHistoryStorage.Load();
            var platformBuilds = history.builds
                .Where(b => b.platform == comparisonBuildA.platform)
                .OrderBy(b => b.timestampUnix)
                .Take(10)
                .ToList();

            if (platformBuilds.Count < 2) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Trends", EditorStyles.boldLabel);

            var sizeChartRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            var sizeValues = platformBuilds.Select(b => (float)b.sizeBytes).ToList();
            ChartRenderer.DrawLineChart(sizeChartRect, sizeValues, "Size Trend", AccentColor);

            EditorGUILayout.Space(10);

            var timeChartRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            var timeValues = platformBuilds.Select(b => (float)b.timeSeconds).ToList();
            ChartRenderer.DrawLineChart(timeChartRect, timeValues, "Time Trend", new Color(0.3f, 0.85f, 0.4f));

            EditorGUILayout.EndVertical();
        }

        private List<(string name, long size, Color color)> GetFileCategories(FileBreakdown breakdown)
        {
            return new List<(string name, long size, Color color)>
            {
                ("Scripts", breakdown.scripts?.size ?? 0, new Color(0.3f, 0.6f, 0.9f)),
                ("Resources", breakdown.resources?.size ?? 0, new Color(0.3f, 0.85f, 0.5f)),
                ("Streaming Assets", breakdown.streamingAssets?.size ?? 0, new Color(0.6f, 0.4f, 0.8f)),
                ("Plugins", breakdown.plugins?.size ?? 0, new Color(1f, 0.6f, 0.3f)),
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

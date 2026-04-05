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
        private void DrawDetailView()
        {
            if (selectedBuild == null)
            {
                currentView = ViewMode.List;
                return;
            }

            var history = BuildHistoryStorage.Load();
            var baselineBuild = history.GetBaseline(selectedBuild);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawBuildSummary(selectedBuild, history, baselineBuild);
            EditorGUILayout.Space(10);

            DrawInsightCards(selectedBuild, baselineBuild);
            EditorGUILayout.Space(10);

            DrawWhatChanged(selectedBuild, baselineBuild);
            EditorGUILayout.Space(10);

            DrawBuildSteps(selectedBuild);
            EditorGUILayout.Space(10);

            DrawAndroidPackageAnatomy(selectedBuild);
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

            DrawSceneUsage(selectedBuild);
            EditorGUILayout.Space(10);

            DrawEngineModules(selectedBuild);
            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        private void DrawBuildSummary(BuildRecord build, BuildHistoryData history, BuildRecord baselineBuild)
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

            EditorGUILayout.Space(8);
            DrawBaselineAndBudgets(build, history, baselineBuild);

            EditorGUILayout.EndVertical();
        }

        private void DrawBaselineAndBudgets(BuildRecord build, BuildHistoryData history, BuildRecord baselineBuild)
        {
            var profile = history.GetProfile(build.platform, build.artifactType, createIfMissing: true);

            EditorGUILayout.LabelField("Baselines & Budgets", EditorStyles.miniBoldLabel);
            DrawInfoRow("Configuration", $"{build.platform} • {build.ArtifactLabel}");

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(baselineBuild != null && baselineBuild.guid == build.guid);
            if (GUILayout.Button("Set Current As Baseline", GUILayout.Width(170)))
            {
                history.SetBaseline(build);
                BuildHistoryStorage.Save(history);
                baselineBuild = build;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(profile.baselineGuid));
            if (GUILayout.Button("Clear Baseline", GUILayout.Width(120)))
            {
                history.ClearBaseline(build);
                BuildHistoryStorage.Save(history);
                baselineBuild = null;
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (baselineBuild == null)
            {
                EditorGUILayout.HelpBox(
                    "Pin a baseline build for this platform/artifact to unlock local deltas, budgets, and the What Got Bigger panel.",
                    MessageType.Info);
            }
            else if (baselineBuild.guid == build.guid)
            {
                EditorGUILayout.HelpBox("This build is the pinned baseline for its configuration.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Comparing against baseline {baselineBuild.buildName} ({baselineBuild.Timestamp:yyyy-MM-dd HH:mm}).",
                    MessageType.None);

                DrawDeltaStatusRow(
                    "Size vs Baseline",
                    build.sizeBytes,
                    baselineBuild.sizeBytes,
                    bytes => BuildMetricsFormatters.FormatBytes(bytes),
                    SuccessColor,
                    WarningColor);

                DrawDeltaStatusRow(
                    "Time vs Baseline",
                    build.timeSeconds,
                    baselineBuild.timeSeconds,
                    seconds => BuildMetricsFormatters.FormatTime((int)seconds),
                    SuccessColor,
                    WarningColor);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Budgets", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            var sizeBudgetMb = profile.sizeBudgetBytes > 0 ? profile.sizeBudgetBytes / (1024f * 1024f) : 0f;
            sizeBudgetMb = EditorGUILayout.FloatField("Size Budget (MB)", sizeBudgetMb);
            var timeBudgetSeconds = EditorGUILayout.IntField("Time Budget (sec)", profile.timeBudgetSeconds);
            if (EditorGUI.EndChangeCheck())
            {
                profile.sizeBudgetBytes = Math.Max(0L, (long)Math.Round(sizeBudgetMb * 1024f * 1024f));
                profile.timeBudgetSeconds = Math.Max(0, timeBudgetSeconds);
                BuildHistoryStorage.Save(history);
            }

            DrawBudgetStatusRow(
                "Size Budget",
                build.sizeBytes,
                profile.sizeBudgetBytes,
                bytes => BuildMetricsFormatters.FormatBytes(bytes));

            DrawBudgetStatusRow(
                "Time Budget",
                build.timeSeconds,
                profile.timeBudgetSeconds,
                seconds => BuildMetricsFormatters.FormatTime((int)seconds));
        }

        private void DrawDeltaStatusRow(
            string label,
            long currentValue,
            long baselineValue,
            Func<long, string> formatter,
            Color improvementColor,
            Color regressionColor)
        {
            var delta = currentValue - baselineValue;
            var deltaPercent = baselineValue > 0 ? (delta / (float)baselineValue) * 100f : 0f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(120));

            if (delta == 0)
            {
                EditorGUILayout.LabelField("No change", EditorStyles.miniLabel);
            }
            else
            {
                var color = delta < 0 ? improvementColor : regressionColor;
                var icon = delta < 0 ? "✓" : "⚠";
                var text = delta < 0
                    ? $"{icon} {formatter(Math.Abs(delta))} smaller/faster ({Math.Abs(deltaPercent):F1}%)"
                    : $"{icon} {formatter(delta)} larger/slower ({deltaPercent:F1}%)";
                var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
                EditorGUILayout.LabelField(text, style);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBudgetStatusRow(string label, long currentValue, long budgetValue, Func<long, string> formatter)
        {
            if (budgetValue <= 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("No budget set", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                return;
            }

            var ratio = currentValue / (float)budgetValue;
            var stateText = ratio > 1f
                ? "Over budget"
                : ratio >= 0.9f ? "Close to budget" : "Within budget";
            var stateColor = ratio > 1f
                ? ErrorColor
                : ratio >= 0.9f ? WarningColor : SuccessColor;

            var detail = ratio > 1f
                ? $"{formatter(Math.Max(0L, currentValue - budgetValue))} over"
                : $"{formatter(currentValue)} / {formatter(budgetValue)}";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(120));
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = stateColor } };
            EditorGUILayout.LabelField($"{stateText} • {detail}", style);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInsightCards(BuildRecord build, BuildRecord baselineBuild)
        {
            var cards = BuildInsightCards(build, baselineBuild);
            if (cards.Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Insights", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            foreach (var card in cards)
            {
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = card.color;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = previousColor;

                EditorGUILayout.LabelField(card.title, EditorStyles.miniBoldLabel);
                var style = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false };
                EditorGUILayout.LabelField(card.body, style);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }

        private List<InsightCard> BuildInsightCards(BuildRecord build, BuildRecord baselineBuild)
        {
            var cards = new List<InsightCard>();
            var assetBreakdown = build.assetBreakdown;
            var fileBreakdown = build.fileBreakdown;

            if (assetBreakdown?.hasAssets == true && assetBreakdown.totalAssetsSize > 0)
            {
                var totalAssetSize = Math.Max(1L, assetBreakdown.totalAssetsSize);
                var textureSize = assetBreakdown.textures?.size ?? 0;
                var fontSize = assetBreakdown.fonts?.size ?? 0;
                var audioSize = assetBreakdown.audio?.size ?? 0;

                if (textureSize >= BuildMetricsThresholds.TextureWarningBytes || textureSize >= totalAssetSize * BuildMetricsThresholds.TextureWarningRatio)
                {
                    cards.Add(new InsightCard(
                        "Textures dominate the payload",
                        $"Textures account for {BuildMetricsFormatters.FormatBytes(textureSize)} ({(textureSize / (float)totalAssetSize) * 100f:F1}% of attributed assets). Review large atlases, background images, and import compression settings.",
                        new Color(0.28f, 0.22f, 0.42f)));
                }

                if (fontSize >= BuildMetricsThresholds.FontWarningBytes || fontSize >= totalAssetSize * BuildMetricsThresholds.FontWarningRatio)
                {
                    cards.Add(new InsightCard(
                        "Fonts are heavier than usual",
                        $"Fonts/TMP assets contribute {BuildMetricsFormatters.FormatBytes(fontSize)}. Trim unused TMP font assets or reduce atlas resolution if these are only needed for a few locales.",
                        new Color(0.38f, 0.2f, 0.28f)));
                }

                if (audioSize >= BuildMetricsThresholds.AudioWarningBytes || audioSize >= totalAssetSize * BuildMetricsThresholds.AudioWarningRatio)
                {
                    cards.Add(new InsightCard(
                        "Audio footprint is meaningful",
                        $"Audio contributes {BuildMetricsFormatters.FormatBytes(audioSize)}. Check music loops, compression quality, and whether long clips can be streamed instead of packed.",
                        new Color(0.17f, 0.28f, 0.44f)));
                }
            }

            var streamingAssetsSize = fileBreakdown?.streamingAssets?.size ?? 0;
            if (streamingAssetsSize >= BuildMetricsThresholds.StreamingAssetsWarningBytes || streamingAssetsSize >= build.sizeBytes * BuildMetricsThresholds.StreamingAssetsWarningRatio)
            {
                cards.Add(new InsightCard(
                    "StreamingAssets is noticeably large",
                    $"StreamingAssets contributes {BuildMetricsFormatters.FormatBytes(streamingAssetsSize)}. Large raw files here bypass Unity compression and can inflate every build.",
                    new Color(0.33f, 0.24f, 0.12f)));
            }

            var pluginSize = fileBreakdown?.plugins?.size ?? 0;
            var nativeSize = build.androidPackageInsight?.nativeLibrariesSize ?? 0;
            if (pluginSize >= BuildMetricsThresholds.PluginWarningBytes || pluginSize >= build.sizeBytes * BuildMetricsThresholds.PluginWarningRatio || nativeSize >= BuildMetricsThresholds.NativeLibraryWarningBytes)
            {
                var source = nativeSize > 0 ? nativeSize : pluginSize;
                cards.Add(new InsightCard(
                    "Plugins or native libraries dominate",
                    $"Plugins/native code account for about {BuildMetricsFormatters.FormatBytes(source)}. Review SDK/plugin packages, strip unused ABIs, and check whether optional integrations can be removed.",
                    new Color(0.34f, 0.18f, 0.14f)));
            }

            if (baselineBuild != null && baselineBuild.guid != build.guid)
            {
                var textureDelta = (assetBreakdown?.textures?.size ?? 0) - (baselineBuild.assetBreakdown?.textures?.size ?? 0);
                if (textureDelta > BuildMetricsThresholds.TextureDeltaWarningBytes)
                {
                    cards.Add(new InsightCard(
                        "Texture usage grew since baseline",
                        $"Textures are up by {BuildMetricsFormatters.FormatBytes(textureDelta)} vs baseline. The What Got Bigger panel can help narrow that to folders and assets.",
                        new Color(0.34f, 0.25f, 0.1f)));
                }
            }

            return cards;
        }

        private void DrawWhatChanged(BuildRecord build, BuildRecord baselineBuild)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("What Got Bigger?", EditorStyles.boldLabel);

            if (baselineBuild == null)
            {
                EditorGUILayout.HelpBox("Set a baseline for this configuration to see the biggest movers by category, folder, and asset.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (baselineBuild.guid == build.guid)
            {
                EditorGUILayout.HelpBox("This build is the current baseline, so there is nothing to diff against yet.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawChangeList("Category Movers", BuildCategoryChanges(build, baselineBuild), 4);
            DrawChangeList("Folder Movers", BuildFolderChanges(build, baselineBuild), 5);
            DrawChangeList("Asset Movers", BuildAssetChanges(build, baselineBuild), 5);

            EditorGUILayout.EndVertical();
        }

        private void DrawChangeList(string title, List<ChangeItem> items, int limit)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);

            var topItems = items.Where(item => item.deltaBytes > 0).OrderByDescending(item => item.deltaBytes).Take(limit).ToList();
            if (topItems.Count == 0)
            {
                EditorGUILayout.LabelField("No material growth detected.", EditorStyles.miniLabel);
                return;
            }

            foreach (var item in topItems)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.name, GUILayout.Width(260));
                var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight, normal = { textColor = WarningColor } };
                EditorGUILayout.LabelField($"+{BuildMetricsFormatters.FormatBytes(item.deltaBytes)}", style, GUILayout.Width(90));
                EditorGUILayout.LabelField($"Now {BuildMetricsFormatters.FormatBytes(item.currentBytes)}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBuildSteps(BuildRecord build)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Build Steps", EditorStyles.boldLabel);

            if (build.buildSteps == null || build.buildSteps.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No detailed build-step data was captured for this build. Unity only records this extra detail when the build is created with DetailedBuildReport enabled.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var maxDuration = Math.Max(1L, build.buildSteps.Max(step => step.durationMs));
            foreach (var step in build.buildSteps.OrderByDescending(step => step.durationMs))
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
                GUILayout.Space(step.depth * 10f);
                EditorGUILayout.LabelField(step.name, GUILayout.Width(Mathf.Max(80f, 280f - step.depth * 10f)));
                var rect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
                var fillRect = new Rect(rect.x, rect.y, rect.width * (step.durationMs / (float)maxDuration), rect.height);
                EditorGUI.DrawRect(fillRect, AccentColor);
                EditorGUILayout.LabelField(FormatDurationMs(step.durationMs), GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAndroidPackageAnatomy(BuildRecord build)
        {
            if (!string.Equals(build.platform, "Android", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Android Package Anatomy", EditorStyles.boldLabel);

            var insight = build.androidPackageInsight;
            if (insight == null)
            {
                EditorGUILayout.HelpBox("Package anatomy was not available for this Android build.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawCategoryBar("Native Libs", insight.nativeLibrariesSize, GetPercentage(insight.nativeLibrariesSize, build.sizeBytes), new Color(0.9f, 0.47f, 0.25f));
            DrawCategoryBar("DEX / Code", insight.dexCodeSize, GetPercentage(insight.dexCodeSize, build.sizeBytes), new Color(0.28f, 0.64f, 0.96f));
            DrawCategoryBar("Android Resources", insight.androidResourcesSize, GetPercentage(insight.androidResourcesSize, build.sizeBytes), new Color(0.43f, 0.82f, 0.46f));
            DrawCategoryBar("Unity Data", insight.unityDataSize, GetPercentage(insight.unityDataSize, build.sizeBytes), new Color(0.86f, 0.46f, 0.71f));
            DrawCategoryBar("Streaming Assets", insight.streamingAssetsSize, GetPercentage(insight.streamingAssetsSize, build.sizeBytes), new Color(0.56f, 0.4f, 0.84f));

            if (insight.manifestSize > 0)
            {
                DrawInfoRow("Manifest", BuildMetricsFormatters.FormatBytes(insight.manifestSize));
            }

            if (insight.sdkInsights != null && insight.sdkInsights.Length > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("SDK Heuristics", EditorStyles.miniBoldLabel);
                foreach (var sdk in insight.sdkInsights)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{sdk.name} • {BuildMetricsFormatters.FormatBytes(sdk.sizeBytes)}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField($"{sdk.fileCount} matching files or package hints", EditorStyles.miniLabel);
                    foreach (var evidence in (sdk.evidence ?? Array.Empty<string>()).Take(3))
                    {
                        EditorGUILayout.LabelField($"• {evidence}", EditorStyles.wordWrappedLabel);
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneUsage(BuildRecord build)
        {
            if (build.sceneUsage == null || build.sceneUsage.Length == 0)
            {
                return;
            }

            var usage = string.IsNullOrWhiteSpace(selectedSceneUsageAssetPath)
                ? build.sceneUsage.OrderByDescending(item => item.sizeBytes).FirstOrDefault()
                : build.sceneUsage.FirstOrDefault(item => string.Equals(item.assetPath, selectedSceneUsageAssetPath, StringComparison.OrdinalIgnoreCase));

            if (usage == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Scene Usage Attribution", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Path.GetFileName(usage.assetPath), EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"{usage.scenePaths.Length} scene(s) reference this asset", EditorStyles.miniLabel);
            foreach (var scenePath in usage.scenePaths)
            {
                EditorGUILayout.LabelField($"• {scenePath}", EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEngineModules(BuildRecord build)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity Engine Modules", EditorStyles.boldLabel);

            if (build.engineModules == null || build.engineModules.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No stripping/module data was captured for this build. Unity only reports included modules when stripping information is available.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var module in build.engineModules)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(module.name, EditorStyles.miniBoldLabel);
                if (module.reasons != null && module.reasons.Length > 0)
                {
                    foreach (var reason in module.reasons)
                    {
                        EditorGUILayout.LabelField($"• {reason}", EditorStyles.wordWrappedLabel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Included, but Unity did not provide a detailed reason.", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
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

        private static float GetPercentage(long value, long total)
        {
            return total > 0 ? (value / (float)total) * 100f : 0f;
        }

        private static string FormatDurationMs(long durationMs)
        {
            if (durationMs < 1000)
            {
                return $"{durationMs} ms";
            }

            return BuildMetricsFormatters.FormatTime(Mathf.RoundToInt(durationMs / 1000f));
        }

        private void DrawFileBreakdown(BuildRecord build)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Breakdown", EditorStyles.boldLabel);

            var categories = GetFileCategories(build.fileBreakdown);
            var total = categories.Sum(c => c.size);

            foreach (var category in categories.OrderByDescending(c => c.size))
            {
                var percentage = total > 0 ? (category.size / (float)total) * 100f : 0f;
                DrawCategoryBar(category.name, category.size, percentage, category.color);
            }

            EditorGUILayout.Space(10);

            var chartRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            DrawFileBreakdownPieChart(chartRect, categories);

            EditorGUILayout.EndVertical();
        }

        private void DrawFileBreakdownPieChart(Rect rect, List<(string name, long size, Color color)> categories)
        {
            if (categories == null || categories.Count == 0) return;

            var data   = categories.ToDictionary(c => c.name, c => c.size);
            var colors = categories.ToDictionary(c => c.name, c => c.color);
            ChartRenderer.DrawPieChart(rect, data, colors);
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
                    DrawAssetItem(build, asset);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetItem(BuildRecord build, TopFile asset)
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

            var sceneUsage = GetSceneUsageForAsset(build, asset.path);
            EditorGUI.BeginDisabledGroup(sceneUsage == null);
            if (GUILayout.Button("Scenes", GUILayout.Width(70)))
            {
                selectedSceneUsageAssetPath = asset.path;
            }
            EditorGUI.EndDisabledGroup();

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

        private AssetSceneUsage GetSceneUsageForAsset(BuildRecord build, string assetPath)
        {
            if (build?.sceneUsage == null || string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            return build.sceneUsage.FirstOrDefault(usage => string.Equals(usage.assetPath, assetPath, StringComparison.OrdinalIgnoreCase));
        }

        private List<ChangeItem> BuildCategoryChanges(BuildRecord build, BuildRecord baselineBuild)
        {
            var current = GetFileCategorySizes(build.fileBreakdown);
            var baseline = GetFileCategorySizes(baselineBuild.fileBreakdown);

            return current.Keys
                .Union(baseline.Keys)
                .Select(key => new ChangeItem(
                    key,
                    current.TryGetValue(key, out var currentBytes) ? currentBytes : 0L,
                    baseline.TryGetValue(key, out var baselineBytes) ? baselineBytes : 0L))
                .ToList();
        }

        private List<ChangeItem> BuildFolderChanges(BuildRecord build, BuildRecord baselineBuild)
        {
            return BuildChangeItems(
                build.assetBreakdown?.topFolders?.ToDictionary(folder => folder.path, folder => folder.size, StringComparer.OrdinalIgnoreCase),
                baselineBuild.assetBreakdown?.topFolders?.ToDictionary(folder => folder.path, folder => folder.size, StringComparer.OrdinalIgnoreCase));
        }

        private List<ChangeItem> BuildAssetChanges(BuildRecord build, BuildRecord baselineBuild)
        {
            return BuildChangeItems(
                build.assetBreakdown?.topAssets?.ToDictionary(asset => asset.path, asset => asset.size, StringComparer.OrdinalIgnoreCase),
                baselineBuild.assetBreakdown?.topAssets?.ToDictionary(asset => asset.path, asset => asset.size, StringComparer.OrdinalIgnoreCase));
        }

        private List<ChangeItem> BuildChangeItems(
            IDictionary<string, long> current,
            IDictionary<string, long> baseline)
        {
            current = current ?? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            baseline = baseline ?? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            return current.Keys
                .Union(baseline.Keys, StringComparer.OrdinalIgnoreCase)
                .Select(key => new ChangeItem(
                    key,
                    current.TryGetValue(key, out var currentBytes) ? currentBytes : 0L,
                    baseline.TryGetValue(key, out var baselineBytes) ? baselineBytes : 0L))
                .ToList();
        }
    }
}

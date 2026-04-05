using System;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public partial class BuildMetricsWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private ViewMode currentView = ViewMode.List;
        private BuildRecord selectedBuild;
        private BuildRecord comparisonBuildA;
        private BuildRecord comparisonBuildB;
        private string selectedSceneUsageAssetPath;
        private SortField sortField = SortField.Date;
        private bool sortAscending = false;
        private string platformFilter = "All";
        private string searchQuery = "";
        private string chartPlatformFilter = "Auto";
        private string chartArtifactFilter = "All";
        private bool chartPlatformInitialized = false;
        private bool showFilters = false;
        private string artifactFilter = "All";

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
            BuildMetricsExtensions.DrawTopPanels(this);

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
                    selectedSceneUsageAssetPath = null;
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

        private readonly struct ChangeItem
        {
            public ChangeItem(string name, long currentBytes, long baselineBytes)
            {
                this.name = name;
                this.currentBytes = currentBytes;
                this.baselineBytes = baselineBytes;
                deltaBytes = currentBytes - baselineBytes;
            }

            public readonly string name;
            public readonly long currentBytes;
            public readonly long baselineBytes;
            public readonly long deltaBytes;
        }

        private readonly struct InsightCard
        {
            public InsightCard(string title, string body, Color color)
            {
                this.title = title;
                this.body = body;
                this.color = color;
            }

            public readonly string title;
            public readonly string body;
            public readonly Color color;
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsMenu
    {
        private const string MenuRoot = "Tools/Build Metrics/";
        private const int MenuPriority = 1000;

        [MenuItem(MenuRoot + "Open Reports Folder", false, MenuPriority + 20)]
        public static void OpenReportsFolder()
        {
            Directory.CreateDirectory(BuildMetricsStorage.ReportsDirectory);
            EditorUtility.RevealInFinder(BuildMetricsStorage.ReportsDirectory);
        }

        [MenuItem(MenuRoot + "Documentation", false, MenuPriority + 21)]
        public static void OpenDocumentation()
        {
            var asset = BuildMetricsPackagePaths.LoadPreferredDocumentationAsset();

            if (asset == null)
            {
                EditorUtility.DisplayDialog(
                    "Documentation Not Found",
                    "The local Build Metrics documentation file could not be found.",
                    "OK"
                );
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }

        [MenuItem(MenuRoot + "About", false, MenuPriority + 60)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "Build Metrics",
                $"Build Metrics for Unity\n" +
                $"Version {BuildMetricsConstants.Version}\n\n" +
                $"Track build performance and catch regressions locally.\n\n" +
                $"Install the optional Build Metrics Cloud add-on if you want dashboard upload and CI onboarding.\n\n" +
                $"Support: {BuildMetricsConstants.SupportEmail}",
                "OK"
            );
        }
    }
}

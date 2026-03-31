using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class BuildMetricsPackagePaths
    {
        private static string cachedInstallRoot;

        public static string InstallRoot => cachedInstallRoot ?? (cachedInstallRoot = ResolveInstallRoot());
        public static string ReadmeAssetPath => CombineAssetPath(InstallRoot, "README.md");
        public static string InstallationGuideAssetPath => GetFirstExistingAssetPath(
            CombineAssetPath(InstallRoot, "Documentation~/installation.md"),
            CombineAssetPath(InstallRoot, "Documentation/installation.md"));
        public static string PackageOverviewAssetPath => GetFirstExistingAssetPath(
            CombineAssetPath(InstallRoot, "Documentation~/asset-store.md"),
            CombineAssetPath(InstallRoot, "Documentation/asset-store.md"));
        public static string OfflineFeaturesAssetPath => GetFirstExistingAssetPath(
            CombineAssetPath(InstallRoot, "Documentation~/offline-features.md"),
            CombineAssetPath(InstallRoot, "Documentation/offline-features.md"));

        public static Object LoadPrimaryDocumentationAsset()
        {
            return AssetDatabase.LoadAssetAtPath<Object>(InstallationGuideAssetPath)
                ?? AssetDatabase.LoadAssetAtPath<Object>(ReadmeAssetPath);
        }

        private static string ResolveInstallRoot()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BuildMetricsPackagePaths).Assembly);
            if (packageInfo != null && !string.IsNullOrWhiteSpace(packageInfo.assetPath))
            {
                return NormalizeAssetPath(packageInfo.assetPath);
            }

            var candidates = AssetDatabase.FindAssets("BuildMetricsMenu t:Script")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith("/Editor/BuildMetricsMenu.cs", System.StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var assetPath = candidates.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                return NormalizeAssetPath(Path.GetDirectoryName(Path.GetDirectoryName(assetPath)) ?? "Assets/BuildMetrics");
            }

            return "Assets/BuildMetrics";
        }

        private static string GetFirstExistingAssetPath(params string[] candidates)
        {
            return candidates.FirstOrDefault(AssetExists) ?? candidates.FirstOrDefault() ?? string.Empty;
        }

        private static bool AssetExists(string assetPath)
        {
            return !string.IsNullOrWhiteSpace(assetPath) && AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null;
        }

        private static string CombineAssetPath(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return relativePath.Replace('\\', '/');
            }

            return NormalizeAssetPath(Path.Combine(root, relativePath));
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}

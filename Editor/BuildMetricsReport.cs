using System;
using UnityEngine;

namespace BuildMetrics.Editor
{
    [Serializable]
    public class BuildMetricsReport
    {
        public string project;
        public string bundleId;
        public string platform;
        public string unityVersion;
        public string buildGuid;
        public string timestamp;
        public string status;
        public int buildTimeSeconds;
        public long outputSizeBytes;
        public string outputPath;
        public string artifactType;
        public string artifactExtension;
        public bool developmentBuild;
        public bool allowDebugging;
        public bool scriptDebugging;
        public string scriptingBackend;
        public MachineInfo machine;
        public BuildSummary summary;

        // Build Detail: Git & File Breakdown
        public string buildName;
        public string buildNumber;
        public GitInfo git;
        public FileBreakdown fileBreakdown;
        public AssetBreakdown assetBreakdown;
        public BuildStepInfo[] buildSteps;
        public AssetSceneUsage[] sceneUsage;
        public EngineModuleInfo[] engineModules;
        public AndroidPackageInsight androidPackageInsight;
    }

    [Serializable]
    public class MachineInfo
    {
        public string os;
        public string cpu;
        public int ramGb;
    }

    [Serializable]
    public class BuildSummary
    {
        public int errors;
        public int warnings;
    }

    [Serializable]
    public class GitInfo
    {
        public string commitSha;
        public string commitMessage;
        public string branch;
        public bool isDirty;
    }

    [Serializable]
    public class FileBreakdown
    {
        public FileCategoryData scripts;
        public FileCategoryData resources;
        public FileCategoryData streamingAssets;
        public FileCategoryData plugins;
        public FileCategoryData scenes;
        public FileCategoryData shaders;
        public FileCategoryData other;
    }

    [Serializable]
    public class FileCategoryData
    {
        public long size;
        public int count;
        public OtherBreakdown breakdown; // Optional: detailed breakdown for "other" category
    }

    [Serializable]
    public class OtherBreakdown
    {
        public FileCategorySubData spriteAtlases;
        public FileCategorySubData textures;
        public FileCategorySubData meshes;
        public FileCategorySubData audio;
        public FileCategorySubData assetBundles;
        public FileCategorySubData unityRuntime;
        public FileCategorySubData fonts;

        // iOS-specific categories (actionable)
        public FileCategorySubData iosAssetCatalogs; // Assets.car
        public FileCategorySubData iosAppResources; // .png, .jpg, .storyboardc, .nib

        // Android-specific categories (actionable)
        public FileCategorySubData androidAddressables; // assets/aa/, .bundle
        public FileCategorySubData androidUnityData; // assets/bin/Data/, sharedassets
        public FileCategorySubData androidResources; // res/, resources.arsc
        public FileCategorySubData androidCode; // classes.dex

        // WebGL-specific categories
        public FileCategorySubData webglData; // *.data
        public FileCategorySubData webglWasm; // *.wasm
        public FileCategorySubData webglJs; // *.js

        // System files (collapsed by default, not user-actionable)
        public FileCategorySubData iosSystem; // Frameworks, SwiftSupport, PlugIns, CodeSignature
        public FileCategorySubData androidSystem; // lib/, jniLibs/

        public FileCategorySubData other; // Truly unknown files
    }

    [Serializable]
    public class FileCategorySubData
    {
        public long size;
        public int count;
    }

    [Serializable]
    public class TopFile
    {
        public string path;
        public long size;
        public string category;
    }

    [Serializable]
    public class TopFolder
    {
        public string path;
        public long size;
        public int count;
    }

    [Serializable]
    public class BuildStepInfo
    {
        public string name;
        public int depth;
        public long durationMs;
        public int messageCount;
    }

    [Serializable]
    public class AssetSceneUsage
    {
        public string assetPath;
        public string category;
        public long sizeBytes;
        public string[] scenePaths;
    }

    [Serializable]
    public class EngineModuleInfo
    {
        public string name;
        public string[] reasons;
    }

    [Serializable]
    public class AndroidPackageInsight
    {
        public long nativeLibrariesSize;
        public long dexCodeSize;
        public long androidResourcesSize;
        public long unityDataSize;
        public long streamingAssetsSize;
        public long manifestSize;
        public AndroidSdkInsight[] sdkInsights;
    }

    [Serializable]
    public class AndroidSdkInsight
    {
        public string name;
        public long sizeBytes;
        public int fileCount;
        public string[] evidence;
    }

    [Serializable]
    public class AssetBreakdown
    {
        public bool hasAssets;
        public long totalAssetsSize;
        public int totalAssets;
        public AssetCategoryData textures;
        public AssetCategoryData spriteAtlases;
        public AssetCategoryData audio;
        public AssetCategoryData models;
        public AssetCategoryData animations;
        public AssetCategoryData prefabs;
        public AssetCategoryData scenes;
        public AssetCategoryData scripts;
        public AssetCategoryData shaders;
        public AssetCategoryData materials;
        public AssetCategoryData fonts;
        public AssetCategoryData videos;
        public AssetCategoryData otherAssets;
        public TopFile[] topAssets; // Top 20 largest assets from Assets/**
        public TopFolder[] topFolders; // Top 10 folders from the deduplicated asset list
    }

    [Serializable]
    public class AssetCategoryData
    {
        public long size;
        public int count;
    }
}

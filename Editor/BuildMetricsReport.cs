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
    }

    [Serializable]
    public class TopFile
    {
        public string path;
        public long size;
        public string category;
    }

    [Serializable]
    public class AssetBreakdown
    {
        public bool hasAssets;
        public long totalAssetsSize;
        public int totalAssets;
        public AssetCategoryData textures;
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
    }

    [Serializable]
    public class AssetCategoryData
    {
        public long size;
        public int count;
    }
}

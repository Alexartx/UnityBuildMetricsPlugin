using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildMetrics.Editor
{
    [Serializable]
    public class BuildHistoryData
    {
        public List<BuildRecord> builds = new List<BuildRecord>();
        public List<BaselineBudgetProfile> profiles = new List<BaselineBudgetProfile>();

        public const int MaxBuilds    = 20;
        public const int ChartMaxPoints = 20; // how many recent builds the trend chart shows

        public void AddBuild(BuildRecord record)
        {
            builds.RemoveAll(b => b.guid == record.guid);
            builds.Insert(0, record);

            if (builds.Count > MaxBuilds)
            {
                builds.RemoveRange(MaxBuilds, builds.Count - MaxBuilds);
            }
        }

        public void RemoveBuild(string guid)
        {
            builds.RemoveAll(b => b.guid == guid);
        }

        public BuildRecord GetBuild(string guid)
        {
            return builds.FirstOrDefault(b => b.guid == guid);
        }

        public IEnumerable<BuildRecord> GetSortedBuilds(SortField field, bool ascending)
        {
            var sorted = field switch
            {
                SortField.Date => builds.OrderBy(b => b.timestampUnix),
                SortField.Size => builds.OrderBy(b => b.sizeBytes),
                SortField.Time => builds.OrderBy(b => b.timeSeconds),
                SortField.Platform => builds.OrderBy(b => b.platform),
                _ => builds.OrderBy(b => b.timestampUnix)
            };

            return ascending ? sorted : sorted.Reverse();
        }

        public IEnumerable<BuildRecord> FilterBuilds(string platformFilter, DateTime? startDate, DateTime? endDate)
        {
            var filtered = builds.AsEnumerable();

            if (!string.IsNullOrEmpty(platformFilter) && platformFilter != "All")
            {
                filtered = filtered.Where(b => b.platform == platformFilter);
            }

            if (startDate.HasValue)
            {
                filtered = filtered.Where(b => b.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filtered = filtered.Where(b => b.Timestamp <= endDate.Value);
            }

            return filtered;
        }

        public List<string> GetUniquePlatforms()
        {
            var platforms = new List<string> { "All" };
            platforms.AddRange(builds.Select(b => b.platform).Distinct().OrderBy(p => p));
            return platforms;
        }

        public BuildRecord GetBaseline(BuildRecord build)
        {
            if (build == null)
            {
                return null;
            }

            var profile = GetProfile(build.platform, build.artifactType);
            if (profile == null || string.IsNullOrWhiteSpace(profile.baselineGuid))
            {
                return null;
            }

            return builds.FirstOrDefault(candidate => candidate.guid == profile.baselineGuid);
        }

        public BaselineBudgetProfile GetProfile(string platform, string artifactType, bool createIfMissing = false)
        {
            var key = GetConfigurationKey(platform, artifactType);
            var profile = profiles.FirstOrDefault(existing => existing.key == key);
            if (profile == null && createIfMissing)
            {
                profile = new BaselineBudgetProfile
                {
                    key = key,
                    platform = platform ?? string.Empty,
                    artifactType = artifactType ?? string.Empty
                };
                profiles.Add(profile);
            }

            return profile;
        }

        public void SetBaseline(BuildRecord build)
        {
            if (build == null)
            {
                return;
            }

            var profile = GetProfile(build.platform, build.artifactType, createIfMissing: true);
            profile.baselineGuid = build.guid;
        }

        public void ClearBaseline(BuildRecord build)
        {
            if (build == null)
            {
                return;
            }

            var profile = GetProfile(build.platform, build.artifactType);
            if (profile != null)
            {
                profile.baselineGuid = null;
            }
        }

        public static string GetConfigurationKey(string platform, string artifactType)
        {
            return $"{platform ?? string.Empty}::{artifactType ?? string.Empty}";
        }
    }

    [Serializable]
    public class BaselineBudgetProfile
    {
        public string key;
        public string platform;
        public string artifactType;
        public string baselineGuid;
        public long sizeBudgetBytes;
        public int timeBudgetSeconds;
    }

    public enum SortField
    {
        Date,
        Size,
        Time,
        Platform
    }
}

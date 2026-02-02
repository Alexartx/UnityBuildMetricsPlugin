using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildMetrics.Editor
{
    [Serializable]
    public class BuildHistoryData
    {
        public List<BuildRecord> builds = new List<BuildRecord>();

        private const int MaxBuilds = 10;

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
    }

    public enum SortField
    {
        Date,
        Size,
        Time,
        Platform
    }
}

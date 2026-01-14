using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace BuildMetrics.Editor
{
    /// <summary>
    /// Collects git information using git CLI.
    /// Falls back gracefully if git is not available or not a git repository.
    /// </summary>
    public static class GitInfoCollector
    {
        /// <summary>
        /// Collect git information from the current repository.
        /// Returns null if git is not available or this is not a git repository.
        /// </summary>
        public static GitInfo Collect()
        {
            if (!IsGitAvailable())
            {
                UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Git not available, skipping git info collection");
                return null;
            }

            if (!IsGitRepository())
            {
                UnityEngine.Debug.Log($"{BuildMetricsConstants.LogPrefix} Not a git repository, skipping git info collection");
                return null;
            }

            try
            {
                var commitSha = RunGitCommand("rev-parse HEAD");
                var branch = RunGitCommand("rev-parse --abbrev-ref HEAD");
                var commitMessage = RunGitCommand("log -1 --pretty=%B");
                var isDirty = !string.IsNullOrWhiteSpace(RunGitCommand("status --porcelain"));

                if (string.IsNullOrWhiteSpace(commitSha))
                {
                    return null;
                }

                return new GitInfo
                {
                    commitSha = commitSha.Trim(),
                    commitMessage = commitMessage?.Trim().Split('\n')[0] ?? "", // First line only
                    branch = branch?.Trim() ?? "unknown",
                    isDirty = isDirty
                };
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} Failed to collect git info: {ex.Message}");
                return null;
            }
        }

        private static bool IsGitAvailable()
        {
            try
            {
                var output = RunGitCommand("--version");
                return !string.IsNullOrWhiteSpace(output) && output.Contains("git version");
            }
            catch
            {
                return false;
            }
        }

        private static bool IsGitRepository()
        {
            try
            {
                var output = RunGitCommand("rev-parse --git-dir");
                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        private static string RunGitCommand(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Application.dataPath // Unity project root
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
                {
                    throw new Exception($"Git command failed: {error}");
                }

                return output;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to run git command '{arguments}': {ex.Message}");
            }
        }
    }
}

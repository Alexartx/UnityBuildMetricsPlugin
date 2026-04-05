using System;

namespace BuildMetrics.Editor
{
    /// <summary>
    /// Session-only store for the last upload result.
    /// Cleared on editor restart. Used to show a status bar in the Build History window.
    /// </summary>
    public static class BuildMetricsStatus
    {
        public enum UploadState { None, Uploading, Success, Failed }

        public static UploadState LastUploadState { get; private set; } = UploadState.None;
        public static string LastUploadDetail { get; private set; } = "";
        public static string LastUploadError  { get; private set; } = "";
        public static DateTime LastUploadTime { get; private set; }

        /// <summary>Fired on the Editor main thread whenever state changes.</summary>
        public static event Action OnStatusChanged;

        public static void SetUploading()
        {
            LastUploadState  = UploadState.Uploading;
            LastUploadDetail = "Uploading...";
            LastUploadError  = "";
            LastUploadTime   = DateTime.Now;
            OnStatusChanged?.Invoke();
        }

        /// <param name="platform">e.g. "Android"</param>
        /// <param name="sizeBytes">output size in bytes</param>
        /// <param name="timeSeconds">build time in seconds</param>
        public static void SetSuccess(string platform, long sizeBytes, int timeSeconds)
        {
            LastUploadState  = UploadState.Success;
            LastUploadDetail = $"{platform}  ·  {BuildMetricsFormatters.FormatBytes(sizeBytes)}  ·  {BuildMetricsFormatters.FormatTime(timeSeconds)}";
            LastUploadError  = "";
            LastUploadTime   = DateTime.Now;
            OnStatusChanged?.Invoke();
        }

        public static void SetFailed(string reason)
        {
            LastUploadState  = UploadState.Failed;
            LastUploadDetail = "";
            LastUploadError  = reason;
            LastUploadTime   = DateTime.Now;
            OnStatusChanged?.Invoke();
        }

        public static void Clear()
        {
            LastUploadState  = UploadState.None;
            LastUploadDetail = "";
            LastUploadError  = "";
            OnStatusChanged?.Invoke();
        }
    }
}

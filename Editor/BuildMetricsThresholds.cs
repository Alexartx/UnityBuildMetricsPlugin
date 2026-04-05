namespace BuildMetrics.Editor
{
    internal static class BuildMetricsThresholds
    {
        public const long TextureWarningBytes        = 20L * 1024 * 1024;
        public const float TextureWarningRatio       = 0.45f;

        public const long FontWarningBytes           = 3L * 1024 * 1024;
        public const float FontWarningRatio          = 0.08f;

        public const long AudioWarningBytes          = 5L * 1024 * 1024;
        public const float AudioWarningRatio         = 0.12f;

        public const long StreamingAssetsWarningBytes  = 5L * 1024 * 1024;
        public const float StreamingAssetsWarningRatio = 0.08f;

        public const long PluginWarningBytes         = 15L * 1024 * 1024;
        public const float PluginWarningRatio        = 0.3f;
        public const long NativeLibraryWarningBytes  = 8L * 1024 * 1024;

        public const long TextureDeltaWarningBytes   = 2L * 1024 * 1024;
    }
}

namespace BuildMetrics.Editor
{
    internal static class BuildMetricsCloudConstants
    {
        public const string DashboardUrl = "https://app.buildmetrics.moonlightember.com/dashboard";
        public const string DocsUrl = "https://moonlightember.com/products/build-metrics/docs";
        public const string DefaultApiUrl = "https://buildmetrics-api.onrender.com/api/builds";
        public const string SetupCompletePref = "BuildMetrics.SetupComplete";
        public const string SetupDismissedPref = "BuildMetrics.SetupDismissed";

        public const string ApiKeyPrefix = "bm_";
        public const int ApiKeyMinLength = 20;
        public const int ApiTimeoutSeconds = 10;

        public const int WizardWidth = 500;
        public const int WizardHeight = 400;
    }
}

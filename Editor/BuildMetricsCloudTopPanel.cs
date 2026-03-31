using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    internal sealed class BuildMetricsCloudTopPanel : IBuildMetricsTopPanelExtension
    {
        public int Order => 100;

        public void DrawTopPanel(BuildMetricsWindow window)
        {
            var state = BuildMetricsStatus.LastUploadState;
            if (state == BuildMetricsStatus.UploadState.None)
            {
                return;
            }

            var pendingCount = BuildMetricsCloudStorage.GetPendingReports().Length;
            var messageColor = GetMessageColor(state);
            var message = BuildStatusMessage(state, pendingCount);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = false,
                richText = false
            };
            labelStyle.normal.textColor = messageColor;

            GUILayout.Label(message, labelStyle);
            GUILayout.FlexibleSpace();

            if (state == BuildMetricsStatus.UploadState.Success)
            {
                if (GUILayout.Button("View Dashboard", GUILayout.Width(120)))
                {
                    Application.OpenURL(BuildMetricsCloudConstants.DashboardUrl);
                }
            }
            else if (state == BuildMetricsStatus.UploadState.Failed)
            {
                if (GUILayout.Button("Fix Setup", GUILayout.Width(90)))
                {
                    BuildMetricsSetupWizard.ShowWizard();
                }
            }

            if (pendingCount > 0 && GUILayout.Button("Upload Pending", GUILayout.Width(110)))
            {
                BuildMetricsUploader.TryUploadPending();
            }

            if (GUILayout.Button("x", GUILayout.Width(24)))
            {
                BuildMetricsStatus.Clear();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static Color GetMessageColor(BuildMetricsStatus.UploadState state)
        {
            switch (state)
            {
                case BuildMetricsStatus.UploadState.Uploading:
                    return new Color(0.15f, 0.45f, 0.85f);
                case BuildMetricsStatus.UploadState.Success:
                    return new Color(0.15f, 0.55f, 0.2f);
                default:
                    return new Color(0.85f, 0.2f, 0.2f);
            }
        }

        private static string BuildStatusMessage(BuildMetricsStatus.UploadState state, int pendingCount)
        {
            switch (state)
            {
                case BuildMetricsStatus.UploadState.Uploading:
                    return "Uploading build metrics to Build Metrics Cloud...";
                case BuildMetricsStatus.UploadState.Success:
                    return string.IsNullOrWhiteSpace(BuildMetricsStatus.LastUploadDetail)
                        ? "Build metrics uploaded successfully."
                        : $"Build metrics uploaded: {BuildMetricsStatus.LastUploadDetail}";
                default:
                    return pendingCount > 0
                        ? $"Cloud upload failed: {BuildMetricsStatus.LastUploadError} ({pendingCount} pending)"
                        : $"Cloud upload failed: {BuildMetricsStatus.LastUploadError}";
            }
        }
    }
}

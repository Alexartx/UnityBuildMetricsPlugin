using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BuildMetrics.Editor
{
    public class BuildMetricsUploader
    {
        public static bool ValidateApiKey(string apiKey, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                errorMessage = "API key is empty";
                return false;
            }

            // Basic format validation
            if (!apiKey.StartsWith("bm_"))
            {
                errorMessage = "Invalid API key format. Must start with 'bm_'";
                return false;
            }

            if (apiKey.Length < 20)
            {
                errorMessage = "API key is too short";
                return false;
            }

            return true;
        }
        public static void TryUploadPending()
        {
            var pending = BuildMetricsStorage.GetPendingReports();
            foreach (var reportPath in pending)
            {
                TryUploadReport(reportPath, isPending: true);
            }
        }

        public static void TryUploadReport(string reportPath, bool isPending = false)
        {
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
            {
                return;
            }

            var apiUrl = BuildMetricsSettings.ApiUrl;
            var apiKey = BuildMetricsSettings.ApiKey;

            // Validate API key
            if (!ValidateApiKey(apiKey, out string validationError))
            {
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} {validationError}. " +
                    $"Get your API key at: {BuildMetricsConstants.DashboardUrl}");

                if (!isPending)
                {
                    BuildMetricsStorage.Enqueue(reportPath);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                Debug.LogError($"{BuildMetricsConstants.LogPrefix} API URL is not configured");
                if (!isPending)
                {
                    BuildMetricsStorage.Enqueue(reportPath);
                }
                return;
            }

            var json = File.ReadAllText(reportPath);
            var request = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            SendRequest(request, success =>
            {
                if (success)
                {
                    if (isPending)
                    {
                        File.Delete(reportPath);
                    }
                }
                else if (!isPending)
                {
                    BuildMetricsStorage.Enqueue(reportPath);
                }
            });
        }

        private static void SendRequest(UnityWebRequest request, Action<bool> onComplete)
        {
            var operation = request.SendWebRequest();

            void Update()
            {
                if (!operation.isDone)
                {
                    return;
                }

                EditorApplication.update -= Update;
                bool success = request.result == UnityWebRequest.Result.Success;

                if (success)
                {
                    Debug.Log($"{BuildMetricsConstants.LogPrefix} Build metrics uploaded successfully! " +
                        $"View dashboard: {BuildMetricsConstants.DashboardUrl}");
                }
                else
                {
                    var errorMessage = $"{BuildMetricsConstants.LogPrefix} Upload failed: {request.result}";
                    if (!string.IsNullOrWhiteSpace(request.error))
                    {
                        errorMessage += $" - {request.error}";
                    }
                    if (!string.IsNullOrWhiteSpace(request.downloadHandler?.text))
                    {
                        errorMessage += $"\n{BuildMetricsConstants.LogPrefix} Response: {request.downloadHandler.text}";
                    }

                    // Add helpful context based on error type
                    if (request.responseCode == 401 || request.responseCode == 403)
                    {
                        errorMessage += $"\n{BuildMetricsConstants.LogPrefix} Check your API key at: {BuildMetricsConstants.DashboardUrl}";
                    }
                    else if (request.responseCode == 402 || request.responseCode == 429)
                    {
                        errorMessage += $"\n{BuildMetricsConstants.LogPrefix} Quota exceeded. Upgrade your plan at: {BuildMetricsConstants.DashboardUrl}/billing";
                    }

                    Debug.LogError(errorMessage);
                }
                onComplete?.Invoke(success);
                request.Dispose();
            }

            EditorApplication.update += Update;
        }
    }
}

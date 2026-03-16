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
        /// <summary>
        /// Local format check only — fast, no network.
        /// </summary>
        public static bool ValidateApiKey(string apiKey, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                errorMessage = "API key is empty";
                return false;
            }

            if (!apiKey.StartsWith("bm_"))
            {
                errorMessage = "Invalid API key format (must start with 'bm_')";
                return false;
            }

            if (apiKey.Length < 20)
            {
                errorMessage = "API key is too short";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Contacts the server to verify the key is active.
        /// Calls <paramref name="onResult"/> with (isValid, message) on the main thread.
        /// </summary>
        public static void ValidateApiKeyWithServer(string apiKey, Action<bool, string> onResult)
        {
            // Quick local check first
            if (!ValidateApiKey(apiKey, out var localError))
            {
                onResult?.Invoke(false, localError);
                return;
            }

            // Derive validate endpoint: strip path and append /api/validate
            var baseUrl     = BuildMetricsSettings.ApiUrl; // e.g. https://host/api/builds
            string validateUrl;
            try
            {
                var uri = new Uri(baseUrl);
                validateUrl = $"{uri.Scheme}://{uri.Authority}/api/validate";
            }
            catch
            {
                validateUrl = baseUrl.Replace("/api/builds", "/api/validate");
            }
            var projectsUrl = validateUrl;

            var request = UnityWebRequest.Get(projectsUrl);
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.timeout = 10;

            var operation = request.SendWebRequest();

            void Update()
            {
                try
                {
                    if (!operation.isDone)
                        return;

                    EditorApplication.update -= Update;

                    bool ok = request.result == UnityWebRequest.Result.Success
                              || request.responseCode == 200;

                    string msg;
                    if (ok)
                    {
                        msg = "✓ API key is valid — settings saved.";
                    }
                    else if (request.responseCode == 401 || request.responseCode == 403)
                    {
                        msg = "API key not recognised. Double-check you copied it correctly.";
                        ok  = false;
                    }
                    else
                    {
                        // Network issue / server down — pass format-valid as true to not block user
                        msg = $"Could not reach server (HTTP {request.responseCode}). " +
                              "Key format is valid — saving anyway.";
                        ok  = true;
                    }

                    request.Dispose();
                    onResult?.Invoke(ok, msg);
                }
                catch (Exception ex)
                {
                    EditorApplication.update -= Update;
                    request.Dispose();
                    onResult?.Invoke(true, $"Validation check failed ({ex.Message}). Key format valid — saving anyway.");
                }
            }

            EditorApplication.update += Update;
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
                var msg = $"{validationError}. Get your API key at: {BuildMetricsConstants.DashboardUrl}";
                Debug.LogWarning($"{BuildMetricsConstants.LogPrefix} {msg}");
                BuildMetricsStatus.SetFailed(validationError);

                if (!isPending)
                {
                    BuildMetricsStorage.Enqueue(reportPath);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                const string msg = "API URL is not configured";
                Debug.LogError($"{BuildMetricsConstants.LogPrefix} {msg}");
                BuildMetricsStatus.SetFailed(msg);
                if (!isPending)
                {
                    BuildMetricsStorage.Enqueue(reportPath);
                }
                return;
            }

            var json = File.ReadAllText(reportPath);

            // Parse lightweight build info for the status bar
            var reportSummary = TryParseReportSummary(json);

            var request = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            BuildMetricsStatus.SetUploading();

            SendRequest(request, (success, responseCode, errorText) =>
            {
                if (success)
                {
                    BuildMetricsStatus.SetSuccess(
                        reportSummary.platform,
                        reportSummary.outputSizeBytes,
                        reportSummary.buildTimeSeconds);

                    if (isPending)
                    {
                        File.Delete(reportPath);
                    }
                }
                else
                {
                    BuildMetricsStatus.SetFailed(errorText);
                    if (!isPending)
                    {
                        BuildMetricsStorage.Enqueue(reportPath);
                    }
                }
            });
        }

        // Minimal struct so we avoid re-parsing the full BuildMetricsReport
        [Serializable]
        private struct ReportSummary
        {
            public string platform;
            public long   outputSizeBytes;
            public int    buildTimeSeconds;
        }

        private static ReportSummary TryParseReportSummary(string json)
        {
            try { return JsonUtility.FromJson<ReportSummary>(json); }
            catch { return new ReportSummary { platform = "Unknown" }; }
        }

        private static void SendRequest(UnityWebRequest request, Action<bool, long, string> onComplete)
        {
            var operation = request.SendWebRequest();

            void Update()
            {
                try
                {
                    if (!operation.isDone)
                        return;

                    EditorApplication.update -= Update;

                    bool success    = request.result == UnityWebRequest.Result.Success;
                    long statusCode = request.responseCode;
                    string errorText;

                    if (success)
                    {
                        Debug.Log($"{BuildMetricsConstants.LogPrefix} Build metrics uploaded successfully! " +
                            $"View dashboard: {BuildMetricsConstants.DashboardUrl}");
                        errorText = "";
                    }
                    else
                    {
                        errorText = BuildFriendlyError(request);
                        Debug.LogError($"{BuildMetricsConstants.LogPrefix} {errorText}");
                    }

                    onComplete?.Invoke(success, statusCode, errorText);
                    request.Dispose();
                }
                catch (Exception ex)
                {
                    EditorApplication.update -= Update;
                    var msg = $"Unexpected upload error: {ex.Message}";
                    Debug.LogError($"{BuildMetricsConstants.LogPrefix} {msg}");
                    onComplete?.Invoke(false, 0, msg);
                    request.Dispose();
                }
            }

            EditorApplication.update += Update;
        }

        private static string BuildFriendlyError(UnityWebRequest request)
        {
            var msg = $"Upload failed: {request.result}";

            if (!string.IsNullOrWhiteSpace(request.error))
                msg += $" — {request.error}";

            if (request.responseCode == 401 || request.responseCode == 403)
                msg = $"Invalid API key. Check your key at: {BuildMetricsConstants.DashboardUrl}";
            else if (request.responseCode == 402 || request.responseCode == 429)
                msg = $"Quota exceeded — upgrade your plan at: {BuildMetricsConstants.DashboardUrl}/billing";

            return msg;
        }
    }
}

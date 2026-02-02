using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMetrics.Editor
{
    public static class ChartRenderer
    {
        public static void DrawLineChart(Rect rect, List<float> values, string label, Color color)
        {
            if (values == null || values.Count == 0) return;

            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            var paddingRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 30);

            var maxValue = values.Max();
            var minValue = values.Min();
            var range = maxValue - minValue;

            if (range < 0.01f)
            {
                range = maxValue * 0.1f;
                minValue = maxValue - range * 0.5f;
            }

            var points = new List<Vector3>();
            for (int i = 0; i < values.Count; i++)
            {
                var x = paddingRect.x + (i / (float)(values.Count - 1)) * paddingRect.width;
                var normalizedValue = range > 0 ? (values[i] - minValue) / range : 0.5f;
                var y = paddingRect.y + paddingRect.height - (normalizedValue * paddingRect.height);
                points.Add(new Vector3(x, y, 0));
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(3f, points.ToArray());

            for (int i = 0; i < points.Count; i++)
            {
                Handles.DrawSolidDisc(points[i], Vector3.forward, 3f);
            }

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.gray },
                fontSize = 10
            };

            GUI.Label(new Rect(rect.x + 5, rect.y + 2, rect.width, 20), label, labelStyle);
        }

        public static void DrawSparkline(Rect rect, List<float> values, Color color)
        {
            if (values == null || values.Count < 2) return;

            var maxValue = values.Max();
            var minValue = values.Min();
            var range = maxValue - minValue;

            if (range < 0.01f) return;

            var padding = 4f;
            var paddedRect = new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2, rect.height - padding * 2);

            var points = new List<Vector3>();
            for (int i = 0; i < values.Count; i++)
            {
                var x = paddedRect.x + (i / (float)(values.Count - 1)) * paddedRect.width;
                var normalizedValue = (values[i] - minValue) / range;
                var y = paddedRect.y + paddedRect.height - (normalizedValue * paddedRect.height);
                points.Add(new Vector3(x, y, 0));
            }

            Handles.color = color;
            Handles.DrawAAPolyLine(2f, points.ToArray());
        }

        public static void DrawPieChart(Rect rect, Dictionary<string, long> data, Dictionary<string, Color> colors)
        {
            if (data == null || data.Count == 0) return;

            var total = data.Values.Sum();
            if (total == 0) return;

            var center = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
            var radius = Mathf.Min(rect.width, rect.height) / 2 - 10;

            var startAngle = 0f;

            foreach (var kvp in data.OrderByDescending(x => x.Value))
            {
                var percentage = kvp.Value / (float)total;
                var sweepAngle = percentage * 360f;

                if (sweepAngle > 1f)
                {
                    var color = colors.ContainsKey(kvp.Key) ? colors[kvp.Key] : Color.gray;
                    Handles.color = color;
                    Handles.DrawSolidArc(center, Vector3.forward,
                        Quaternion.Euler(0, 0, startAngle) * Vector3.up,
                        sweepAngle, radius);
                }

                startAngle += sweepAngle;
            }
        }
    }
}

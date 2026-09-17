using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Loads chart JSON files from a Resources folder and deserializes
    /// them into ChartData, ready for NoteSpawner to consume.
    /// </summary>
    public static class ChartLoader
    {
        /// <summary>
        /// Loads and parses the chart at the given Resources-relative path
        /// (no file extension, e.g. "Charts/test-song").
        /// </summary>
        public static ChartData LoadChart(string resourcePath)
        {
            TextAsset json = Resources.Load<TextAsset>(resourcePath);
            if (json == null)
            {
                UnityEngine.Debug.LogError($"[ChartLoader] No chart found at Resources/{resourcePath}.json");
                return null;
            }

            ChartData chart = JsonUtility.FromJson<ChartData>(json.text);
            if (chart == null)
            {
                UnityEngine.Debug.LogError($"[ChartLoader] Failed to parse chart JSON at Resources/{resourcePath}.json");
                return null;
            }

            if (chart.notes == null)
            {
                UnityEngine.Debug.LogWarning($"[ChartLoader] Chart '{resourcePath}' has no notes array.");
                return chart;
            }

            EnsureSortedByTime(chart.notes);
            return chart;
        }

        private static void EnsureSortedByTime(System.Collections.Generic.List<NoteData> notes)
        {
            for (int i = 1; i < notes.Count; i++)
            {
                if (notes[i].time < notes[i - 1].time)
                {
                    notes.Sort((a, b) => a.time.CompareTo(b.time));
                    return;
                }
            }
        }
    }
}

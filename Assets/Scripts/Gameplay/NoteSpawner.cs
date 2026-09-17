using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Loads a chart and spawns its notes just far enough ahead of time
    /// (leadTime) that each one travels from its spawn point to the
    /// strumline exactly as its target beat arrives.
    /// </summary>
    public class NoteSpawner : MonoBehaviour
    {
        [Header("Chart")]
        [Tooltip("Resources-relative path to the chart JSON, no file extension.")]
        public string chartResourcePath = "Charts/test-song";

        [Header("Timing")]
        [Tooltip("Seconds a note is visible/travelling before its hit time.")]
        public float leadTime = 2f;

        [Header("Lane Layout")]
        [Tooltip("Must match the strumline's receptor spacing so notes line up with it.")]
        public float laneSpacing = 1.5f;

        [Tooltip("Must match the strumline's Y position.")]
        public float strumlineY = 3.5f;

        [Tooltip("Y position notes spawn at, above the strumline.")]
        public float spawnY = 9f;

        [Tooltip("World-space size of a spawned note.")]
        public float noteSize = 1f;

        [Header("Optional Prefab")]
        [Tooltip("If assigned, instantiated for each note instead of generating one at runtime. Should have a Note component and a SpriteRenderer.")]
        public GameObject notePrefab;

        private const int LaneCount = 4;

        private ChartData chart;
        private int nextNoteIndex;
        private Color[] laneColors;

        private void Start()
        {
            chart = ChartLoader.LoadChart(chartResourcePath);
            laneColors = PlaceholderAssetFactory.CreateLaneColors();

            if (chart == null || chart.notes == null || chart.notes.Count == 0)
            {
                UnityEngine.Debug.LogError($"[NoteSpawner] No usable chart loaded from '{chartResourcePath}'; nothing will spawn.");
            }
        }

        private void Update()
        {
            if (chart == null || chart.notes == null)
            {
                return;
            }

            Conductor conductor = Conductor.Instance;
            if (conductor == null || !conductor.SongHasStarted)
            {
                return;
            }

            float songPosition = conductor.SongPositionInSeconds;

            while (nextNoteIndex < chart.notes.Count &&
                   songPosition >= chart.notes[nextNoteIndex].time - leadTime)
            {
                SpawnNote(chart.notes[nextNoteIndex]);
                nextNoteIndex++;
            }
        }

        private void SpawnNote(NoteData data)
        {
            float laneX = GetLaneX(data.lane);
            Vector3 spawnPosition = new Vector3(laneX, spawnY, 0f);
            Vector3 strumlinePosition = new Vector3(laneX, strumlineY, 0f);

            GameObject noteObject = notePrefab != null
                ? Instantiate(notePrefab)
                : CreatePlaceholderNoteObject(data);

            Note note = noteObject.GetComponent<Note>();
            if (note == null)
            {
                note = noteObject.AddComponent<Note>();
            }

            note.Initialize(data, spawnPosition, strumlinePosition);
        }

        private GameObject CreatePlaceholderNoteObject(NoteData data)
        {
            GameObject noteObject = new GameObject($"Note_lane{data.lane}_t{data.time:F2}");

            SpriteRenderer renderer = noteObject.AddComponent<SpriteRenderer>();
            Color laneColor = data.lane >= 0 && data.lane < laneColors.Length
                ? laneColors[data.lane]
                : Color.white;
            renderer.sprite = PlaceholderAssetFactory.CreateSolidSprite(laneColor);

            noteObject.transform.localScale = Vector3.one * noteSize;

            return noteObject;
        }

        private float GetLaneX(int lane)
        {
            float totalWidth = (LaneCount - 1) * laneSpacing;
            float startX = -totalWidth / 2f;
            return startX + lane * laneSpacing;
        }
    }
}

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
        [Tooltip("Resources-relative path to the chart JSON, no file extension. Ignored if Auto Generate From Audio is on.")]
        public string chartResourcePath = "Charts/test-song";

        [Header("Auto-Generated Chart (optional)")]
        [Tooltip("If on, ignore chartResourcePath and generate a chart from songClip's audio instead.")]
        public bool autoGenerateFromAudio = false;

        [Tooltip("Song to analyze when Auto Generate From Audio is on. Should be the same clip playing on the Conductor's AudioSource.")]
        public AudioClip songClip;

        [Tooltip("Onset-detection tuning for auto-generated charts.")]
        public AutoChartSettings autoChartSettings = AutoChartSettings.Default;

        [Header("Timing")]
        [Tooltip("Seconds a note is visible/travelling before its hit time.")]
        public float leadTime = 2f;

        [Header("Visuals")]
        [Tooltip("Directional arrow sprite, drawn facing up. Rotated per lane at runtime (FNF-style: one " +
                 "sprite reused for all 4 lanes) and tinted with that lane's colour. Ignored if Note Prefab " +
                 "is assigned. If left unassigned, falls back to a plain coloured square.")]
        public Sprite arrowSprite;

        [Header("Lane Layout")]
        [Tooltip("Must match the strumline's receptor spacing so notes line up with it. Keep this bigger than " +
                 "the arrow sprite's world-space width (sprite pixel width / its Pixels Per Unit, times Note " +
                 "Size) or adjacent lanes will visually overlap.")]
        public float laneSpacing = 2.8f;

        [Tooltip("Must match the strumline's Y position.")]
        public float strumlineY = 3.5f;

        [Tooltip("Y position notes spawn at, below the strumline (off-screen at the bottom).")]
        public float spawnY = -7f;

        [Tooltip("World-space size of a spawned note.")]
        public float noteSize = 1f;

        [Header("Optional Prefab")]
        [Tooltip("If assigned, instantiated for each note instead of generating one at runtime. Should have a Note component and a SpriteRenderer.")]
        public GameObject notePrefab;

        private const int LaneCount = 4;

        private ChartData chart;
        private int nextNoteIndex;
        private Color[] laneColors;
        private bool chartLoadFailed;

        private void Start()
        {
            laneColors = PlaceholderAssetFactory.CreateLaneColors();

            if (!autoGenerateFromAudio)
            {
                chart = ChartLoader.LoadChart(chartResourcePath);
                if (chart == null || chart.notes == null || chart.notes.Count == 0)
                {
                    UnityEngine.Debug.LogError($"[NoteSpawner] No usable chart loaded from '{chartResourcePath}'; nothing will spawn.");
                    chartLoadFailed = true;
                }
            }
            // Auto-generation needs the Conductor's bpm, which may not exist yet
            // (e.g. TestSceneSetup creates it in its own Start()), so it's deferred to Update.
        }

        private void Update()
        {
            Conductor conductor = Conductor.Instance;

            if (chart == null && !chartLoadFailed)
            {
                if (!autoGenerateFromAudio || conductor == null)
                {
                    return;
                }

                if (songClip == null)
                {
                    UnityEngine.Debug.LogError("[NoteSpawner] Auto Generate From Audio is on but no songClip is assigned.");
                    chartLoadFailed = true;
                    return;
                }

                chart = AutoChartGenerator.Generate(songClip, songClip.name, conductor.bpm, autoChartSettings, conductor.tempoMap);
                if (chart == null || chart.notes == null || chart.notes.Count == 0)
                {
                    UnityEngine.Debug.LogError($"[NoteSpawner] Auto-generation produced no notes for '{songClip.name}'.");
                    chartLoadFailed = true;
                    return;
                }
            }

            if (chart == null || conductor == null || !conductor.SongHasStarted)
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
            Color laneColor = data.lane >= 0 && data.lane < laneColors.Length
                ? laneColors[data.lane]
                : Color.white;

            GameObject noteObject = notePrefab != null
                ? Instantiate(notePrefab)
                : CreatePlaceholderNoteObject(data, laneColor);

            Note note = noteObject.GetComponent<Note>();
            if (note == null)
            {
                note = noteObject.AddComponent<Note>();
            }

            note.Initialize(data, spawnPosition, strumlinePosition, laneColor);
        }

        private GameObject CreatePlaceholderNoteObject(NoteData data, Color laneColor)
        {
            GameObject noteObject = new GameObject($"Note_lane{data.lane}_t{data.time:F2}");

            SpriteRenderer renderer = noteObject.AddComponent<SpriteRenderer>();

            if (arrowSprite != null)
            {
                renderer.sprite = arrowSprite;
                renderer.color = laneColor;
                noteObject.transform.rotation = Quaternion.Euler(0f, 0f, GetLaneArrowRotationZ(data.lane));
            }
            else
            {
                renderer.sprite = PlaceholderAssetFactory.CreateSolidSprite(laneColor);
            }

            noteObject.transform.localScale = Vector3.one * noteSize;

            return noteObject;
        }

        private static float GetLaneArrowRotationZ(int lane)
        {
            float[] rotations = PlaceholderAssetFactory.LaneArrowRotationsZ;
            return lane >= 0 && lane < rotations.Length ? rotations[lane] : 0f;
        }

        private float GetLaneX(int lane)
        {
            float totalWidth = (LaneCount - 1) * laneSpacing;
            float startX = -totalWidth / 2f;
            return startX + lane * laneSpacing;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using FunkyThursday.Core;
using FunkyThursday.Gameplay;

namespace FunkyThursday.Debug
{
    /// <summary>
    /// Minimal test rig: drop into an empty scene to spawn 4 coloured
    /// receptor placeholders and prove the Conductor's audio/visual sync
    /// works before any real gameplay is built on top of it.
    /// Press Space to start the song.
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Assign a test music clip here in the Inspector.")]
        public AudioClip testClip;

        [Tooltip("BPM of the assigned test clip. Set this per-song instead of editing Conductor. Ignored if Auto Detect Bpm is on.")]
        public float songBpm = 110f;

        [Tooltip("If on, estimate BPM automatically from testClip's audio instead of using Song Bpm. " +
                 "Fixes long-song drift caused by a hand-guessed tempo being slightly wrong.")]
        public bool autoDetectBpm = false;

        [Tooltip("If on, build a full tempo map instead of a single BPM, so tempo changes mid-song " +
                 "(speed-ups/slow-downs) are tracked instead of breaking sync. Takes priority over Auto Detect Bpm.")]
        public bool autoDetectTempoMap = false;

        [Tooltip("Seconds to shift song position by, to compensate for audio latency or lead-in silence.")]
        public float songOffsetSeconds = 0f;

        [Header("Receptor Layout")]
        [Tooltip("Directional arrow sprite, drawn facing up. Rotated per lane at runtime (FNF-style: one " +
                 "sprite reused for all 4 lanes) and tinted with that lane's colour. If left unassigned, " +
                 "falls back to a plain coloured square.")]
        public Sprite arrowSprite;

        [Tooltip("Size in world units of each receptor square.")]
        public float receptorSize = 1f;

        [Tooltip("Gap in world units between receptor squares. Must match NoteSpawner.laneSpacing so notes line " +
                 "up with the receptors, and be bigger than the arrow sprite's world-space width or adjacent " +
                 "lanes will visually overlap.")]
        public float receptorSpacing = 2.8f;

        [Tooltip("Y position (world units) of the receptor row.")]
        public float receptorHeightY = 3.5f;

        private Conductor conductor;
        private bool songStarted;

        private void Start()
        {
            SpawnReceptors();
            SetUpConductor();
            SetUpScoreManager();
            SetUpInputHandler();
        }

        private void SpawnReceptors()
        {
            Color[] laneColors = PlaceholderAssetFactory.CreateLaneColors();
            int laneCount = laneColors.Length;
            float totalWidth = (laneCount - 1) * receptorSpacing;
            float startX = -totalWidth / 2f;

            for (int lane = 0; lane < laneCount; lane++)
            {
                GameObject receptor = new GameObject($"Receptor_{lane}");
                receptor.transform.SetParent(transform);
                receptor.transform.position = new Vector3(startX + lane * receptorSpacing, receptorHeightY, 0f);

                SpriteRenderer renderer = receptor.AddComponent<SpriteRenderer>();
                if (arrowSprite != null)
                {
                    renderer.sprite = arrowSprite;
                    renderer.color = laneColors[lane];
                    receptor.transform.rotation = Quaternion.Euler(0f, 0f, PlaceholderAssetFactory.LaneArrowRotationsZ[lane]);
                }
                else
                {
                    renderer.sprite = PlaceholderAssetFactory.CreateSolidSprite(laneColors[lane]);
                }
                receptor.transform.localScale = Vector3.one * receptorSize;

                BeatPulse pulse = receptor.AddComponent<BeatPulse>();
                pulse.target = receptor.transform;
            }
        }

        private void SetUpConductor()
        {
            GameObject conductorObject = new GameObject("Conductor");
            AudioSource audioSource = conductorObject.AddComponent<AudioSource>();
            audioSource.clip = testClip;
            audioSource.playOnAwake = false;

            float bpmToUse = songBpm;
            TempoMap tempoMap = null;

            if (autoDetectTempoMap && testClip != null)
            {
                tempoMap = AutoChartGenerator.DetectTempoMap(testClip);
                if (tempoMap != null && tempoMap.changePoints.Count > 0)
                {
                    bpmToUse = tempoMap.changePoints[0].bpm;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[TestSceneSetup] Tempo map detection failed; falling back to flat BPM.");
                }
            }

            if (tempoMap == null && autoDetectBpm && testClip != null)
            {
                float? detectedBpm = AutoChartGenerator.DetectBpm(testClip);
                if (detectedBpm.HasValue)
                {
                    bpmToUse = detectedBpm.Value;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[TestSceneSetup] BPM auto-detection failed; falling back to Song Bpm.");
                }
            }

            conductor = conductorObject.AddComponent<Conductor>();
            conductor.bpm = bpmToUse;
            conductor.tempoMap = tempoMap;
            conductor.songOffsetSeconds = songOffsetSeconds;
        }

        private void SetUpScoreManager()
        {
            if (ScoreManager.Instance != null)
            {
                return;
            }

            GameObject scoreObject = new GameObject("ScoreManager");
            scoreObject.AddComponent<ScoreManager>();
        }

        private void SetUpInputHandler()
        {
            if (FindFirstObjectByType<NoteInputHandler>() != null)
            {
                return;
            }

            GameObject inputObject = new GameObject("NoteInputHandler");
            inputObject.AddComponent<NoteInputHandler>();
        }

        private void Update()
        {
            if (!songStarted && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (Conductor.Instance == null)
                {
                    UnityEngine.Debug.LogError("[TestSceneSetup] No Conductor instance found.");
                    return;
                }

                Conductor.Instance.StartSong();
                songStarted = true;
                UnityEngine.Debug.Log("[TestSceneSetup] Song started.");
            }
        }
    }
}

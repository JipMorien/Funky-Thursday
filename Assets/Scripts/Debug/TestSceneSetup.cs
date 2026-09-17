using UnityEngine;
using UnityEngine.InputSystem;
using FunkyThursday.Core;

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

        [Tooltip("BPM of the assigned test clip. Set this per-song instead of editing Conductor.")]
        public float songBpm = 110f;

        [Tooltip("Seconds to shift song position by, to compensate for audio latency or lead-in silence.")]
        public float songOffsetSeconds = 0f;

        [Header("Receptor Layout")]
        [Tooltip("Size in world units of each receptor square.")]
        public float receptorSize = 1f;

        [Tooltip("Gap in world units between receptor squares.")]
        public float receptorSpacing = 1.5f;

        [Tooltip("Y position (world units) of the receptor row.")]
        public float receptorHeightY = 3.5f;

        private Conductor conductor;
        private bool songStarted;

        private void Start()
        {
            SpawnReceptors();
            SetUpConductor();
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
                renderer.sprite = PlaceholderAssetFactory.CreateSolidSprite(laneColors[lane]);
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

            conductor = conductorObject.AddComponent<Conductor>();
            conductor.bpm = songBpm;
            conductor.songOffsetSeconds = songOffsetSeconds;
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

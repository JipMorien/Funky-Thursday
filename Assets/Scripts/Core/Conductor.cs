using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Tracks song playback position using the audio DSP clock rather than
    /// Time.time, since Time.time drifts relative to the audio hardware clock
    /// and will slowly desync visuals from audio over a long song.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Conductor : MonoBehaviour
    {
        public static Conductor Instance { get; private set; }

        [Header("Song Settings")]
        [Tooltip("Beats per minute of the current song.")]
        public float bpm = 89f;

        [Tooltip("Seconds to shift song position by, to compensate for audio " +
                 "latency or lead-in silence. Positive delays the song, negative advances it.")]
        public float songOffsetSeconds = 0f;

        [Tooltip("Optional. If assigned, beat position follows this piecewise tempo instead of the flat bpm above - " +
                 "use for songs that speed up or slow down mid-track. See AutoChartGenerator.DetectTempoMap.")]
        public TempoMap tempoMap;

        [Header("Read-Only Status")]
        [SerializeField] private float songPositionInSeconds;
        [SerializeField] private float songPositionInBeats;
        [SerializeField] private bool songHasStarted;

        private AudioSource audioSource;
        private double dspSongStartTime;
        private float secPerBeat;

        public float SongPositionInSeconds => songPositionInSeconds;
        public float SongPositionInBeats => songPositionInBeats;
        public bool SongHasStarted => songHasStarted;
        public float ClipLengthSeconds => audioSource != null && audioSource.clip != null ? audioSource.clip.length : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            audioSource = GetComponent<AudioSource>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!songHasStarted)
            {
                return;
            }

            songPositionInSeconds = (float)(AudioSettings.dspTime - dspSongStartTime) - songOffsetSeconds;

            songPositionInBeats = (tempoMap != null && tempoMap.changePoints.Count > 0)
                ? tempoMap.TimeToBeats(songPositionInSeconds)
                : songPositionInSeconds / secPerBeat;
        }

        /// <summary>
        /// Schedules the assigned AudioSource's clip to start on the audio
        /// thread's clock, then anchors the conductor to that same clock so
        /// the reported song position lines up with what is actually audible.
        /// </summary>
        public void StartSong(float startDelaySeconds = 0f)
        {
            if (audioSource.clip == null)
            {
                UnityEngine.Debug.LogError("Conductor.StartSong called with no AudioClip assigned to the AudioSource.");
                return;
            }

            secPerBeat = 60f / bpm;

            double dspStartTime = AudioSettings.dspTime + startDelaySeconds;
            audioSource.PlayScheduled(dspStartTime);

            dspSongStartTime = dspStartTime;
            songHasStarted = true;
        }

        public void StopSong()
        {
            audioSource.Stop();
            songHasStarted = false;
            songPositionInSeconds = 0f;
            songPositionInBeats = 0f;
        }
    }
}

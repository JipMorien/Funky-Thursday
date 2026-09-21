using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Everything one playable level needs: which song, how to get its chart,
    /// and its difficulty tuning. LevelSelectController lists every instance
    /// of this found under Resources/Levels; GameplaySessionController reads
    /// whichever one GameFlowManager was told to start.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "FunkyThursday/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Display")]
        public string levelName = "New Level";
        [TextArea] public string description;

        [Header("Song")]
        public AudioClip songClip;
        [Tooltip("Used only if both auto-detect options below are off.")]
        public float bpm = 100f;
        public float songOffsetSeconds = 0f;

        [Header("Tempo Detection")]
        [Tooltip("If on, estimate BPM automatically from songClip's audio instead of using Bpm above.")]
        public bool autoDetectBpm = true;
        [Tooltip("If on, build a full tempo map instead of a single BPM, for songs that speed up/slow down. Takes priority over Auto Detect Bpm.")]
        public bool autoDetectTempoMap = false;

        [Header("Chart Source")]
        [Tooltip("If set, load this hand-authored chart (Resources-relative path, no extension) instead of auto-generating one.")]
        public string chartResourcePath;
        [Tooltip("Used to auto-generate the chart when Chart Resource Path is empty. Vary these per level to create difficulty tiers from the same song.")]
        public AutoChartSettings autoChartSettings = AutoChartSettings.Default;

        [Header("Feel")]
        [Tooltip("Seconds a note is visible/travelling before its hit time. Lower feels faster/harder.")]
        public float noteTravelLeadTime = 2f;
    }
}

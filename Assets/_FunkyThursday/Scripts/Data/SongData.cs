using System;
using FunkyThursday.Charts;
using FunkyThursday.Visuals;
using UnityEngine;

namespace FunkyThursday.Data
{
    /// <summary>
    /// Everything one level needs, in one asset: chart, audio, opponent art and placeholder colours,
    /// and backdrop. The menus read it for the Level Select, and GameplayController plays it.
    /// </summary>
    [CreateAssetMenu(fileName = "New Song", menuName = "Funky Thursday/Song Data")]
    public sealed class SongData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save-file key, e.g. graveyard-gatekeeper. Don't change it after release.")]
        public string id;
        public string displayName;
        public string opponentName;
        public string difficulty;
        [Min(1)] public int level = 1;

        [Header("Song")]
        public TextAsset chart;
        public AudioClip instrumental;
        public AudioClip vocals;
        [Tooltip("Where the Level Select preview starts, in seconds.")]
        [Min(0f)] public float previewStart = 10f;

        [Header("Opponent")]
        public Color opponentCloak = new Color32(0x5A, 0x14, 0x24, 0xFF);
        public Color opponentEyes = new Color32(0xFF, 0x4A, 0x3D, 0xFF);
        public CharacterPuppet.PoseAnimations opponentPoses = new CharacterPuppet.PoseAnimations();

        [Header("Stage")]
        public SpriteAnimation backdrop;
        public Color backdropTint = Color.white;
        [Tooltip("Path inside StreamingAssets, e.g. Video/bg_crypt-keeper.mp4.")]
        public string backdropVideo;

        [NonSerialized] Chart _chart;

        /// <summary>Parsed chart, cached after the first call. Throws ChartFormatException on bad data.</summary>
        public Chart LoadChart()
        {
            if (_chart == null) _chart = ChartLoader.Load(chart);
            return _chart;
        }

        /// <summary>BPM without throwing; 0 if the chart can't be read.</summary>
        public float Bpm
        {
            get
            {
                try
                {
                    return chart != null ? LoadChart().Bpm : 0f;
                }
                catch (ChartFormatException)
                {
                    return 0f;
                }
            }
        }

        void OnValidate() => _chart = null;
    }
}

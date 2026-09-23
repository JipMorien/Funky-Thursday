using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Visuals
{
    /// <summary>
    /// Plays a SpriteAnimation on a SpriteRenderer. With Beats Per Loop above zero the loop is locked
    /// to the Conductor instead of the clock, so background motion stays on the beat and freezes on pause.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteAnimation clip;
        [SerializeField] Conductor conductor;

        [Tooltip("0 = play at the clip's own fps. Otherwise one loop lasts this many beats, locked to the song.")]
        [SerializeField, Min(0f)] float beatsPerLoop;

        SpriteRenderer _renderer;
        float _time;

        public SpriteAnimation Clip => clip;
        public bool IsPlaying => SpriteAnimation.IsUsable(clip);

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Play(SpriteAnimation newClip, bool restart = true)
        {
            clip = newClip;
            if (restart) _time = 0f;
            Apply();
        }

        /// <summary>Stops animating; the renderer keeps whatever sprite it has.</summary>
        public void Stop() => clip = null;

        void Update()
        {
            if (!IsPlaying) return;
            _time += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (!IsPlaying || _renderer == null) return;

            bool beatLocked = beatsPerLoop > 0f && conductor != null && conductor.State != Conductor.PlaybackState.Stopped;
            _renderer.sprite = beatLocked
                ? clip.SampleNormalized((float)(conductor.SongPositionInBeats / beatsPerLoop))
                : clip.Sample(_time);
        }
    }
}

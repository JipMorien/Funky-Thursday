using System;
using FunkyThursday.Core;
using FunkyThursday.Placeholders;
using UnityEngine;

namespace FunkyThursday.Visuals
{
    /// <summary>
    /// Drives one singer: an idle loop locked to the beat, a sing animation per lane (held for
    /// sustains) and a miss reaction. Uses Higgsfield animations when assigned; anything missing falls
    /// back to the procedural wraith, which fakes poses with a nudge and bops by squashing.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterPuppet : MonoBehaviour
    {
        [Serializable]
        public sealed class PoseAnimations
        {
            public SpriteAnimation idle;
            public SpriteAnimation left;
            public SpriteAnimation down;
            public SpriteAnimation up;
            public SpriteAnimation right;
            public SpriteAnimation miss;

            [Tooltip("Health-bar head icons (32×32 from higgs_import.py base).")]
            public Sprite icon;
            public Sprite iconLosing;

            public SpriteAnimation ForLane(NoteLane lane)
            {
                switch (lane)
                {
                    case NoteLane.Left: return left;
                    case NoteLane.Down: return down;
                    case NoteLane.Up: return up;
                    default: return right;
                }
            }

            public bool HasIdle => SpriteAnimation.IsUsable(idle);
        }

        const float BopSeconds = 0.12f;
        const float MinPoseSeconds = 0.3f;
        const float MissSeconds = 0.35f;
        static readonly Color MissTint = new Color(0.45f, 0.4f, 0.6f);

        [Header("Higgsfield animations")]
        [SerializeField] PoseAnimations poses = new PoseAnimations();
        [SerializeField] Conductor conductor;
        [Tooltip("Beats per idle loop. The idle is locked to the song so the character dances on the beat.")]
        [SerializeField, Min(0.25f)] float idleBeatsPerLoop = 2f;
        [SerializeField, Min(0.1f)] float artScale = 1f;
        [SerializeField] bool flipX;

        [Header("Placeholder look (used when no idle animation is assigned)")]
        [SerializeField] Color cloak = new Color32(0x2B, 0x2F, 0x5E, 0xFF);
        [SerializeField] Color eyes = new Color32(0x7F, 0xE7, 0xFF, 0xFF);
        [SerializeField, Min(0.1f)] float scale = 3f;
        [SerializeField, Min(0f)] float poseNudge = 0.25f;
        [SerializeField, Min(1)] int bopEveryBeats = 1;

        SpriteRenderer _renderer;
        Vector3 _home;
        NoteLane _pose;
        float _poseElapsed;
        float _bopTimer;
        float _singTimer;
        float _missTimer;

        public Color Cloak => cloak;
        public Color Eyes => eyes;
        public bool UsesArt => poses.HasIdle;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _home = transform.localPosition;
            Apply();
        }

        public void SetPlaceholderColors(Color newCloak, Color newEyes)
        {
            cloak = newCloak;
            eyes = newEyes;
            Apply();
        }

        /// <summary>Swaps the whole animation set, e.g. a new opponent per level. Null = placeholder.</summary>
        public void SetPoses(PoseAnimations newPoses)
        {
            poses = newPoses ?? new PoseAnimations();
            ResetPose();
        }

        public Sprite GetIcon(bool losing)
        {
            Sprite art = losing ? (poses.iconLosing != null ? poses.iconLosing : poses.icon) : poses.icon;
            return art != null ? art : PixelArtFactory.GetWraithIcon(cloak, eyes, losing);
        }

        public void Bop(int beat)
        {
            if (beat < 0 || beat % bopEveryBeats != 0 || _singTimer > 0f) return;
            _bopTimer = BopSeconds;
        }

        public void Sing(NoteLane lane, double holdSeconds)
        {
            _pose = lane;
            _poseElapsed = 0f;
            _singTimer = Mathf.Max(MinPoseSeconds, (float)holdSeconds + 0.1f);
            _missTimer = 0f;
        }

        public void Miss(NoteLane lane)
        {
            _pose = lane;
            _poseElapsed = 0f;
            _missTimer = MissSeconds;
            _singTimer = MissSeconds;
        }

        public void ResetPose()
        {
            _bopTimer = _singTimer = _missTimer = _poseElapsed = 0f;
            Apply();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _poseElapsed += dt;
            _bopTimer = Mathf.Max(0f, _bopTimer - dt);
            _singTimer = Mathf.Max(0f, _singTimer - dt);
            _missTimer = Mathf.Max(0f, _missTimer - dt);
            Apply();
        }

        void Apply()
        {
            if (_renderer == null) return;

            bool singing = _singTimer > 0f;
            bool missing = _missTimer > 0f;
            bool art = poses.HasIdle;

            SpriteAnimation action = missing && SpriteAnimation.IsUsable(poses.miss) ? poses.miss
                : singing && SpriteAnimation.IsUsable(poses.ForLane(_pose)) ? poses.ForLane(_pose)
                : null;

            if (action != null) _renderer.sprite = action.Sample(_poseElapsed);
            else if (art) _renderer.sprite = SampleIdle();
            else _renderer.sprite = PixelArtFactory.GetWraith(cloak, eyes);

            _renderer.flipX = flipX;

            // Fake whatever the art doesn't cover: a nudge for missing sing poses, a tint for a missing miss.
            Vector2 nudge = singing && !missing && action == null ? Lanes.Direction(_pose) * poseNudge : Vector2.zero;
            transform.localPosition = _home + (Vector3)nudge;
            _renderer.color = missing && action == null ? Color.Lerp(Color.white, MissTint, _missTimer / MissSeconds) : Color.white;

            float baseScale = art ? artScale : scale;
            float squash = art ? 0f : _bopTimer / BopSeconds;
            transform.localScale = new Vector3(baseScale * (1f + 0.06f * squash), baseScale * (1f - 0.08f * squash), 1f);
        }

        Sprite SampleIdle()
        {
            bool beatLocked = conductor != null && conductor.State != Conductor.PlaybackState.Stopped;
            return beatLocked
                ? poses.idle.SampleNormalized((float)(conductor.SongPositionInBeats / idleBeatsPerLoop))
                : poses.idle.Sample(Time.time);
        }
    }
}

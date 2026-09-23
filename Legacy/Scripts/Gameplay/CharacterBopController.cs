using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Procedurally animates a stage character's Transform (squash/stretch/
    /// lean, no sprite-swapping) - no Animator/AnimationClip assets needed,
    /// the same code-driven approach BeatPulse already uses for receptors.
    /// Idles with a beat-synced bop (derived straight from
    /// Conductor.SongPositionInBeats, so it never drifts out of time), and
    /// punches into a short directional "sing" pose or a "miss" wobble on
    /// demand, then eases back to idle on its own.
    /// Only one character on stage should be marked Is Player Controlled -
    /// that's the one NoteInputHandler and Note react to on the player's own
    /// hits/misses, via the static PlayerInstance.
    /// </summary>
    public class CharacterBopController : MonoBehaviour
    {
        [Tooltip("True for the character that should react to the player's own hits/misses (found via PlayerInstance). False for cosmetic/opponent characters that just idle-bop.")]
        public bool isPlayerControlled;

        [Header("Idle Bop")]
        [Range(0f, 0.5f)] public float idleSquash = 0.12f;

        [Header("Sing Pose")]
        public float singPoseDuration = 0.25f;
        public float singLeanDegrees = 22f;
        public float singShiftUnits = 0.15f;
        [Range(0f, 0.6f)] public float singSquashAmount = 0.3f;

        [Header("Miss Pose")]
        public float missPoseDuration = 0.3f;
        public float missWobbleDegrees = 10f;
        [Range(0f, 0.5f)] public float missSquashAmount = 0.1f;

        /// <summary>The stage's player-controlled character, if one exists. Null-safe to call into from input code.</summary>
        public static CharacterBopController PlayerInstance { get; private set; }

        private enum Pose { Idle, SingLeft, SingDown, SingUp, SingRight, Miss }

        private Vector3 baseScale;
        private Vector3 baseLocalPosition;
        private Pose currentPose = Pose.Idle;
        private float poseTimer;
        private float poseDuration;

        private void Awake()
        {
            baseScale = transform.localScale;
            baseLocalPosition = transform.localPosition;

            if (isPlayerControlled)
            {
                PlayerInstance = this;
            }
        }

        private void OnDestroy()
        {
            if (PlayerInstance == this)
            {
                PlayerInstance = null;
            }
        }

        /// <summary>Plays the directional sing pose for a lane (0=left, 1=down, 2=up, 3=right). Returns to idle automatically.</summary>
        public void PlaySing(int lane)
        {
            Pose pose = lane switch
            {
                0 => Pose.SingLeft,
                1 => Pose.SingDown,
                2 => Pose.SingUp,
                3 => Pose.SingRight,
                _ => Pose.Idle,
            };

            if (pose == Pose.Idle)
            {
                return;
            }

            StartPose(pose, singPoseDuration);
        }

        /// <summary>Plays a quick miss wobble. Returns to idle automatically.</summary>
        public void PlayMiss()
        {
            StartPose(Pose.Miss, missPoseDuration);
        }

        private void StartPose(Pose pose, float duration)
        {
            currentPose = pose;
            poseTimer = 0f;
            poseDuration = duration;
        }

        private void Update()
        {
            if (currentPose != Pose.Idle)
            {
                poseTimer += Time.deltaTime;
                float t = poseDuration > 0f ? Mathf.Clamp01(poseTimer / poseDuration) : 1f;
                ApplyPose(currentPose, t);

                if (poseTimer >= poseDuration)
                {
                    currentPose = Pose.Idle;
                }
                return;
            }

            ApplyIdle();
        }

        private void ApplyIdle()
        {
            float beatFraction = 0f;
            Conductor conductor = Conductor.Instance;
            if (conductor != null && conductor.SongHasStarted)
            {
                float beats = conductor.SongPositionInBeats;
                beatFraction = beats - Mathf.Floor(beats);
            }

            // Snap-squash right on the beat, ease back out over the rest of it - the classic FNF bop.
            float squash = (1f - beatFraction) * (1f - beatFraction) * idleSquash;

            transform.localScale = new Vector3(baseScale.x * (1f + squash * 0.5f), baseScale.y * (1f - squash), baseScale.z);
            transform.localPosition = baseLocalPosition;
            transform.localEulerAngles = Vector3.zero;
        }

        private void ApplyPose(Pose pose, float t)
        {
            // A single sine hump: 0 at the start and end of the pose, peaking at its midpoint.
            float hump = Mathf.Sin(t * Mathf.PI);

            Vector3 scale = baseScale;
            Vector3 offset = Vector3.zero;
            float rotationZ = 0f;

            switch (pose)
            {
                case Pose.SingLeft:
                    rotationZ = singLeanDegrees * hump;
                    offset.x = -singShiftUnits * hump;
                    break;
                case Pose.SingRight:
                    rotationZ = -singLeanDegrees * hump;
                    offset.x = singShiftUnits * hump;
                    break;
                case Pose.SingDown:
                    scale.y = baseScale.y * (1f - singSquashAmount * hump);
                    scale.x = baseScale.x * (1f + singSquashAmount * 0.5f * hump);
                    offset.y = -singShiftUnits * hump;
                    break;
                case Pose.SingUp:
                    scale.y = baseScale.y * (1f + singSquashAmount * hump);
                    scale.x = baseScale.x * (1f - singSquashAmount * 0.5f * hump);
                    offset.y = singShiftUnits * hump;
                    break;
                case Pose.Miss:
                    rotationZ = missWobbleDegrees * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
                    scale.y = baseScale.y * (1f - missSquashAmount * hump);
                    scale.x = baseScale.x * (1f + missSquashAmount * 0.5f * hump);
                    break;
            }

            transform.localScale = scale;
            transform.localPosition = baseLocalPosition + offset;
            transform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
        }
    }
}

using UnityEngine;
using FunkyThursday.Core;
using FunkyThursday.Debug;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Flashes a receptor white and punches its scale for a moment whenever
    /// its lane is hit, so a hit reads as a distinct, snappier pop layered on
    /// top of BeatPulse's constant beat pulse. The scale punch is delegated
    /// to this receptor's own BeatPulse (if present) since that script
    /// already owns and decays this Transform's localScale each frame -
    /// writing to it from here too would just fight BeatPulse for control.
    /// Listens for NoteInputHandler.OnLaneHit and filters to its own lane.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ReceptorHitFlash : MonoBehaviour
    {
        [Tooltip("Which lane (0=left, 1=down, 2=up, 3=right) this receptor belongs to.")]
        public int lane;

        [Tooltip("Scale multiplier applied at the instant of a hit (via this receptor's BeatPulse, if present).")]
        public float punchScale = 1.4f;

        [Tooltip("How quickly the flash decays back to normal.")]
        public float flashDecaySpeed = 10f;

        private SpriteRenderer spriteRenderer;
        private BeatPulse beatPulse;
        private Color baseColor;
        private float flashAmount;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            beatPulse = GetComponent<BeatPulse>();
            baseColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            NoteInputHandler.OnLaneHit += HandleLaneHit;
        }

        private void OnDisable()
        {
            NoteInputHandler.OnLaneHit -= HandleLaneHit;
        }

        private void HandleLaneHit(int hitLane, HitJudgment judgment)
        {
            if (hitLane != lane || judgment == HitJudgment.Miss)
            {
                return;
            }

            beatPulse?.Punch(punchScale);
            flashAmount = 1f;
        }

        private void Update()
        {
            flashAmount = Mathf.MoveTowards(flashAmount, 0f, Time.deltaTime * flashDecaySpeed);
            spriteRenderer.color = Color.Lerp(baseColor, Color.white, flashAmount);
        }
    }
}

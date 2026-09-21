using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.Debug
{
    /// <summary>
    /// Visual metronome used to verify the Conductor's beat timing by eye:
    /// pulses a Transform on every new integer beat and logs the beat number.
    /// </summary>
    public class BeatPulse : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform to pulse. Defaults to this GameObject's transform if left empty.")]
        public Transform target;

        [Header("Pulse Settings")]
        [Tooltip("Scale multiplier applied at the moment a beat lands.")]
        public float pulseScale = 1.2f;

        [Tooltip("How quickly the pulse scales back down to its resting scale.")]
        public float returnSpeed = 8f;

        private Vector3 baseScale;
        private int lastBeat = -1;

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }

            baseScale = target.localScale;
        }

        private void Update()
        {
            Conductor conductor = Conductor.Instance;
            if (conductor == null || !conductor.SongHasStarted)
            {
                return;
            }

            int currentBeat = Mathf.FloorToInt(conductor.SongPositionInBeats);

            if (currentBeat != lastBeat)
            {
                lastBeat = currentBeat;
                target.localScale = baseScale * pulseScale;
                UnityEngine.Debug.Log($"[BeatPulse] Beat {currentBeat}");
            }

            target.localScale = Vector3.Lerp(target.localScale, baseScale, Time.deltaTime * returnSpeed);
        }

        /// <summary>Immediately scales up by the given multiplier of the resting scale; decays back via the normal per-frame Lerp. Lets other systems (e.g. a note hit) punch the same Transform this script already owns, without fighting over localScale.</summary>
        public void Punch(float scaleMultiplier)
        {
            target.localScale = baseScale * scaleMultiplier;
        }
    }
}

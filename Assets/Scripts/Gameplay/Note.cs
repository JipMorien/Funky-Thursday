using System.Collections.Generic;
using UnityEngine;
using FunkyThursday.Core;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// A single scrolling note. Its position each frame is derived directly
    /// from Conductor.SongPositionInSeconds rather than accumulated with
    /// Time.deltaTime, for the same reason Conductor itself uses dspTime:
    /// deriving position straight from the audio clock means the note lands
    /// on the strumline exactly on time regardless of frame-rate hitches,
    /// instead of drifting the way a fixed speed * deltaTime walk could.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Note : MonoBehaviour
    {
        [Tooltip("Seconds of timing error, before or after the note's target time, that still counts as " +
                 "hittable at all (the loosest 'Okay' judgment tier). Also how long past its target time " +
                 "an unhit note lingers before it self-destroys as a miss.")]
        public float hitWindow = 0.15f;

        [Tooltip("Timing error (seconds) at or under which a hit counts as Perfect. Must be <= goodWindow.")]
        public float perfectWindow = 0.05f;

        [Tooltip("Timing error (seconds) at or under which a hit counts as Good, if not already Perfect. Must be <= hitWindow.")]
        public float goodWindow = 0.10f;

        public NoteData Data { get; private set; }

        private static readonly List<Note> activeNotes = new List<Note>();

        /// <summary>All notes currently spawned and not yet hit or despawned.</summary>
        public static IReadOnlyList<Note> ActiveNotes => activeNotes;

        private Vector3 spawnPosition;
        private Vector3 strumlinePosition;
        private float spawnSongTime;
        private float targetSongTime;
        private bool initialized;
        private bool wasHit;

        /// <summary>
        /// Sets up the note's data and travel path. Must be called immediately
        /// after creation/instantiation, before this component's next Update.
        /// If data.sustainLength is greater than zero, a stretched "tail" is
        /// spawned trailing the note, sized in world space to match how far
        /// the note travels during that hold duration - the same idea as a
        /// long tile in Piano Tiles, for sounds too long to time as a single tap.
        /// </summary>
        public void Initialize(NoteData data, Vector3 spawnPosition, Vector3 strumlinePosition, Color color)
        {
            Data = data;
            this.spawnPosition = spawnPosition;
            this.strumlinePosition = strumlinePosition;
            targetSongTime = data.time;

            Conductor conductor = Conductor.Instance;
            spawnSongTime = conductor != null ? conductor.SongPositionInSeconds : targetSongTime;

            transform.position = spawnPosition;
            initialized = true;

            activeNotes.Add(this);

            if (data.sustainLength > 0f)
            {
                CreateSustainTail(color);
            }
        }

        private void CreateSustainTail(Color color)
        {
            float totalTravelTime = targetSongTime - spawnSongTime;
            if (totalTravelTime <= 0f)
            {
                return;
            }

            float totalDistance = Vector3.Distance(spawnPosition, strumlinePosition);
            float speed = totalDistance / totalTravelTime;
            float tailWorldLength = Data.sustainLength * speed;
            if (tailWorldLength <= 0f)
            {
                return;
            }

            float parentScale = transform.localScale.y;
            if (Mathf.Approximately(parentScale, 0f))
            {
                return;
            }

            Vector3 travelDirection = (strumlinePosition - spawnPosition).normalized;

            GameObject tail = new GameObject("SustainTail");
            tail.transform.SetParent(transform, false);

            SpriteRenderer tailRenderer = tail.AddComponent<SpriteRenderer>();
            tailRenderer.sprite = PlaceholderAssetFactory.CreateSolidSprite(color);
            tailRenderer.color = new Color(color.r, color.g, color.b, 0.55f);
            tailRenderer.sortingOrder = -1;

            // Local scale/position are relative to this note's own (possibly non-1) scale,
            // so divide the desired world length back out to compensate.
            tail.transform.localScale = new Vector3(1f, tailWorldLength / parentScale, 1f);

            // Trail behind the head, on the side it approached from, so the tail
            // is still scrolling through the strumline for the rest of the hold.
            Vector3 worldOffset = -travelDirection * (tailWorldLength * 0.5f);
            tail.transform.localPosition = transform.InverseTransformVector(worldOffset);
        }

        /// <summary>
        /// Classifies a timing error (seconds, already known to be within
        /// hitWindow) into a judgment tier - tightest to loosest.
        /// </summary>
        public HitJudgment GetJudgment(float absDelta)
        {
            if (absDelta <= perfectWindow)
            {
                return HitJudgment.Perfect;
            }

            if (absDelta <= goodWindow)
            {
                return HitJudgment.Good;
            }

            return HitJudgment.Okay;
        }

        /// <summary>
        /// Called by the input handler when this note was successfully hit
        /// within its hit window. Despawns it without counting as a miss.
        /// </summary>
        public void Hit()
        {
            if (wasHit)
            {
                return;
            }

            wasHit = true;
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            Conductor conductor = Conductor.Instance;
            if (conductor == null)
            {
                return;
            }

            float songPosition = conductor.SongPositionInSeconds;
            float totalTravelTime = targetSongTime - spawnSongTime;

            float t = totalTravelTime > 0f
                ? (songPosition - spawnSongTime) / totalTravelTime
                : 1f;

            transform.position = Vector3.LerpUnclamped(spawnPosition, strumlinePosition, t);

            // For a sustain note, don't despawn until the whole hold has had a chance
            // to scroll through the strumline - otherwise the tail gets cut off right
            // as the head arrives, instead of being visible for its full duration.
            float missDeadline = targetSongTime + Data.sustainLength + hitWindow;
            if (songPosition >= missDeadline)
            {
                // Opponent/CPU notes always land - only the player's own notes can be missed.
                if (Data.isPlayerNote)
                {
                    ScoreManager.Instance?.RegisterHit(HitJudgment.Miss);
                    HealthManager.Instance?.ApplyJudgment(HitJudgment.Miss);
                    UnityEngine.Debug.Log($"[Note] Missed lane {Data.lane} note at {targetSongTime:F2}s");
                }
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            activeNotes.Remove(this);
        }
    }
}

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
        [Tooltip("How long after its target time the note is allowed to linger before it self-destroys.")]
        public float missDespawnWindow = 0.15f;

        public NoteData Data { get; private set; }

        private Vector3 spawnPosition;
        private Vector3 strumlinePosition;
        private float spawnSongTime;
        private float targetSongTime;
        private bool initialized;

        /// <summary>
        /// Sets up the note's data and travel path. Must be called immediately
        /// after creation/instantiation, before this component's next Update.
        /// </summary>
        public void Initialize(NoteData data, Vector3 spawnPosition, Vector3 strumlinePosition)
        {
            Data = data;
            this.spawnPosition = spawnPosition;
            this.strumlinePosition = strumlinePosition;
            targetSongTime = data.time;

            Conductor conductor = Conductor.Instance;
            spawnSongTime = conductor != null ? conductor.SongPositionInSeconds : targetSongTime;

            transform.position = spawnPosition;
            initialized = true;
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

            if (songPosition >= targetSongTime + missDespawnWindow)
            {
                Destroy(gameObject);
            }
        }
    }
}

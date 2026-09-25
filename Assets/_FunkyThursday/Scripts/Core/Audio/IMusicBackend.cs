using UnityEngine;

namespace FunkyThursday.Core.Audio
{
    /// <summary>Which audio engine the Conductor plays the song through (FMOD spike: compare the two).</summary>
    public enum MusicBackendKind
    {
        Unity,
        Fmod
    }

    /// <summary>
    /// Everything the Conductor needs from an audio engine: a raw audio clock, sample-accurate scheduled
    /// start of the instrumental and vocal track, pause/resume, and vocal muting. The Conductor keeps
    /// its own smoothing and beat logic on top, so both engines are timed by identical code.
    /// </summary>
    public interface IMusicBackend
    {
        string Name { get; }

        /// <summary>The engine's audio clock in seconds. Advances once per mix block, not per frame.</summary>
        double Clock { get; }

        /// <summary>Loads both tracks. Returns false (and logs why) when they can't be played.</summary>
        bool Load(AudioClip instrumental, AudioClip vocals, string songId, out double lengthSeconds);

        /// <summary>Starts both tracks from the top so the first sample is heard at <paramref name="clockTime"/>.</summary>
        void PlayAt(double clockTime);

        void Pause();

        /// <summary>Resumes after <see cref="Pause"/>, re-aligning the vocals to the instrumental.</summary>
        void Unpause();

        void Stop();

        /// <summary>Instrumental playback position in seconds (used to resync after a pause).</summary>
        double PlaybackPosition { get; }

        void SetVocalsMuted(bool muted);

        void SetVolume(float volume);

        void Release();
    }
}

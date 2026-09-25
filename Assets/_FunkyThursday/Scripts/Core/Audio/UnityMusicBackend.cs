using UnityEngine;

namespace FunkyThursday.Core.Audio
{
    /// <summary>The original playback path: two AudioSources scheduled on AudioSettings.dspTime.</summary>
    public sealed class UnityMusicBackend : IMusicBackend
    {
        readonly AudioSource _instrumental;
        readonly AudioSource _vocals;
        bool _hasVocals;

        public UnityMusicBackend(AudioSource instrumental, AudioSource vocals)
        {
            _instrumental = instrumental;
            _vocals = vocals;
            Configure(_instrumental);
            Configure(_vocals);
        }

        public string Name => "Unity";

        public double Clock => AudioSettings.dspTime;

        public double PlaybackPosition =>
            _instrumental.clip != null ? (double)_instrumental.timeSamples / _instrumental.clip.frequency : 0.0;

        public bool Load(AudioClip instrumental, AudioClip vocals, string songId, out double lengthSeconds)
        {
            lengthSeconds = instrumental != null ? instrumental.length : 0.0;
            if (instrumental == null) return false;

            _instrumental.clip = instrumental;
            _hasVocals = vocals != null;
            _vocals.clip = vocals;
            _vocals.mute = false;
            return true;
        }

        public void PlayAt(double clockTime)
        {
            _instrumental.PlayScheduled(clockTime);
            if (_hasVocals) _vocals.PlayScheduled(clockTime);
        }

        public void Pause()
        {
            _instrumental.Pause();
            if (_hasVocals) _vocals.Pause();
        }

        public void Unpause()
        {
            if (_hasVocals)
            {
                _vocals.timeSamples = Mathf.Min(_instrumental.timeSamples, _vocals.clip.samples - 1);
                _vocals.UnPause();
            }
            _instrumental.UnPause();
        }

        public void Stop()
        {
            _instrumental.Stop();
            _vocals.Stop();
        }

        public void SetVocalsMuted(bool muted) => _vocals.mute = muted;

        public void SetVolume(float volume)
        {
            // AudioListener.volume (GameSettings.ApplyAudio) already scales Unity audio.
        }

        public void Release() => Stop();

        static void Configure(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
        }
    }
}

#if FT_FMOD
using System.IO;
using UnityEngine;

namespace FunkyThursday.Core.Audio
{
    /// <summary>
    /// FMOD spike: plays the same Inst/Voices .ogg files through the FMOD Core API (no FMOD Studio
    /// project or banks needed). Tracks are streamed from StreamingAssets/FmodSpike/&lt;song id&gt;/,
    /// because FMOD can't read Unity AudioClip assets; run Funky Thursday → FMOD Spike → Copy Songs
    /// To StreamingAssets first. Both channels start on the same master DSP clock tick via setDelay.
    /// Only compiled when the FT_FMOD scripting define is set (after importing FMOD for Unity).
    /// </summary>
    public sealed class FmodMusicBackend : IMusicBackend
    {
        public const string StreamingFolder = "FmodSpike";

        readonly FMOD.System _core;
        readonly FMOD.ChannelGroup _master;
        readonly int _sampleRate;

        FMOD.Sound _instrumentalSound;
        FMOD.Sound _vocalsSound;
        FMOD.Channel _instrumental;
        FMOD.Channel _vocals;
        bool _hasVocals;
        bool _vocalsMuted;
        float _volume = 1f;
        float _instrumentalFrequency = 44100f;

        public FmodMusicBackend()
        {
            _core = FMODUnity.RuntimeManager.CoreSystem;
            Check(_core.getMasterChannelGroup(out _master), "getMasterChannelGroup");
            Check(_core.getSoftwareFormat(out _sampleRate, out _, out _), "getSoftwareFormat");
            if (_sampleRate <= 0) _sampleRate = 48000;
        }

        public string Name => "FMOD";

        public double Clock
        {
            get
            {
                _master.getDSPClock(out ulong clock, out _);
                return (double)clock / _sampleRate;
            }
        }

        public double PlaybackPosition
        {
            get
            {
                if (!_instrumental.hasHandle()) return 0.0;
                _instrumental.getPosition(out uint samples, FMOD.TIMEUNIT.PCM);
                return samples / (double)_instrumentalFrequency;
            }
        }

        public bool Load(AudioClip instrumental, AudioClip vocals, string songId, out double lengthSeconds)
        {
            lengthSeconds = 0.0;
            ReleaseSounds();

            string folder = Path.Combine(Application.streamingAssetsPath, StreamingFolder, songId ?? "");
            string instPath = Path.Combine(folder, "Inst.ogg");
            string vocalsPath = Path.Combine(folder, "Voices.ogg");

            if (!File.Exists(instPath))
            {
                Debug.LogError($"FMOD spike: {instPath} not found. Run Funky Thursday → FMOD Spike → Copy Songs To StreamingAssets.");
                return false;
            }

            const FMOD.MODE mode = FMOD.MODE.CREATESTREAM | FMOD.MODE._2D | FMOD.MODE.LOOP_OFF | FMOD.MODE.ACCURATETIME;
            if (!Check(_core.createSound(instPath, mode, out _instrumentalSound), "createSound Inst")) return false;

            _instrumentalSound.getLength(out uint lengthMs, FMOD.TIMEUNIT.MS);
            _instrumentalSound.getDefaults(out _instrumentalFrequency, out _);
            if (_instrumentalFrequency <= 0f) _instrumentalFrequency = 44100f;
            lengthSeconds = lengthMs / 1000.0;

            _hasVocals = File.Exists(vocalsPath) && Check(_core.createSound(vocalsPath, mode, out _vocalsSound), "createSound Voices");
            _vocalsMuted = false;
            return true;
        }

        public void PlayAt(double clockTime)
        {
            ulong start = (ulong)System.Math.Max(0.0, clockTime * _sampleRate);

            // Start paused, set the delay, then unpause, so both streams begin on the exact same tick.
            Check(_core.playSound(_instrumentalSound, _master, true, out _instrumental), "playSound Inst");
            _instrumental.setDelay(start, 0, false);
            _instrumental.setVolume(_volume);

            if (_hasVocals)
            {
                Check(_core.playSound(_vocalsSound, _master, true, out _vocals), "playSound Voices");
                _vocals.setDelay(start, 0, false);
                _vocals.setVolume(_volume);
                _vocals.setMute(_vocalsMuted);
                _vocals.setPaused(false);
            }
            _instrumental.setPaused(false);
        }

        public void Pause()
        {
            if (_instrumental.hasHandle()) _instrumental.setPaused(true);
            if (_vocals.hasHandle()) _vocals.setPaused(true);
        }

        public void Unpause()
        {
            if (_hasVocals && _vocals.hasHandle())
            {
                _instrumental.getPosition(out uint position, FMOD.TIMEUNIT.PCM);
                _vocals.setPosition(position, FMOD.TIMEUNIT.PCM);
                _vocals.setPaused(false);
            }
            if (_instrumental.hasHandle()) _instrumental.setPaused(false);
        }

        public void Stop()
        {
            if (_instrumental.hasHandle()) _instrumental.stop();
            if (_vocals.hasHandle()) _vocals.stop();
            _instrumental.clearHandle();
            _vocals.clearHandle();
        }

        public void SetVocalsMuted(bool muted)
        {
            _vocalsMuted = muted;
            if (_vocals.hasHandle()) _vocals.setMute(muted);
        }

        /// <summary>FMOD ignores AudioListener.volume, so the master volume option is applied here.</summary>
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            if (_instrumental.hasHandle()) _instrumental.setVolume(_volume);
            if (_vocals.hasHandle()) _vocals.setVolume(_volume);
        }

        public void Release()
        {
            Stop();
            ReleaseSounds();
        }

        void ReleaseSounds()
        {
            Stop();
            if (_instrumentalSound.hasHandle()) _instrumentalSound.release();
            if (_vocalsSound.hasHandle()) _vocalsSound.release();
            _instrumentalSound.clearHandle();
            _vocalsSound.clearHandle();
            _hasVocals = false;
        }

        static bool Check(FMOD.RESULT result, string call)
        {
            if (result == FMOD.RESULT.OK) return true;
            Debug.LogError($"FMOD spike: {call} failed: {result}");
            return false;
        }
    }
}
#endif

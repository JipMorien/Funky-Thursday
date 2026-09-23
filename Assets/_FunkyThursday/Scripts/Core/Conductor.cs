using System;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// The single source of musical time. Schedules the instrumental (and optional vocal) track on
    /// the DSP clock and exposes the song position in seconds, beats and steps (4 steps per beat).
    ///
    /// AudioSettings.dspTime only advances once per audio buffer, so reading it directly makes
    /// notes stutter. The conductor advances a smoothed clock with unscaledDeltaTime every frame
    /// and nudges it toward the DSP clock whenever that updates, never moving backwards unless
    /// the error is large enough to need a hard resync (e.g. after a hitch).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(AudioSource))]
    public sealed class Conductor : MonoBehaviour
    {
        public enum PlaybackState
        {
            Stopped,
            Playing,
            Paused,
            Finished
        }

        public const int StepsPerBeat = 4;

        const double ResyncThreshold = 0.05;   // seconds of drift before snapping to the DSP clock
        const double DriftCorrection = 0.5;    // fraction of small drift removed per DSP update
        const int MaxStepsPerFrame = 64;       // caps event storms after a long hitch

        [Header("Timing")]
        [SerializeField, Min(1f)] float bpm = 100f;

        [Tooltip("Seconds between Play() and the first audio sample. Lets notes scroll in before the music starts.")]
        [SerializeField, Min(0.1f)] float startDelay = 2f;

        [Tooltip("Output latency compensation in milliseconds. Raise it if hits feel consistently late.")]
        [SerializeField] float audioOffsetMs = 0f;

        [Header("Sources")]
        [Tooltip("Optional. Created as a child automatically when a vocal track is played.")]
        [SerializeField] AudioSource vocalsSource;

        /// <summary>Fired once per beat. Negative during the count-in.</summary>
        public event Action<int> BeatHit;

        /// <summary>Fired once per step (quarter beat). Negative during the count-in.</summary>
        public event Action<int> StepHit;

        /// <summary>Fired once when the song position passes the end of the instrumental.</summary>
        public event Action SongFinished;

        AudioSource _source;
        double _dspSongStart;
        double _lastDsp;
        double _smoothedTime;
        double _pauseDspTime;
        double _songLength;
        bool _pausedBeforeStart;
        bool _hasVocals;
        int _lastStep;

        public PlaybackState State { get; private set; } = PlaybackState.Stopped;
        public bool IsPlaying => State == PlaybackState.Playing;

        public float Bpm => bpm;

        /// <summary>Seconds per beat.</summary>
        public double Crochet => 60.0 / bpm;

        /// <summary>Seconds per step.</summary>
        public double StepCrochet => Crochet / StepsPerBeat;

        /// <summary>Song time in seconds, offset-corrected. Negative during the count-in.</summary>
        public double SongPosition { get; private set; }

        public double SongPositionInBeats => SongPosition / Crochet;
        public int CurrentBeat => (int)Math.Floor(SongPositionInBeats);
        public int CurrentStep => (int)Math.Floor(SongPosition / StepCrochet);
        public double SongLength => _songLength;

        public float AudioOffsetMs
        {
            get => audioOffsetMs;
            set => audioOffsetMs = value;
        }

        public float StartDelay
        {
            get => startDelay;
            set => startDelay = Mathf.Max(0.1f, value);
        }

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            ConfigureSource(_source);
            if (vocalsSource != null) ConfigureSource(vocalsSource);
        }

        /// <summary>Schedules an instrumental-only song.</summary>
        public void Play(AudioClip clip, float songBpm) => Play(clip, null, songBpm);

        /// <summary>Schedules the instrumental and (optional) vocal track to start together after the start delay.</summary>
        public void Play(AudioClip instrumental, AudioClip vocals, float songBpm)
        {
            if (instrumental == null) throw new ArgumentNullException(nameof(instrumental));
            if (songBpm <= 0f) throw new ArgumentOutOfRangeException(nameof(songBpm), "BPM must be positive.");

            Stop();

            bpm = songBpm;
            _songLength = instrumental.length;
            _source.clip = instrumental;

            _hasVocals = vocals != null;
            if (_hasVocals)
            {
                EnsureVocalsSource();
                vocalsSource.clip = vocals;
                vocalsSource.mute = false;
            }

            double now = AudioSettings.dspTime;
            _dspSongStart = now + startDelay;
            _source.PlayScheduled(_dspSongStart);
            if (_hasVocals) vocalsSource.PlayScheduled(_dspSongStart);

            _lastDsp = now;
            _smoothedTime = now - _dspSongStart;
            SongPosition = _smoothedTime - audioOffsetMs * 0.001;
            _lastStep = CurrentStep - 1;

            State = PlaybackState.Playing;
        }

        public void Pause()
        {
            if (State != PlaybackState.Playing) return;

            _pauseDspTime = AudioSettings.dspTime;
            _pausedBeforeStart = _pauseDspTime < _dspSongStart;

            if (_pausedBeforeStart)
            {
                _source.Stop();
                if (_hasVocals) vocalsSource.Stop();
            }
            else
            {
                _source.Pause();
                if (_hasVocals) vocalsSource.Pause();
            }

            State = PlaybackState.Paused;
        }

        public void Resume()
        {
            if (State != PlaybackState.Paused) return;

            double now = AudioSettings.dspTime;

            if (_pausedBeforeStart)
            {
                _dspSongStart += now - _pauseDspTime;
                _source.PlayScheduled(_dspSongStart);
                if (_hasVocals) vocalsSource.PlayScheduled(_dspSongStart);
            }
            else
            {
                if (_hasVocals)
                {
                    vocalsSource.timeSamples = Mathf.Min(_source.timeSamples, vocalsSource.clip.samples - 1);
                    vocalsSource.UnPause();
                }
                _source.UnPause();
                _dspSongStart = now - (double)_source.timeSamples / _source.clip.frequency;
            }

            _lastDsp = now;
            _smoothedTime = now - _dspSongStart;
            SongPosition = _smoothedTime - audioOffsetMs * 0.001;
            State = PlaybackState.Playing;
        }

        public void Stop()
        {
            if (_source != null) _source.Stop();
            if (vocalsSource != null) vocalsSource.Stop();
            State = PlaybackState.Stopped;
            SongPosition = 0.0;
        }

        /// <summary>FNF-style: the vocal track drops out while the player is missing.</summary>
        public void SetVocalsMuted(bool muted)
        {
            if (vocalsSource != null) vocalsSource.mute = muted;
        }

        public double BeatToSeconds(double beat) => beat * Crochet;
        public double SecondsToBeat(double seconds) => seconds / Crochet;

        void Update()
        {
            if (State != PlaybackState.Playing) return;

            AdvanceClock();
            DispatchSteps();

            if (SongPosition >= _songLength)
            {
                State = PlaybackState.Finished;
                SongFinished?.Invoke();
            }
        }

        void AdvanceClock()
        {
            double previous = _smoothedTime;
            _smoothedTime += Time.unscaledDeltaTime;

            double dsp = AudioSettings.dspTime;
            if (dsp != _lastDsp)
            {
                _lastDsp = dsp;
                double target = dsp - _dspSongStart;
                double error = target - _smoothedTime;

                _smoothedTime = Math.Abs(error) > ResyncThreshold
                    ? target
                    : Math.Max(previous, _smoothedTime + error * DriftCorrection);
            }

            SongPosition = _smoothedTime - audioOffsetMs * 0.001;
        }

        void DispatchSteps()
        {
            int step = CurrentStep;
            if (step - _lastStep > MaxStepsPerFrame) _lastStep = step - MaxStepsPerFrame;

            while (_lastStep < step)
            {
                _lastStep++;
                StepHit?.Invoke(_lastStep);

                if (PositiveModulo(_lastStep, StepsPerBeat) == 0)
                {
                    BeatHit?.Invoke(FloorDivide(_lastStep, StepsPerBeat));
                }
            }
        }

        void EnsureVocalsSource()
        {
            if (vocalsSource != null) return;

            var child = new GameObject("Vocals");
            child.transform.SetParent(transform, false);
            vocalsSource = child.AddComponent<AudioSource>();
            ConfigureSource(vocalsSource);
        }

        static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
        }

        static int PositiveModulo(int value, int divisor) => ((value % divisor) + divisor) % divisor;
        static int FloorDivide(int value, int divisor) => (int)Math.Floor((double)value / divisor);
    }
}

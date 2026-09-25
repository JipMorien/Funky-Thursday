using System;
using FunkyThursday.Core.Audio;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// The single source of musical time. Schedules the instrumental (and optional vocal) track on
    /// the audio engine's DSP clock and exposes the song position in seconds, beats and steps.
    ///
    /// The audio clock only advances once per mix block, so reading it directly makes notes stutter.
    /// The conductor advances a smoothed clock with unscaledDeltaTime every frame and nudges it toward
    /// the audio clock whenever that updates, never moving backwards unless the error is large enough
    /// to need a hard resync (e.g. after a hitch).
    ///
    /// FMOD spike: playback goes through an <see cref="IMusicBackend"/> (Unity AudioSources or the
    /// FMOD Core API), chosen per round with <see cref="Backend"/>. The smoothing and beat logic is
    /// shared, and <see cref="ClockStats"/> measures how each engine's clock behaves.
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

        /// <summary>How the audio clock behaved during the current round.</summary>
        public struct ClockReport
        {
            public string backend;
            public int clockUpdates;
            public double meanClockStepMs;
            public double meanClockErrorMs;
            public double maxClockErrorMs;
            public int hardResyncs;
        }

        public const int StepsPerBeat = 4;

        const double ResyncThreshold = 0.05;   // seconds of drift before snapping to the audio clock
        const double DriftCorrection = 0.5;    // fraction of small drift removed per clock update
        const int MaxStepsPerFrame = 64;       // caps event storms after a long hitch

        [Header("Timing")]
        [SerializeField, Min(1f)] float bpm = 100f;

        [Tooltip("Seconds between Play() and the first audio sample. Lets notes scroll in before the music starts.")]
        [SerializeField, Min(0.1f)] float startDelay = 2f;

        [Tooltip("Output latency compensation in milliseconds. Raise it if hits feel consistently late.")]
        [SerializeField] float audioOffsetMs = 0f;

        [Header("Audio engine (FMOD spike)")]
        [SerializeField] MusicBackendKind backend = MusicBackendKind.Unity;

        [Header("Sources")]
        [Tooltip("Optional. Created as a child automatically when a vocal track is played.")]
        [SerializeField] AudioSource vocalsSource;

        /// <summary>Fired once per beat. Negative during the count-in.</summary>
        public event Action<int> BeatHit;

        /// <summary>Fired once per step (quarter beat). Negative during the count-in.</summary>
        public event Action<int> StepHit;

        /// <summary>Fired once when the song position passes the end of the instrumental.</summary>
        public event Action SongFinished;

        IMusicBackend _music;
        MusicBackendKind _activeKind;
        double _songStart;
        double _lastClock;
        double _smoothedTime;
        double _pauseClock;
        double _songLength;
        bool _pausedBeforeStart;
        int _lastStep;

        // Clock measurements for the comparison.
        int _clockUpdates;
        double _clockStepSum;
        double _clockErrorSum;
        double _clockErrorMax;
        int _hardResyncs;

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

        /// <summary>Engine used from the next Play() on.</summary>
        public MusicBackendKind Backend
        {
            get => backend;
            set => backend = value;
        }

        /// <summary>Folder name under StreamingAssets/FmodSpike for the FMOD engine (the song id).</summary>
        public string SongId { get; set; }

        /// <summary>Name of the engine actually playing (falls back to Unity if FMOD isn't available).</summary>
        public string ActiveBackendName => _music != null ? _music.Name : "none";

        public ClockReport ClockStats => new ClockReport
        {
            backend = ActiveBackendName,
            clockUpdates = _clockUpdates,
            meanClockStepMs = _clockUpdates > 1 ? _clockStepSum / (_clockUpdates - 1) * 1000.0 : 0.0,
            meanClockErrorMs = _clockUpdates > 0 ? _clockErrorSum / _clockUpdates * 1000.0 : 0.0,
            maxClockErrorMs = _clockErrorMax * 1000.0,
            hardResyncs = _hardResyncs
        };

        void Awake()
        {
            EnsureVocalsSource();
        }

        void OnDestroy()
        {
            _music?.Release();
            _music = null;
        }

        /// <summary>Schedules an instrumental-only song.</summary>
        public void Play(AudioClip clip, float songBpm) => Play(clip, null, songBpm);

        /// <summary>Schedules the instrumental and (optional) vocal track to start together after the start delay.</summary>
        public void Play(AudioClip instrumental, AudioClip vocals, float songBpm)
        {
            if (instrumental == null) throw new ArgumentNullException(nameof(instrumental));
            if (songBpm <= 0f) throw new ArgumentOutOfRangeException(nameof(songBpm), "BPM must be positive.");

            Stop();
            SelectBackend();

            bpm = songBpm;
            if (!_music.Load(instrumental, vocals, SongId, out _songLength))
            {
                // FMOD couldn't load the files: fall back so the round still plays.
                Debug.LogWarning($"{_music.Name} could not load the song; falling back to Unity audio.");
                UseBackend(MusicBackendKind.Unity);
                _music.Load(instrumental, vocals, SongId, out _songLength);
            }
            _music.SetVolume(GameSettings.Volume);

            double now = _music.Clock;
            _songStart = now + startDelay;
            _music.PlayAt(_songStart);

            ResetClockStats();
            _lastClock = now;
            _smoothedTime = now - _songStart;
            SongPosition = _smoothedTime - audioOffsetMs * 0.001;
            _lastStep = CurrentStep - 1;

            State = PlaybackState.Playing;
        }

        public void Pause()
        {
            if (State != PlaybackState.Playing) return;

            _pauseClock = _music.Clock;
            _pausedBeforeStart = _pauseClock < _songStart;

            if (_pausedBeforeStart) _music.Stop();
            else _music.Pause();

            State = PlaybackState.Paused;
        }

        public void Resume()
        {
            if (State != PlaybackState.Paused) return;

            double now = _music.Clock;

            if (_pausedBeforeStart)
            {
                _songStart += now - _pauseClock;
                _music.PlayAt(_songStart);
            }
            else
            {
                _music.Unpause();
                _songStart = now - _music.PlaybackPosition;
            }

            _lastClock = now;
            _smoothedTime = now - _songStart;
            SongPosition = _smoothedTime - audioOffsetMs * 0.001;
            State = PlaybackState.Playing;
        }

        public void Stop()
        {
            _music?.Stop();
            State = PlaybackState.Stopped;
            SongPosition = 0.0;
        }

        /// <summary>FNF-style: the vocal track drops out while the player is missing.</summary>
        public void SetVocalsMuted(bool muted) => _music?.SetVocalsMuted(muted);

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

            double clock = _music.Clock;
            if (clock != _lastClock)
            {
                double step = clock - _lastClock;
                _lastClock = clock;
                double target = clock - _songStart;
                double error = target - _smoothedTime;

                RecordClockUpdate(step, Math.Abs(error));

                if (Math.Abs(error) > ResyncThreshold)
                {
                    _smoothedTime = target;
                    _hardResyncs++;
                }
                else
                {
                    _smoothedTime = Math.Max(previous, _smoothedTime + error * DriftCorrection);
                }
            }

            SongPosition = _smoothedTime - audioOffsetMs * 0.001;
        }

        void RecordClockUpdate(double step, double absError)
        {
            if (_clockUpdates > 0) _clockStepSum += step;
            _clockErrorSum += absError;
            if (absError > _clockErrorMax) _clockErrorMax = absError;
            _clockUpdates++;
        }

        void ResetClockStats()
        {
            _clockUpdates = 0;
            _clockStepSum = 0.0;
            _clockErrorSum = 0.0;
            _clockErrorMax = 0.0;
            _hardResyncs = 0;
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

        void SelectBackend()
        {
            MusicBackendKind wanted = backend;
#if !FT_FMOD
            if (wanted == MusicBackendKind.Fmod)
            {
                Debug.LogWarning("FMOD selected, but the FT_FMOD scripting define isn't set. Using Unity audio.");
                wanted = MusicBackendKind.Unity;
            }
#endif
            if (_music == null || _activeKind != wanted) UseBackend(wanted);
        }

        void UseBackend(MusicBackendKind kind)
        {
            _music?.Release();
            _activeKind = kind;
#if FT_FMOD
            if (kind == MusicBackendKind.Fmod)
            {
                _music = new FmodMusicBackend();
                return;
            }
#endif
            _music = new UnityMusicBackend(GetComponent<AudioSource>(), vocalsSource);
        }

        void EnsureVocalsSource()
        {
            if (vocalsSource != null) return;

            var child = new GameObject("Vocals");
            child.transform.SetParent(transform, false);
            vocalsSource = child.AddComponent<AudioSource>();
        }

        static int PositiveModulo(int value, int divisor) => ((value % divisor) + divisor) % divisor;
        static int FloorDivide(int value, int divisor) => (int)Math.Floor((double)value / divisor);
    }
}

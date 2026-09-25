using System;
using FunkyThursday.AI;
using FunkyThursday.Charts;
using FunkyThursday.Core;
using FunkyThursday.Data;
using FunkyThursday.Flow;
using FunkyThursday.Placeholders;
using FunkyThursday.UI;
using FunkyThursday.Visuals;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Runs one round of the selected SongData: loads chart and audio, applies the player's options,
    /// wires judgements to health, characters and the vocal track, and hands pausing and the end of
    /// the round to the Pause Menu and Round End screen. The song comes from SongManager; opened
    /// directly in the editor it falls back to its own library and Start Level.
    /// </summary>
    public sealed class GameplayController : MonoBehaviour
    {
        public enum RoundState
        {
            Idle,
            Playing,
            Paused,
            Cleared,
            Perished
        }

        [Header("Songs")]
        [SerializeField] SongLibrary library;
        [Tooltip("Level played when the scene is opened directly, without coming from the menu.")]
        [SerializeField, Min(0)] int startLevel;
        [SerializeField, Min(0)] int countdownBeats = 4;

        [Header("Scene")]
        [SerializeField] Conductor conductor;
        [SerializeField] NoteSpawner spawner;
        [SerializeField] PlayerInput input;
        [SerializeField] HitJudge judge;
        [SerializeField] AIOpponent opponentAI;
        [SerializeField] AIOpponent playerBot;
        [SerializeField] HealthSystem health;
        [SerializeField] HealthBar healthBar;
        [SerializeField] CharacterPuppet opponent;
        [SerializeField] CharacterPuppet player;
        [SerializeField] SpriteRenderer backdrop;
        [SerializeField] SpriteAnimator backdropAnimator;
        [SerializeField] VideoLoopBackdrop videoBackdrop;
        [Tooltip("Play each song's MP4 loop through the VideoPlayer instead of its sprite-sheet backdrop.")]
        [SerializeField] bool preferVideoBackdrop;

        [Header("Menus")]
        [SerializeField] PauseMenu pauseMenu;
        [SerializeField] RoundEndScreen roundEnd;

        [Header("Testing")]
        [Tooltip("The player side plays itself. Use it to watch a whole chart.")]
        [SerializeField] bool botplay;
        [Tooltip("Number keys 1-4 jump between songs mid-round.")]
        [SerializeField] bool debugLevelKeys = true;
        [SerializeField] bool showOverlay;

        RoundStats _stats;
        int _combo;
        HitResult? _last;
        int _ignorePauseUntilFrame;

        /// <summary>Fired when a round ends as Cleared or Perished.</summary>
        public event Action<RoundState, RoundStats> RoundEnded;

        public RoundState State { get; private set; } = RoundState.Idle;
        public SongData CurrentSong { get; private set; }
        public Chart CurrentChart { get; private set; }
        public int CurrentLevel { get; private set; } = -1;
        public RoundStats Stats => _stats;

        SongLibrary Library =>
            SongManager.Instance != null && SongManager.Instance.Library != null ? SongManager.Instance.Library : library;

        void Awake()
        {
            bool missing = conductor == null || spawner == null || input == null || judge == null
                || opponentAI == null || playerBot == null || health == null || healthBar == null
                || opponent == null || player == null || pauseMenu == null || roundEnd == null;
            if (missing)
            {
                Debug.LogError("GameplayController has unassigned scene references. Run Funky Thursday → Build All Scenes.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            judge.Judged += HandleJudged;
            judge.SustainHeld += HandleSustain;
            opponentAI.Sang += HandleOpponentSang;
            opponentAI.DrainRequested += HandleDrain;
            playerBot.Sang += HandleBotSang;
            conductor.BeatHit += HandleBeat;
            conductor.SongFinished += HandleSongFinished;
            health.Died += HandleDied;
            pauseMenu.ResumeRequested += Resume;
            pauseMenu.RestartRequested += Restart;
            pauseMenu.ExitRequested += ExitToLevelSelect;
            roundEnd.RetryRequested += Restart;
            roundEnd.ExitRequested += ExitToLevelSelect;
        }

        void OnDisable()
        {
            judge.Judged -= HandleJudged;
            judge.SustainHeld -= HandleSustain;
            opponentAI.Sang -= HandleOpponentSang;
            opponentAI.DrainRequested -= HandleDrain;
            playerBot.Sang -= HandleBotSang;
            conductor.BeatHit -= HandleBeat;
            conductor.SongFinished -= HandleSongFinished;
            health.Died -= HandleDied;
            pauseMenu.ResumeRequested -= Resume;
            pauseMenu.RestartRequested -= Restart;
            pauseMenu.ExitRequested -= ExitToLevelSelect;
            roundEnd.RetryRequested -= Restart;
            roundEnd.ExitRequested -= ExitToLevelSelect;
        }

        void Start()
        {
            GameSettings.ApplyAudio();
            if (Library == null || Library.Count == 0)
            {
                Debug.LogError("No Song Library assigned. Run Funky Thursday → Build All Scenes.", this);
                return;
            }

            player.SetPlaceholderColors(Library.playerCloak, Library.playerEyes);
            player.SetPoses(Library.playerPoses);

            int index = SongManager.Instance != null ? SongManager.Instance.CurrentIndex : startLevel;
            LoadLevel(index);
        }

        void Update()
        {
            if (State != RoundState.Playing || Time.frameCount <= _ignorePauseUntilFrame) return;

            if (input.PausePressed) Pause();
            else if (input.RestartPressed) Restart();
            else if (debugLevelKeys && input.LevelKeyPressed >= 0) LoadLevel(input.LevelKeyPressed);
        }

        // ── Round flow ───────────────────────────────────────────────────────────

        public void LoadLevel(int index)
        {
            SongData song = Library != null ? Library.Get(index) : null;
            if (song == null || song.chart == null || song.instrumental == null)
            {
                Debug.LogWarning($"Song slot {index + 1} is missing its chart or instrumental.", this);
                return;
            }

            Chart chart;
            try
            {
                chart = song.LoadChart();
            }
            catch (ChartFormatException e)
            {
                Debug.LogError(e.Message, song.chart);
                return;
            }

            if (Math.Abs(song.instrumental.length - chart.LengthSeconds) > 5.0)
            {
                Debug.LogWarning($"{song.displayName}: audio is {song.instrumental.length:0.0}s but the chart is {chart.LengthSeconds:0.0}s. Check the clip and bpm.", this);
            }

            CurrentSong = song;
            CurrentChart = chart;
            CurrentLevel = index;

            pauseMenu.Hide();
            roundEnd.Hide();

            opponent.SetPlaceholderColors(song.opponentCloak, song.opponentEyes);
            opponent.SetPoses(song.opponentPoses);
            player.ResetPose();
            ApplyBackdrop(song);

            spawner.Load(chart);
            health.ResetHealth();
            healthBar.SetFighters(opponent, player);
            healthBar.Snap();
            opponentAI.Configure(chart.Data.ai);

            judge.GhostTapping = GameSettings.GhostTapping;
            TimingProfile timing = TimingProfile.For(GameSettings.Timing);
            judge.SetWindows(timing.PerfectWindowMs, timing.GoodWindowMs, timing.MissWindowMs);
            health.LossScale = timing.LossScale;
            health.DrainScale = timing.DrainScale;
            judge.enabled = !botplay;
            playerBot.enabled = botplay;

            _stats = default;
            _combo = 0;
            _last = null;

            conductor.AudioOffsetMs = GameSettings.AudioOffsetMs;
            conductor.StartDelay = Mathf.Max(0.5f, countdownBeats * 60f / chart.Bpm);
            conductor.Play(song.instrumental, song.vocals, chart.Bpm);
            State = RoundState.Playing;
        }

        public void Pause()
        {
            if (State != RoundState.Playing) return;
            conductor.Pause();
            State = RoundState.Paused;
            pauseMenu.Show(CurrentSong != null ? CurrentSong.displayName : "");
        }

        public void Resume()
        {
            if (State != RoundState.Paused) return;
            pauseMenu.Hide();
            conductor.Resume();
            State = RoundState.Playing;
            _ignorePauseUntilFrame = Time.frameCount + 1; // the key that closed the menu must not reopen it
        }

        public void Restart() => LoadLevel(CurrentLevel);

        public void ExitToLevelSelect()
        {
            conductor.Stop();
            if (SongManager.Instance != null) SongManager.Instance.ReturnToLevelSelect();
            else SongManager.GoTo(SongManager.MenuScene);
        }

        void EndRound(RoundState result)
        {
            State = result;
            _stats.cleared = result == RoundState.Cleared;

            bool newBest;
            if (SongManager.Instance != null)
            {
                newBest = CurrentSong != null && (!SaveData.HasResult(CurrentSong.id) || _stats.Beats(SaveData.GetBest(CurrentSong.id)));
                SongManager.Instance.ReportResult(CurrentSong, _stats);
            }
            else
            {
                newBest = CurrentSong != null && SaveData.Submit(CurrentSong.id, _stats);
            }

            roundEnd.Show(_stats.cleared, _stats, CurrentSong != null ? CurrentSong.displayName : "", newBest);
            RoundEnded?.Invoke(result, _stats);
        }

        void ApplyBackdrop(SongData song)
        {
            bool useVideo = preferVideoBackdrop && videoBackdrop != null && !string.IsNullOrEmpty(song.backdropVideo);
            if (useVideo)
            {
                videoBackdrop.Play(song.backdropVideo);
                if (backdropAnimator != null) backdropAnimator.Stop();
                if (backdrop != null) backdrop.enabled = false;
                return;
            }

            if (videoBackdrop != null) videoBackdrop.Stop();
            if (backdrop == null) return;
            backdrop.enabled = true;

            if (backdropAnimator != null && SpriteAnimation.IsUsable(song.backdrop))
            {
                backdropAnimator.Play(song.backdrop);
                backdrop.color = Color.white;
            }
            else
            {
                if (backdropAnimator != null) backdropAnimator.Stop();
                backdrop.sprite = PixelArtFactory.GraveyardBackdrop;
                backdrop.color = song.backdropTint;
            }
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        void HandleJudged(HitResult result)
        {
            health.Apply(result);
            _last = result;

            if (result.Judgement == Judgement.Miss)
            {
                _stats.misses++;
                _combo = 0;
                player.Miss(result.Lane);
                conductor.SetVocalsMuted(true);
                return;
            }

            if (result.Judgement == Judgement.Perfect) _stats.perfects++;
            else _stats.goods++;

            _combo++;
            _stats.maxCombo = Mathf.Max(_stats.maxCombo, _combo);
            player.Sing(result.Lane, result.HoldSeconds);
            conductor.SetVocalsMuted(false);
        }

        void HandleBotSang(NoteLane lane, double holdSeconds) =>
            HandleJudged(new HitResult(Judgement.Perfect, lane, 0.0, holdSeconds, HitKind.Tap));

        void HandleSustain(float seconds) => health.AddHold(seconds);

        void HandleOpponentSang(NoteLane lane, double holdSeconds) => opponent.Sing(lane, holdSeconds);

        void HandleDrain(float amount, float floor) => health.Drain(amount, floor);

        void HandleBeat(int beat)
        {
            opponent.Bop(beat);
            player.Bop(beat);
        }

        void HandleSongFinished()
        {
            if (State == RoundState.Playing) EndRound(RoundState.Cleared);
        }

        void HandleDied()
        {
            if (State != RoundState.Playing) return;
            conductor.Stop();
            spawner.Clear();
            player.Miss(NoteLane.Down);
            EndRound(RoundState.Perished);
        }

        // ── Debug overlay ────────────────────────────────────────────────────────

        void OnGUI()
        {
            if (!showOverlay) return;

            string song = CurrentSong != null ? $"{CurrentSong.displayName} · {CurrentSong.difficulty} · {CurrentChart.Bpm:0} BPM" : "No song";
            string last = "—";
            if (_last.HasValue)
            {
                HitResult r = _last.Value;
                string detail = r.Kind == HitKind.Tap ? r.OffsetMs.ToString("+0;-0;0") + " ms" : r.Kind.ToString();
                last = $"{r.Judgement} ({detail})";
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 440f, 150f), GUI.skin.box);
            GUILayout.Label(song + (botplay ? "   [BOTPLAY]" : ""));
            GUILayout.Label($"{State}   {conductor.SongPosition:0.00}s   beat {conductor.CurrentBeat}   offset {conductor.AudioOffsetMs:0} ms");
            GUILayout.Label($"Perfect {_stats.perfects}   Good {_stats.goods}   Miss {_stats.misses}   Combo {_combo} (best {_stats.maxCombo})");
            GUILayout.Label($"Health {health.Value:P0}   Last: {last}");
            GUILayout.EndArea();
        }
    }
}

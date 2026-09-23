#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define FT_NEW_INPUT
#endif

using System;
using System.Collections.Generic;
using FunkyThursday.Core;
using FunkyThursday.Visuals;
using UnityEngine;
#if FT_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace FunkyThursday.Placeholders
{
    /// <summary>
    /// Phase 1 sandbox. Builds a complete placeholder stage from code (graveyard backdrop, two
    /// wraiths, both strumlines, tug-of-war health bar) and scrolls a generated call-and-response
    /// pattern against the Conductor, so timing and input can be tested before any chart, song or
    /// Higgsfield asset exists. NoteSpawner + chart loading replace the pattern in the next step.
    /// </summary>
    public sealed class PlaceholderStage : MonoBehaviour
    {
        const int Opponent = 0;
        const int Player = 1;

        const int SortBackdrop = 0;
        const int SortCharacters = 10;
        const int SortReceptors = 20;
        const int SortNotes = 30;
        const int SortHud = 40;

        const float ConfirmDuration = 0.15f;
        const float BopDuration = 0.12f;
        const float PoseDuration = 0.25f;
        const float MissFlashDuration = 0.25f;
        const float CharacterScale = 3f;
        const float CharacterX = 3.2f;
        const float CameraSize = 5f;
        const float BarY = -4.4f;
        const float BarHeight = 0.28f;
        const float BarWidth = 9f;

        [Header("References")]
        [SerializeField] Conductor conductor;

        [Tooltip("Optional. Leave empty to use a generated click track.")]
        [SerializeField] AudioClip songOverride;

        [Header("Test Pattern")]
        [SerializeField, Range(60f, 220f)] float testBpm = 100f;
        [SerializeField, Min(8)] int testBeats = 64;
        [SerializeField, Range(0f, 1f)] float eighthNoteChance = 0.3f;
        [SerializeField] int patternSeed = 1313;

        [Header("Layout (world units)")]
        [SerializeField] float receptorY = 3.4f;
        [SerializeField] float laneSpacing = 1.45f;
        [SerializeField] float innerLaneX = 2.9f;
        [SerializeField] float arrowScale = 1.3f;
        [SerializeField, Min(1f)] float scrollSpeed = 9f;

        [Header("Hit Windows (ms)")]
        [SerializeField, Min(1f)] float perfectWindowMs = 45f;
        [SerializeField, Min(1f)] float goodWindowMs = 90f;
        [SerializeField, Min(1f)] float missWindowMs = 135f;

        [Header("Health")]
        [SerializeField] float healthGainPerfect = 0.023f;
        [SerializeField] float healthGainGood = 0.012f;
        [SerializeField] float healthLossMiss = 0.0475f;

        [Header("Debug")]
        [SerializeField] bool showDebugOverlay = true;

#if FT_NEW_INPUT
        static readonly Key[] PrimaryKeys = { Key.LeftArrow, Key.DownArrow, Key.UpArrow, Key.RightArrow };
        static readonly Key[] AltKeys = { Key.A, Key.S, Key.W, Key.D };
#else
        static readonly KeyCode[] PrimaryKeys = { KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.UpArrow, KeyCode.RightArrow };
        static readonly KeyCode[] AltKeys = { KeyCode.A, KeyCode.S, KeyCode.W, KeyCode.D };
#endif

        struct PatternNote
        {
            public double Time;
            public NoteLane Lane;
            public int Side;
        }

        sealed class ActiveNote
        {
            public double Time;
            public SpriteRenderer Renderer;
        }

        sealed class Strumline
        {
            public readonly float[] LaneX = new float[Lanes.Count];
            public readonly SpriteRenderer[] Receptors = new SpriteRenderer[Lanes.Count];
            public readonly float[] ConfirmTimers = new float[Lanes.Count];
            public readonly bool[] Held = new bool[Lanes.Count];
            public readonly List<ActiveNote>[] Notes = new List<ActiveNote>[Lanes.Count];

            public SpriteRenderer Character;
            public Vector3 CharacterHome;
            public Vector2 PoseOffset;
            public float BopTimer;
            public float PoseTimer;
            public float MissTimer;

            public Strumline()
            {
                for (int i = 0; i < Lanes.Count; i++) Notes[i] = new List<ActiveNote>();
            }
        }

        readonly Strumline[] _strums = { new Strumline(), new Strumline() };
        readonly List<PatternNote> _pattern = new List<PatternNote>();
        readonly Stack<SpriteRenderer> _notePool = new Stack<SpriteRenderer>();

        Camera _camera;
        Transform _noteRoot;
        SpriteRenderer _healthOpponent;
        SpriteRenderer _healthPlayer;
        AudioClip _clip;
        int _nextPatternIndex;
        float _health = 0.5f;

        int _perfects, _goods, _misses, _combo;
        Judgement? _lastJudgement;
        double _lastOffsetMs;

        static float GroundY => -CameraSize + PixelArtFactory.BackdropGroundHeight / PixelArtFactory.BackdropPixelsPerUnit;

        void Awake()
        {
            if (conductor == null) conductor = FindFirstObjectByType<Conductor>();
            if (conductor == null)
            {
                Debug.LogError("PlaceholderStage needs a Conductor in the scene.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (conductor != null) conductor.BeatHit += HandleBeat;
        }

        void OnDisable()
        {
            if (conductor != null) conductor.BeatHit -= HandleBeat;
        }

        void Start()
        {
            SetupCamera();
            _noteRoot = CreateChild("Notes", transform);

            BuildBackdrop();
            BuildStrumline(Opponent);
            BuildStrumline(Player);
            BuildCharacters();
            BuildHealthBar();

            _clip = songOverride != null ? songOverride : ClickTrackFactory.Create(testBpm, testBeats);
            int beats = songOverride != null ? Mathf.FloorToInt(_clip.length * testBpm / 60f) : testBeats;
            BuildPattern(beats);

            StartSong();
        }

        void Update()
        {
            if (PausePressed()) TogglePause();
            if (RestartPressed()) StartSong();
            if (!conductor.IsPlaying) return;

            double time = conductor.SongPosition;
            float dt = Time.deltaTime;

            SpawnDueNotes(time);
            AutoPlayOpponent(time);
            ExpireMissedNotes(time);
            ReadPlayerInput(time);
            PositionNotes(time);
            AnimateReceptors(dt);
            AnimateCharacters(dt);
            UpdateHealthBar();
        }

        // ── Song flow ────────────────────────────────────────────────────────────

        void StartSong()
        {
            ClearActiveNotes();
            _nextPatternIndex = 0;
            _health = 0.5f;
            _perfects = _goods = _misses = _combo = 0;
            _lastJudgement = null;
            UpdateHealthBar();
            conductor.Play(_clip, testBpm);
        }

        void TogglePause()
        {
            if (conductor.IsPlaying) conductor.Pause();
            else if (conductor.State == Conductor.PlaybackState.Paused) conductor.Resume();
        }

        void BuildPattern(int beats)
        {
            _pattern.Clear();
            var rng = new System.Random(patternSeed);
            double crochet = 60.0 / testBpm;
            int previousLane = -1;
            int repeats = 0;

            void Add(double time, int side)
            {
                int lane = rng.Next(Lanes.Count);
                if (lane == previousLane)
                {
                    if (repeats >= 1)
                    {
                        lane = (lane + 1 + rng.Next(Lanes.Count - 1)) % Lanes.Count;
                        repeats = 0;
                    }
                    else repeats++;
                }
                else repeats = 0;

                previousLane = lane;
                _pattern.Add(new PatternNote { Time = time, Lane = (NoteLane)lane, Side = side });
            }

            // Call and response: opponent takes even bars, player answers on odd bars.
            for (int beat = 0; beat < beats - 1; beat++)
            {
                int side = (beat / 4) % 2 == 0 ? Opponent : Player;
                Add(beat * crochet, side);
                if (rng.NextDouble() < eighthNoteChance) Add((beat + 0.5) * crochet, side);
            }
        }

        // ── Notes ────────────────────────────────────────────────────────────────

        void SpawnDueNotes(double time)
        {
            double lead = (receptorY + _camera.orthographicSize + 1f) / scrollSpeed;
            while (_nextPatternIndex < _pattern.Count && _pattern[_nextPatternIndex].Time - time <= lead)
            {
                PatternNote p = _pattern[_nextPatternIndex++];
                _strums[p.Side].Notes[(int)p.Lane].Add(new ActiveNote { Time = p.Time, Renderer = RentNote(p.Lane) });
            }
        }

        void PositionNotes(double time)
        {
            foreach (Strumline strum in _strums)
            {
                for (int lane = 0; lane < Lanes.Count; lane++)
                {
                    foreach (ActiveNote note in strum.Notes[lane])
                    {
                        float y = receptorY - (float)(note.Time - time) * scrollSpeed;
                        note.Renderer.transform.localPosition = new Vector3(strum.LaneX[lane], y, 0f);
                    }
                }
            }
        }

        void AutoPlayOpponent(double time)
        {
            Strumline opponent = _strums[Opponent];
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                List<ActiveNote> notes = opponent.Notes[lane];
                while (notes.Count > 0 && notes[0].Time <= time)
                {
                    RecycleFirst(notes);
                    Confirm(opponent, lane);
                    Pose(opponent, lane);
                }
            }
        }

        void ExpireMissedNotes(double time)
        {
            Strumline player = _strums[Player];
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                List<ActiveNote> notes = player.Notes[lane];
                while (notes.Count > 0 && (time - notes[0].Time) * 1000.0 > missWindowMs)
                {
                    double offset = (time - notes[0].Time) * 1000.0;
                    RecycleFirst(notes);
                    RegisterJudgement(Judgement.Miss, lane, offset);
                }
            }
        }

        void ReadPlayerInput(double time)
        {
            Strumline player = _strums[Player];
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                player.Held[lane] = IsHeld(lane);
                if (!WasPressed(lane)) continue;

                List<ActiveNote> notes = player.Notes[lane];
                if (notes.Count == 0) continue; // ghost tap

                double offsetMs = (time - notes[0].Time) * 1000.0;
                double distance = Math.Abs(offsetMs);
                if (distance > missWindowMs) continue; // too early to count

                Judgement judgement = distance <= perfectWindowMs ? Judgement.Perfect
                    : distance <= goodWindowMs ? Judgement.Good
                    : Judgement.Miss;

                RecycleFirst(notes);
                RegisterJudgement(judgement, lane, offsetMs);
            }
        }

        void RegisterJudgement(Judgement judgement, int lane, double offsetMs)
        {
            Strumline player = _strums[Player];
            switch (judgement)
            {
                case Judgement.Perfect:
                    _perfects++;
                    _combo++;
                    _health += healthGainPerfect;
                    break;
                case Judgement.Good:
                    _goods++;
                    _combo++;
                    _health += healthGainGood;
                    break;
                default:
                    _misses++;
                    _combo = 0;
                    _health -= healthLossMiss;
                    player.MissTimer = MissFlashDuration;
                    break;
            }

            _health = Mathf.Clamp01(_health);
            _lastJudgement = judgement;
            _lastOffsetMs = offsetMs;

            if (judgement != Judgement.Miss)
            {
                Confirm(player, lane);
                Pose(player, lane);
            }
        }

        SpriteRenderer RentNote(NoteLane lane)
        {
            SpriteRenderer renderer;
            if (_notePool.Count > 0)
            {
                renderer = _notePool.Pop();
            }
            else
            {
                renderer = new GameObject("Note").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(_noteRoot, false);
                renderer.transform.localScale = Vector3.one * arrowScale;
                renderer.sortingOrder = SortNotes;
            }

            renderer.sprite = PixelArtFactory.GetArrow(lane, ArrowStyle.Note);
            renderer.transform.localPosition = new Vector3(0f, -100f, 0f);
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        void RecycleFirst(List<ActiveNote> notes)
        {
            SpriteRenderer renderer = notes[0].Renderer;
            notes.RemoveAt(0);
            renderer.gameObject.SetActive(false);
            _notePool.Push(renderer);
        }

        void ClearActiveNotes()
        {
            foreach (Strumline strum in _strums)
            {
                foreach (List<ActiveNote> notes in strum.Notes)
                {
                    while (notes.Count > 0) RecycleFirst(notes);
                }
            }
        }

        // ── Feedback ─────────────────────────────────────────────────────────────

        static void Confirm(Strumline strum, int lane) => strum.ConfirmTimers[lane] = ConfirmDuration;

        static void Pose(Strumline strum, int lane)
        {
            strum.PoseOffset = Lanes.Direction((NoteLane)lane) * 0.25f;
            strum.PoseTimer = PoseDuration;
        }

        void HandleBeat(int beat)
        {
            foreach (Strumline strum in _strums) strum.BopTimer = BopDuration;
        }

        void AnimateReceptors(float dt)
        {
            foreach (Strumline strum in _strums)
            {
                for (int lane = 0; lane < Lanes.Count; lane++)
                {
                    float timer = strum.ConfirmTimers[lane] = Mathf.Max(0f, strum.ConfirmTimers[lane] - dt);
                    bool held = strum.Held[lane];

                    ArrowStyle style = timer > 0f ? ArrowStyle.ReceptorConfirm
                        : held ? ArrowStyle.ReceptorPressed
                        : ArrowStyle.ReceptorIdle;

                    float pulse = timer > 0f ? 1f + 0.12f * (timer / ConfirmDuration) : held ? 0.92f : 1f;

                    SpriteRenderer receptor = strum.Receptors[lane];
                    receptor.sprite = PixelArtFactory.GetArrow((NoteLane)lane, style);
                    receptor.transform.localScale = Vector3.one * (arrowScale * pulse);
                }
            }
        }

        void AnimateCharacters(float dt)
        {
            foreach (Strumline strum in _strums)
            {
                strum.BopTimer = Mathf.Max(0f, strum.BopTimer - dt);
                strum.PoseTimer = Mathf.Max(0f, strum.PoseTimer - dt);
                strum.MissTimer = Mathf.Max(0f, strum.MissTimer - dt);

                float squash = strum.BopTimer / BopDuration;
                strum.Character.transform.localScale = new Vector3(
                    CharacterScale * (1f + 0.06f * squash),
                    CharacterScale * (1f - 0.08f * squash),
                    1f);

                Vector2 offset = strum.PoseTimer > 0f ? strum.PoseOffset : Vector2.zero;
                strum.Character.transform.localPosition = strum.CharacterHome + (Vector3)offset;

                float miss = strum.MissTimer / MissFlashDuration;
                strum.Character.color = Color.Lerp(Color.white, new Color(0.45f, 0.4f, 0.6f), miss);
            }
        }

        void UpdateHealthBar()
        {
            if (_healthOpponent == null) return;

            float left = -BarWidth * 0.5f;
            float right = BarWidth * 0.5f;
            float split = left + BarWidth * (1f - _health);

            SetBarSegment(_healthOpponent, left, split);
            SetBarSegment(_healthPlayer, split, right);
        }

        static void SetBarSegment(SpriteRenderer segment, float from, float to)
        {
            float width = Mathf.Max(0f, to - from);
            segment.enabled = width > 0.0001f;
            segment.transform.localPosition = new Vector3((from + to) * 0.5f, BarY, 0f);
            segment.transform.localScale = new Vector3(width, BarHeight, 1f);
        }

        // ── Stage construction ───────────────────────────────────────────────────

        void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            _camera.orthographic = true;
            _camera.orthographicSize = CameraSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = GothicPalette.Void;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        void BuildBackdrop()
        {
            CreateRenderer("Backdrop", PixelArtFactory.GraveyardBackdrop, transform, Vector3.zero, 1f, SortBackdrop);
        }

        void BuildStrumline(int side)
        {
            Strumline strum = _strums[side];
            Transform root = CreateChild(side == Opponent ? "Opponent Strumline" : "Player Strumline", transform);

            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                float x = side == Opponent
                    ? -(innerLaneX + (Lanes.Count - 1 - lane) * laneSpacing)
                    : innerLaneX + lane * laneSpacing;

                strum.LaneX[lane] = x;
                strum.Receptors[lane] = CreateRenderer(
                    $"Receptor {(NoteLane)lane}",
                    PixelArtFactory.GetArrow((NoteLane)lane, ArrowStyle.ReceptorIdle),
                    root, new Vector3(x, receptorY, 0f), arrowScale, SortReceptors);
            }
        }

        void BuildCharacters()
        {
            Transform root = CreateChild("Characters", transform);

            Strumline opponent = _strums[Opponent];
            opponent.CharacterHome = new Vector3(-CharacterX, GroundY, 0f);
            opponent.Character = CreateRenderer("Opponent",
                PixelArtFactory.GetWraith(GothicPalette.OpponentCloak, GothicPalette.OpponentEyes),
                root, opponent.CharacterHome, CharacterScale, SortCharacters);

            Strumline player = _strums[Player];
            player.CharacterHome = new Vector3(CharacterX, GroundY, 0f);
            player.Character = CreateRenderer("Player",
                PixelArtFactory.GetWraith(GothicPalette.PlayerCloak, GothicPalette.PlayerEyes),
                root, player.CharacterHome, CharacterScale, SortCharacters);
        }

        void BuildHealthBar()
        {
            Transform root = CreateChild("Health Bar", transform);

            SpriteRenderer frame = CreateRenderer("Frame", PixelArtFactory.WhitePixel, root, new Vector3(0f, BarY, 0f), 1f, SortHud);
            frame.transform.localScale = new Vector3(BarWidth + 0.16f, BarHeight + 0.16f, 1f);
            frame.color = GothicPalette.Ash;

            _healthOpponent = CreateRenderer("Opponent Fill", PixelArtFactory.WhitePixel, root, Vector3.zero, 1f, SortHud + 1);
            _healthOpponent.color = GothicPalette.Darken(GothicPalette.OpponentEyes, 0.3f);

            _healthPlayer = CreateRenderer("Player Fill", PixelArtFactory.WhitePixel, root, Vector3.zero, 1f, SortHud + 1);
            _healthPlayer.color = GothicPalette.Darken(GothicPalette.PlayerEyes, 0.3f);
        }

        static Transform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        static SpriteRenderer CreateRenderer(string name, Sprite sprite, Transform parent, Vector3 position, float scale, int sortingOrder)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.transform.localPosition = position;
            renderer.transform.localScale = Vector3.one * scale;
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        // ── Input ────────────────────────────────────────────────────────────────

#if FT_NEW_INPUT
        static bool WasPressed(int lane)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard[PrimaryKeys[lane]].wasPressedThisFrame || keyboard[AltKeys[lane]].wasPressedThisFrame);
        }

        static bool IsHeld(int lane)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard[PrimaryKeys[lane]].isPressed || keyboard[AltKeys[lane]].isPressed);
        }

        static bool PausePressed() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        static bool RestartPressed() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        static bool WasPressed(int lane) => Input.GetKeyDown(PrimaryKeys[lane]) || Input.GetKeyDown(AltKeys[lane]);
        static bool IsHeld(int lane) => Input.GetKey(PrimaryKeys[lane]) || Input.GetKey(AltKeys[lane]);
        static bool PausePressed() => Input.GetKeyDown(KeyCode.Escape);
        static bool RestartPressed() => Input.GetKeyDown(KeyCode.R);
#endif

        // ── Debug overlay ────────────────────────────────────────────────────────

        void OnGUI()
        {
            if (!showDebugOverlay || conductor == null) return;

            GUILayout.BeginArea(new Rect(12f, 12f, 360f, 150f), GUI.skin.box);
            GUILayout.Label($"{conductor.State}   {conductor.SongPosition:0.000}s   beat {conductor.CurrentBeat}   step {conductor.CurrentStep}   {conductor.Bpm:0} BPM");
            GUILayout.Label($"Perfect {_perfects}   Good {_goods}   Miss {_misses}   Combo {_combo}   Health {_health:P0}");
            GUILayout.Label(_lastJudgement.HasValue
                ? $"Last: {_lastJudgement.Value}  ({_lastOffsetMs:+0;-0;0} ms, negative = early)"
                : "Last: —");
            GUILayout.Label("Arrows / WASD hit   ·   Esc pause   ·   R restart");
            GUILayout.EndArea();
        }
    }
}

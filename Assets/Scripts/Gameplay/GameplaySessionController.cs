using UnityEngine;
using FunkyThursday.Core;
using FunkyThursday.Debug;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Boots up a full playable level at runtime: reads the level chosen at
    /// the level-select screen from GameFlowManager (or falls back to the
    /// first level under Resources/Levels, so this scene can be opened and
    /// tested directly), wires up the Conductor/score/health/spawner/input,
    /// runs a short countdown, and hands off to GameFlowManager's results
    /// screen when the song ends or health runs out.
    /// </summary>
    public class GameplaySessionController : MonoBehaviour
    {
        [Header("Visuals")]
        public Sprite arrowSprite;
        public float receptorSize = 1f;
        public float laneSpacing = 2.8f;
        public float strumlineY = 3.5f;
        public float spawnY = -7f;
        public float noteSize = 1f;

        [Header("Countdown")]
        public float countdownSeconds = 3f;

        [Header("Characters")]
        public float characterScale = 3f;
        [Tooltip("Horizontal gap between the lane row's outer edge and each character.")]
        public float characterGapX = 3f;
        public float characterY = 0.5f;

        private const int LaneCount = 4;

        private Conductor conductor;
        private LevelData level;
        private bool songStarted;
        private bool songEnded;
        private float countdownRemaining;
        private bool countingDown;

        private void Awake()
        {
            GameFlowManager.EnsureExists();
        }

        private void Start()
        {
            level = GameFlowManager.Instance.SelectedLevel;
            if (level == null)
            {
                LevelData[] fallback = Resources.LoadAll<LevelData>("Levels");
                level = fallback.Length > 0 ? fallback[0] : null;
            }

            if (level == null || level.songClip == null)
            {
                UnityEngine.Debug.LogError("[GameplaySessionController] No LevelData with a song assigned; cannot start.");
                return;
            }

            SpawnReceptors();
            SpawnCharacters();
            SetUpConductor();
            SetUpScoreManager();
            SetUpHealthManager();
            SetUpInputHandler();
            SetUpNoteSpawner();

            countingDown = true;
            countdownRemaining = countdownSeconds;
        }

        private void SpawnReceptors()
        {
            Color[] laneColors = PlaceholderAssetFactory.CreateLaneColors();
            int laneCount = laneColors.Length;
            float totalWidth = (laneCount - 1) * laneSpacing;
            float startX = -totalWidth / 2f;

            for (int lane = 0; lane < laneCount; lane++)
            {
                GameObject receptor = new GameObject($"Receptor_{lane}");
                receptor.transform.SetParent(transform);
                receptor.transform.position = new Vector3(startX + lane * laneSpacing, strumlineY, 0f);

                SpriteRenderer renderer = receptor.AddComponent<SpriteRenderer>();
                if (arrowSprite != null)
                {
                    renderer.sprite = arrowSprite;
                    renderer.color = laneColors[lane];
                    receptor.transform.rotation = Quaternion.Euler(0f, 0f, PlaceholderAssetFactory.LaneArrowRotationsZ[lane]);
                }
                else
                {
                    renderer.sprite = PlaceholderAssetFactory.CreateSolidSprite(laneColors[lane]);
                }
                receptor.transform.localScale = Vector3.one * receptorSize;

                BeatPulse pulse = receptor.AddComponent<BeatPulse>();
                pulse.target = receptor.transform;

                ReceptorHitFlash hitFlash = receptor.AddComponent<ReceptorHitFlash>();
                hitFlash.lane = lane;
            }
        }

        private void SpawnCharacters()
        {
            float halfWidth = (LaneCount - 1) * laneSpacing / 2f;

            SpawnCharacter("Opponent", -halfWidth - characterGapX,
                new Color(0.55f, 0.6f, 0.75f), new Color(0.3f, 0.35f, 0.5f), isPlayerControlled: false);

            SpawnCharacter("Player", halfWidth + characterGapX,
                new Color(0.95f, 0.75f, 0.35f), new Color(0.65f, 0.4f, 0.15f), isPlayerControlled: true);
        }

        private void SpawnCharacter(string name, float x, Color bodyColor, Color accentColor, bool isPlayerControlled)
        {
            GameObject character = new GameObject(name);
            character.transform.SetParent(transform);
            character.transform.position = new Vector3(x, characterY, 0f);
            character.transform.localScale = Vector3.one * characterScale;

            SpriteRenderer renderer = character.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderAssetFactory.CreateCharacterSprite(bodyColor, accentColor);
            renderer.sortingOrder = -2;

            CharacterBopController bop = character.AddComponent<CharacterBopController>();
            bop.isPlayerControlled = isPlayerControlled;
        }

        private void SetUpConductor()
        {
            GameObject conductorObject = new GameObject("Conductor");
            AudioSource audioSource = conductorObject.AddComponent<AudioSource>();
            audioSource.clip = level.songClip;
            audioSource.playOnAwake = false;

            float bpmToUse = level.bpm;
            TempoMap tempoMap = null;

            if (level.autoDetectTempoMap)
            {
                tempoMap = AutoChartGenerator.DetectTempoMap(level.songClip);
                if (tempoMap != null && tempoMap.changePoints.Count > 0)
                {
                    bpmToUse = tempoMap.changePoints[0].bpm;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[GameplaySessionController] Tempo map detection failed; falling back.");
                }
            }

            if (tempoMap == null && level.autoDetectBpm)
            {
                float? detectedBpm = AutoChartGenerator.DetectBpm(level.songClip);
                if (detectedBpm.HasValue)
                {
                    bpmToUse = detectedBpm.Value;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[GameplaySessionController] BPM auto-detection failed; falling back to Level's Bpm field.");
                }
            }

            conductor = conductorObject.AddComponent<Conductor>();
            conductor.bpm = bpmToUse;
            conductor.tempoMap = tempoMap;
            conductor.songOffsetSeconds = level.songOffsetSeconds;
        }

        private void SetUpScoreManager()
        {
            if (ScoreManager.Instance == null)
            {
                new GameObject("ScoreManager").AddComponent<ScoreManager>();
            }
            ScoreManager.Instance.ResetScore();
        }

        private void SetUpHealthManager()
        {
            if (HealthManager.Instance == null)
            {
                new GameObject("HealthManager").AddComponent<HealthManager>();
            }
            HealthManager.Instance.ResetHealth();
            HealthManager.Instance.OnHealthDepleted += HandleHealthDepleted;
        }

        private void SetUpInputHandler()
        {
            if (FindFirstObjectByType<NoteInputHandler>() == null)
            {
                new GameObject("NoteInputHandler").AddComponent<NoteInputHandler>();
            }
        }

        private void SetUpNoteSpawner()
        {
            NoteSpawner spawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();
            spawner.arrowSprite = arrowSprite;
            spawner.laneSpacing = laneSpacing;
            spawner.strumlineY = strumlineY;
            spawner.spawnY = spawnY;
            spawner.noteSize = noteSize;
            spawner.leadTime = level.noteTravelLeadTime;

            if (!string.IsNullOrEmpty(level.chartResourcePath))
            {
                spawner.chartResourcePath = level.chartResourcePath;
                spawner.autoGenerateFromAudio = false;
            }
            else
            {
                spawner.autoGenerateFromAudio = true;
                spawner.songClip = level.songClip;
                spawner.autoChartSettings = level.autoChartSettings;
            }
        }

        private void Update()
        {
            if (countingDown)
            {
                countdownRemaining -= Time.deltaTime;
                if (countdownRemaining <= 0f)
                {
                    countingDown = false;
                    conductor.StartSong();
                    songStarted = true;
                }
                return;
            }

            if (songStarted && !songEnded && conductor.SongHasStarted &&
                conductor.SongPositionInSeconds >= conductor.ClipLengthSeconds && Note.ActiveNotes.Count == 0)
            {
                songEnded = true;
                FinishLevel(true);
            }
        }

        private void HandleHealthDepleted()
        {
            if (songEnded)
            {
                return;
            }

            songEnded = true;
            FinishLevel(false);
        }

        private void FinishLevel(bool won)
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            int maxCombo = ScoreManager.Instance != null ? ScoreManager.Instance.MaxCombo : 0;

            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.OnHealthDepleted -= HandleHealthDepleted;
            }

            GameFlowManager.Instance.FinishLevel(won, score, maxCombo);
        }

        private void OnGUI()
        {
            if (countingDown)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                style.normal.textColor = Color.white;

                int display = Mathf.CeilToInt(countdownRemaining);
                string text = display > 0 ? display.ToString() : "GO!";
                GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height / 2f - 40f, 200f, 80f), text, style);
            }

            if (level != null)
            {
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
                nameStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(20, Screen.height - 40, 400, 30), level.levelName, nameStyle);
            }
        }
    }
}

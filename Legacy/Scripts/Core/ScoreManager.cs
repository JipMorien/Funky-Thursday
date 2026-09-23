using System;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Timing-accuracy tiers for a hit, tightest to loosest. Miss covers both
    /// a note that timed out unhit and a hit outside even the loosest window
    /// (the latter never reaches RegisterHit - see NoteInputHandler, which
    /// treats "no note in range" as an unjudged ghost tap, not a scored miss).
    /// </summary>
    public enum HitJudgment
    {
        Perfect,
        Good,
        Okay,
        Miss,
    }

    /// <summary>
    /// Tracks the player's running score and combo for the current song, and
    /// how many points each judgment tier is worth. Singleton like Conductor,
    /// so input handling and notes can reach it without extra wiring. Draws a
    /// simple always-on score/combo readout via OnGUI - no Canvas/TMP setup
    /// required to see it working.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        /// <summary>Fired for every judgment (hits and misses alike) as it's registered, for HUD popups etc. to react to.</summary>
        public static event Action<HitJudgment> OnJudgment;

        [Header("Points Per Judgment")]
        public int perfectPoints = 100;
        public int goodPoints = 70;
        public int okayPoints = 50;
        public int missPoints = 0;

        [Header("Read-Only Status")]
        [SerializeField] private int score;
        [SerializeField] private int combo;
        [SerializeField] private int maxCombo;

        [Header("Combo Pop")]
        [Tooltip("Extra font size added to the combo readout at the instant combo increases.")]
        public float comboPopSize = 16f;

        [Tooltip("How quickly the combo pop decays back to its resting size.")]
        public float comboPopDecaySpeed = 10f;

        public int Score => score;
        public int Combo => combo;
        public int MaxCombo => maxCombo;

        private float comboPopAmount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Adds this judgment's points to the score and updates the combo (reset to 0 on Miss).</summary>
        public void RegisterHit(HitJudgment judgment)
        {
            int points = PointsFor(judgment);
            score += points;

            if (judgment == HitJudgment.Miss)
            {
                combo = 0;
            }
            else
            {
                combo++;
                maxCombo = Mathf.Max(maxCombo, combo);
                comboPopAmount = 1f;
            }

            UnityEngine.Debug.Log($"[ScoreManager] {judgment} (+{points}) - Score: {score}, Combo: {combo}");
            OnJudgment?.Invoke(judgment);
        }

        public void ResetScore()
        {
            score = 0;
            combo = 0;
            maxCombo = 0;
        }

        private int PointsFor(HitJudgment judgment)
        {
            switch (judgment)
            {
                case HitJudgment.Perfect: return perfectPoints;
                case HitJudgment.Good: return goodPoints;
                case HitJudgment.Okay: return okayPoints;
                default: return missPoints;
            }
        }

        private void Update()
        {
            comboPopAmount = Mathf.MoveTowards(comboPopAmount, 0f, Time.deltaTime * comboPopDecaySpeed);
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(20, 20, 300, 30), $"Score: {score}", style);

            GUIStyle comboStyle = new GUIStyle(style)
            {
                fontSize = 24 + Mathf.RoundToInt(comboPopAmount * comboPopSize),
            };
            GUI.Label(new Rect(20, 50, 300, 40), $"Combo: {combo}", comboStyle);
        }
    }
}

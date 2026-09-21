using System;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Tracks the player's health for the current song: player hits heal it,
    /// player misses drain it, and hitting zero ends the song in a loss.
    /// Opponent/CPU notes never touch health - see Note.Update, which only
    /// reports misses for player-owned notes. Singleton like Conductor and
    /// ScoreManager, and draws its own OnGUI bar for the same reason
    /// ScoreManager does: no Canvas/TMP setup required to see it working.
    /// </summary>
    public class HealthManager : MonoBehaviour
    {
        public static HealthManager Instance { get; private set; }

        [Header("Health Range")]
        public float maxHealth = 1f;
        public float startHealth = 0.5f;

        [Header("Health Delta Per Judgment")]
        public float perfectGain = 0.025f;
        public float goodGain = 0.015f;
        public float okayGain = 0.005f;
        public float missLoss = 0.08f;

        [Header("Read-Only Status")]
        [SerializeField] private float health;

        private bool depleted;

        public float Health => health;
        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

        /// <summary>Fired once, the first time health reaches zero.</summary>
        public event Action OnHealthDepleted;

        private static Texture2D fillTexture;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResetHealth();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ResetHealth()
        {
            health = Mathf.Clamp(startHealth, 0f, maxHealth);
            depleted = false;
        }

        /// <summary>Applies this judgment's health delta and fires OnHealthDepleted the moment health first hits zero.</summary>
        public void ApplyJudgment(HitJudgment judgment)
        {
            float delta = judgment switch
            {
                HitJudgment.Perfect => perfectGain,
                HitJudgment.Good => goodGain,
                HitJudgment.Okay => okayGain,
                _ => -missLoss,
            };

            health = Mathf.Clamp(health + delta, 0f, maxHealth);

            if (health <= 0f && !depleted)
            {
                depleted = true;
                OnHealthDepleted?.Invoke();
            }
        }

        private void OnGUI()
        {
            const float width = 300f;
            const float barHeight = 20f;
            float x = (Screen.width - width) / 2f;
            const float y = 20f;

            GUI.Box(new Rect(x, y, width, barHeight), GUIContent.none);
            DrawFilledRect(new Rect(x, y, width * HealthFraction, barHeight), Color.Lerp(Color.red, Color.green, HealthFraction));
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
            if (fillTexture == null)
            {
                fillTexture = new Texture2D(1, 1);
            }

            fillTexture.SetPixel(0, 0, color);
            fillTexture.Apply();
            GUI.DrawTexture(rect, fillTexture);
        }
    }
}

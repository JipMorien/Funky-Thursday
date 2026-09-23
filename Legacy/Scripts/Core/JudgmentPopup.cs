using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Pops up a big "PERFECT!" / "GOOD" / "OKAY" / "BAD" readout for every
    /// judgment as it's registered (ScoreManager.OnJudgment), punching in
    /// with a little overshoot, holding, then floating up and fading out.
    /// OnGUI-driven like the rest of this project's HUD - no Canvas/TMP
    /// setup required to see it working.
    /// </summary>
    public class JudgmentPopup : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Total seconds the popup stays on screen, punch-in through fade-out.")]
        public float totalDuration = 0.6f;
        [Range(0f, 1f)] public float punchInEnd = 0.15f;
        [Range(0f, 1f)] public float settleEnd = 0.25f;
        [Range(0f, 1f)] public float fadeStart = 0.65f;

        [Header("Look")]
        public float baseFontSize = 48f;
        public float punchOvershoot = 1.25f;
        public float floatUpPixels = 40f;
        public float screenYFraction = 0.32f;

        [Header("Colors")]
        public Color perfectColor = new Color(1f, 0.85f, 0.1f);
        public Color goodColor = new Color(0.35f, 0.9f, 0.4f);
        public Color okayColor = new Color(1f, 0.6f, 0.15f);
        public Color badColor = new Color(0.95f, 0.2f, 0.25f);

        private string currentLabel;
        private Color currentColor;
        private float timer;

        private void OnEnable()
        {
            ScoreManager.OnJudgment += HandleJudgment;
        }

        private void OnDisable()
        {
            ScoreManager.OnJudgment -= HandleJudgment;
        }

        private void HandleJudgment(HitJudgment judgment)
        {
            currentLabel = LabelFor(judgment);
            currentColor = ColorFor(judgment);
            timer = 0f;
        }

        private static string LabelFor(HitJudgment judgment)
        {
            switch (judgment)
            {
                case HitJudgment.Perfect: return "PERFECT!";
                case HitJudgment.Good: return "GOOD";
                case HitJudgment.Okay: return "OKAY";
                default: return "BAD";
            }
        }

        private Color ColorFor(HitJudgment judgment)
        {
            switch (judgment)
            {
                case HitJudgment.Perfect: return perfectColor;
                case HitJudgment.Good: return goodColor;
                case HitJudgment.Okay: return okayColor;
                default: return badColor;
            }
        }

        private void Update()
        {
            if (timer < totalDuration)
            {
                timer += Time.deltaTime;
            }
        }

        private float ScaleForT(float t)
        {
            if (t < punchInEnd)
            {
                return Mathf.Lerp(0f, punchOvershoot, t / Mathf.Max(0.0001f, punchInEnd));
            }
            if (t < settleEnd)
            {
                return Mathf.Lerp(punchOvershoot, 1f, (t - punchInEnd) / Mathf.Max(0.0001f, settleEnd - punchInEnd));
            }
            return 1f;
        }

        private float AlphaForT(float t)
        {
            if (t < fadeStart)
            {
                return 1f;
            }
            return Mathf.Lerp(1f, 0f, (t - fadeStart) / Mathf.Max(0.0001f, 1f - fadeStart));
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(currentLabel) || timer >= totalDuration)
            {
                return;
            }

            float t = timer / totalDuration;
            float alpha = AlphaForT(t);
            if (alpha <= 0f)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseFontSize * ScaleForT(t)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            Color color = currentColor;
            color.a = alpha;
            style.normal.textColor = color;

            float y = Screen.height * screenYFraction - floatUpPixels * t;
            GUI.Label(new Rect(Screen.width / 2f - 250f, y, 500f, 100f), currentLabel, style);
        }
    }
}

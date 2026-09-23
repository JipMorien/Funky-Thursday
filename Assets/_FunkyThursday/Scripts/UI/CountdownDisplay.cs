using FunkyThursday.Core;
using FunkyThursday.UI.Menus;
using UnityEngine;

namespace FunkyThursday.UI
{
    /// <summary>FNF-style "3, 2, 1, GO!" on the last beats of the count-in, each word popping and fading.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CountdownDisplay : MonoBehaviour
    {
        [SerializeField] Conductor conductor;
        [SerializeField, Min(1f)] float pixelSize = 18f;

        PixelText _label;
        float _age = 99f;
        float _life = 0.5f;

        void Awake()
        {
            _label = UIFactory.Text("Countdown", transform, " ", pixelSize, MenuPalette.Bone);
            _label.enabled = false;
        }

        void OnEnable()
        {
            if (conductor != null) conductor.BeatHit += HandleBeat;
        }

        void OnDisable()
        {
            if (conductor != null) conductor.BeatHit -= HandleBeat;
        }

        void HandleBeat(int beat)
        {
            string word;
            switch (beat)
            {
                case -3: word = "3"; break;
                case -2: word = "2"; break;
                case -1: word = "1"; break;
                case 0: word = "GO!"; break;
                default: return;
            }

            _label.Text = word;
            _label.color = beat == 0 ? MenuPalette.Blood : MenuPalette.Bone;
            _label.FitToText();
            _label.enabled = true;
            _age = 0f;
            _life = conductor != null ? (float)conductor.Crochet * 0.9f : 0.5f;
            MenuSfx.Play(beat == 0 ? MenuSfx.Sound.Confirm : MenuSfx.Sound.Scroll);
        }

        void Update()
        {
            if (!_label.enabled) return;
            if (conductor != null && conductor.State != Conductor.PlaybackState.Playing && _age > 0f)
            {
                _label.enabled = conductor.State == Conductor.PlaybackState.Paused;
                return;
            }

            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _life);
            _label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, Mathf.Sqrt(t));
            Color c = _label.color;
            c.a = 1f - t * t;
            _label.color = c;
            if (t >= 1f) _label.enabled = false;
        }
    }
}

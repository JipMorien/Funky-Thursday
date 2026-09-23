using FunkyThursday.Core;
using FunkyThursday.Gameplay;
using FunkyThursday.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace FunkyThursday.UI
{
    /// <summary>
    /// FNF-style tug-of-war bar on a Canvas. The opponent's colour fills from the left, the
    /// player's from the right, and both head icons ride the split point. Icons bump on every beat
    /// and switch to their losing face when their side is nearly gone. Builds its own children.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] HealthSystem health;
        [SerializeField] Conductor conductor;

        [Header("Layout (canvas pixels)")]
        [SerializeField] Vector2 barSize = new Vector2(600f, 18f);
        [SerializeField, Min(0f)] float border = 4f;
        [SerializeField, Min(16f)] float iconSize = 96f;

        [Header("Feel")]
        [SerializeField, Min(0.1f)] float smoothing = 12f;
        [SerializeField, Range(0f, 0.5f)] float losingThreshold = 0.2f;
        [SerializeField, Min(1f)] float beatBump = 1.2f;

        RectTransform _opponentFill;
        RectTransform _playerFill;
        RectTransform _opponentIconRect;
        RectTransform _playerIconRect;
        Image _opponentFillImage;
        Image _playerFillImage;
        Image _opponentIcon;
        Image _playerIcon;
        Sprite _opponentNormal, _opponentLosing, _playerNormal, _playerLosing;

        float _shown = 0.5f;
        float _bump = 1f;

        void Awake() => Build();

        void OnEnable()
        {
            if (conductor != null) conductor.BeatHit += HandleBeat;
        }

        void OnDisable()
        {
            if (conductor != null) conductor.BeatHit -= HandleBeat;
        }

        /// <summary>Takes colours and icons from the two characters.</summary>
        public void SetFighters(CharacterPuppet opponent, CharacterPuppet player)
        {
            _opponentFillImage.color = Tone(opponent.Eyes);
            _playerFillImage.color = Tone(player.Eyes);
            _opponentNormal = opponent.GetIcon(false);
            _opponentLosing = opponent.GetIcon(true);
            _playerNormal = player.GetIcon(false);
            _playerLosing = player.GetIcon(true);
            Layout();
        }

        /// <summary>Jump straight to the current health (after a restart) instead of easing.</summary>
        public void Snap()
        {
            _shown = health != null ? health.Value : 0.5f;
            Layout();
        }

        void Update()
        {
            if (health == null) return;

            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            _shown = Mathf.Lerp(_shown, health.Value, t);
            _bump = Mathf.Lerp(_bump, 1f, t);
            Layout();
        }

        void HandleBeat(int beat)
        {
            if (beat >= 0) _bump = beatBump;
        }

        void Layout()
        {
            if (_opponentFill == null) return;

            float split = 1f - _shown; // opponent owns [0, split], player owns [split, 1]
            _opponentFill.anchorMax = new Vector2(split, 1f);
            _playerFill.anchorMin = new Vector2(split, 0f);

            PlaceIcon(_opponentIconRect, split, -0.35f);
            PlaceIcon(_playerIconRect, split, 0.35f);

            _opponentIcon.sprite = _shown > 1f - losingThreshold ? _opponentLosing : _opponentNormal;
            _playerIcon.sprite = _shown < losingThreshold ? _playerLosing : _playerNormal;
            _opponentIcon.enabled = _opponentIcon.sprite != null;
            _playerIcon.enabled = _playerIcon.sprite != null;
        }

        void PlaceIcon(RectTransform icon, float split, float side)
        {
            icon.anchorMin = icon.anchorMax = new Vector2(split, 0.5f);
            icon.anchoredPosition = new Vector2(side * iconSize, 0f);
            icon.localScale = new Vector3(_bump, _bump, 1f); // the art already faces the other fighter
        }

        void Build()
        {
            var root = (RectTransform)transform;
            root.sizeDelta = barSize + Vector2.one * (border * 2f);

            Image frame = GetComponent<Image>();
            if (frame == null) frame = gameObject.AddComponent<Image>();
            frame.color = GothicPalette.Ash;
            frame.raycastTarget = false;

            RectTransform inner = CreateRect("Fill Area", root);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = Vector2.one * border;
            inner.offsetMax = -Vector2.one * border;

            _opponentFill = CreateRect("Opponent Fill", inner);
            _opponentFillImage = AddImage(_opponentFill, GothicPalette.Darken(GothicPalette.OpponentEyes, 0.3f));
            _opponentFill.anchorMin = Vector2.zero;
            _opponentFill.offsetMin = _opponentFill.offsetMax = Vector2.zero;

            _playerFill = CreateRect("Player Fill", inner);
            _playerFillImage = AddImage(_playerFill, GothicPalette.Darken(GothicPalette.PlayerEyes, 0.3f));
            _playerFill.anchorMax = Vector2.one;
            _playerFill.offsetMin = _playerFill.offsetMax = Vector2.zero;

            _opponentIconRect = CreateRect("Opponent Icon", inner);
            _opponentIconRect.sizeDelta = Vector2.one * iconSize;
            _opponentIcon = AddImage(_opponentIconRect, Color.white);
            _opponentIcon.preserveAspect = true;

            _playerIconRect = CreateRect("Player Icon", inner);
            _playerIconRect.sizeDelta = Vector2.one * iconSize;
            _playerIcon = AddImage(_playerIconRect, Color.white);
            _playerIcon.preserveAspect = true;

            Layout();
        }

        static Color Tone(Color eyes) => GothicPalette.Darken(eyes, 0.3f);

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}

using System;
using FunkyThursday.Gameplay;
using FunkyThursday.UI.Menus;
using UnityEngine;

namespace FunkyThursday.UI
{
    /// <summary>
    /// Shown when a round ends. Cleared: title, stats, Continue / Retry. Perished: Retry / Level Select.
    /// Phase 5 adds accuracy and the Gothic Rank to the stats block.
    /// </summary>
    public sealed class RoundEndScreen : MonoBehaviour
    {
        public event Action RetryRequested;
        public event Action ExitRequested;

        MenuList _list;
        PixelText _title;
        PixelText _song;
        PixelText _stats;
        PixelText _newBest;
        bool _cleared;
        bool _built;

        public bool IsOpen => _built && gameObject.activeSelf;

        void Awake() => Build();

        public void Show(bool cleared, RoundStats stats, string songTitle, bool newBest)
        {
            Build();
            _cleared = cleared;

            _title.Text = cleared ? "SONG CLEARED" : "PERISHED";
            _title.color = cleared ? MenuPalette.Bone : MenuPalette.Blood;
            _title.FitToText();

            _song.Text = songTitle ?? "";
            _song.FitToText();

            _stats.Text = $"PERFECT {stats.perfects}   GOOD {stats.goods}   MISS {stats.misses}\nMAX COMBO {stats.maxCombo}";
            _stats.FitToText();

            _newBest.enabled = newBest && cleared;

            _list.SetItems(cleared ? new[] { "CONTINUE", "RETRY" } : new[] { "RETRY", "LEVEL SELECT" });
            _list.Select(0, notify: false, sound: false);
            gameObject.SetActive(true);
            _list.Focus();
        }

        public void Hide()
        {
            Build();
            _list.Unfocus();
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (_newBest.enabled) _newBest.color = Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? MenuPalette.Blood : MenuPalette.Bone;
        }

        void Build()
        {
            if (_built) return;
            _built = true;

            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            UIFactory.Panel("Dim", root, MenuPalette.Dim);

            _title = UIFactory.Text("Title", root, " ", 14f, MenuPalette.Bone);
            Top(_title.rectTransform, -170f);
            _song = UIFactory.Text("Song", root, " ", 5f, MenuPalette.Ash);
            Top(_song.rectTransform, -300f);
            _stats = UIFactory.Text("Stats", root, " ", 5f, MenuPalette.Bone);
            Top(_stats.rectTransform, -390f);
            _newBest = UIFactory.Text("New Best", root, "NEW BEST!", 6f, MenuPalette.Blood);
            Top(_newBest.rectTransform, -520f);

            RectTransform listRect = UIFactory.Anchored("End List", root, new Vector2(0.5f, 1f), new Vector2(0f, -640f), new Vector2(900f, 220f));
            _list = listRect.gameObject.AddComponent<MenuList>();
            _list.Configure(7f, 92f);
            _list.SetItems(new[] { " " });
            _list.Confirmed += HandleConfirm;
            _list.BackRequested += () => ExitRequested?.Invoke();
        }

        void HandleConfirm(int index)
        {
            bool retry = _cleared ? index == 1 : index == 0;
            if (retry) RetryRequested?.Invoke();
            else ExitRequested?.Invoke();
        }

        static void Top(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}

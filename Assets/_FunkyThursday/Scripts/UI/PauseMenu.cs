using System;
using FunkyThursday.UI.Menus;
using UnityEngine;

namespace FunkyThursday.UI
{
    /// <summary>In-game pause overlay: Resume, Restart Song, Exit to Level Select. Esc resumes.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        const int Resume = 0;
        const int Restart = 1;
        const int Exit = 2;

        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action ExitRequested;

        MenuList _list;
        PixelText _song;
        bool _built;

        public bool IsOpen => _built && gameObject.activeSelf;

        void Awake() => Build();

        public void Show(string songTitle)
        {
            Build();
            _song.Text = songTitle ?? "";
            _song.FitToText();
            gameObject.SetActive(true);
            _list.Select(Resume, notify: false, sound: false);
            _list.Focus();
        }

        public void Hide()
        {
            Build();
            _list.Unfocus();
            gameObject.SetActive(false);
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

            PixelText title = UIFactory.Text("Title", root, "PAUSED", 16f, MenuPalette.Bone);
            Top(title.rectTransform, -200f);
            _song = UIFactory.Text("Song", root, " ", 5f, MenuPalette.Ash);
            Top(_song.rectTransform, -350f);

            RectTransform listRect = UIFactory.Anchored("Pause List", root, new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(1000f, 300f));
            _list = listRect.gameObject.AddComponent<MenuList>();
            _list.Configure(7f, 92f);
            _list.SetItems(new[] { "RESUME", "RESTART SONG", "EXIT TO LEVEL SELECT" });
            _list.Confirmed += HandleConfirm;
            _list.BackRequested += () => ResumeRequested?.Invoke();
        }

        void HandleConfirm(int index)
        {
            switch (index)
            {
                case Resume: ResumeRequested?.Invoke(); break;
                case Restart: RestartRequested?.Invoke(); break;
                case Exit: ExitRequested?.Invoke(); break;
            }
        }

        static void Top(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}

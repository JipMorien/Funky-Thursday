using UnityEngine;

namespace FunkyThursday.UI.Menus
{
    /// <summary>Title screen: the logo pulses on the beat of the menu music; Play, Options, Quit.</summary>
    public sealed class MainMenuScreen : MenuScreen
    {
        const int Play = 0;
        const int Options = 1;
        const int Quit = 2;

        PixelText _titleTop;
        PixelText _titleBottom;
        MenuList _list;

        protected override void Build()
        {
            _titleTop = UIFactory.Text("Title Funky", Root, "FUNKY", 26f, MenuPalette.Bone);
            Place(_titleTop.rectTransform, -150f);
            _titleBottom = UIFactory.Text("Title Thursday", Root, "THURSDAY", 26f, MenuPalette.Blood);
            Place(_titleBottom.rectTransform, -370f);

            PixelText tagline = UIFactory.Text("Tagline", Root, "A GOTHIC RHYTHM DUEL IN FOUR NIGHTS", 5f, MenuPalette.Ash);
            Place(tagline.rectTransform, -590f);

            RectTransform listRect = UIFactory.Anchored("Main List", Root, new Vector2(0.5f, 1f), new Vector2(0f, -700f), new Vector2(800f, 300f));
            _list = listRect.gameObject.AddComponent<MenuList>();
            _list.Configure(8f, 96f);
            bool canQuit = Application.platform != RuntimePlatform.WebGLPlayer;
            _list.SetItems(canQuit ? new[] { "PLAY", "OPTIONS", "QUIT" } : new[] { "PLAY", "OPTIONS" });
            _list.Confirmed += HandleConfirm;

            PixelText footer = UIFactory.Text("Footer", Root, "ARROWS / WASD MOVE · ENTER SELECT · ESC BACK", 3f, MenuPalette.Faint);
            RectTransform f = footer.rectTransform;
            f.anchorMin = f.anchorMax = f.pivot = new Vector2(0.5f, 0f);
            f.anchoredPosition = new Vector2(0f, 48f);
        }

        public override void Open()
        {
            base.Open();
            _list.Focus();
            Menu.PlayTitleMusic();
        }

        public override void Close()
        {
            _list.Unfocus();
            base.Close();
        }

        void Update()
        {
            // Logo pops on each beat and settles.
            float pulse = 1f + 0.05f * Mathf.Pow(1f - Menu.BeatFraction, 3f);
            _titleTop.rectTransform.localScale = Vector3.one * pulse;
            _titleBottom.rectTransform.localScale = Vector3.one * pulse;
        }

        void HandleConfirm(int index)
        {
            switch (index)
            {
                case Play:
                    Menu.OpenLevelSelect();
                    break;
                case Options:
                    Menu.OpenOptions();
                    break;
                case Quit:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;
            }
        }

        static void Place(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}

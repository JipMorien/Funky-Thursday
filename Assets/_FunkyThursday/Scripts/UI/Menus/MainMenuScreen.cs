using System.Collections;
using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.UI.Menus
{
    /// <summary>
    /// Title screen: the logo pulses on the beat of the menu music; Play, Options, Quit.
    /// Typing Up Up Down Down Left Right Left Right reveals the secret night and jumps straight to it.
    /// </summary>
    public sealed class MainMenuScreen : MenuScreen
    {
        enum Arrow { Up, Down, Left, Right }

        static readonly Arrow[] SecretCode =
        {
            Arrow.Up, Arrow.Up, Arrow.Down, Arrow.Down, Arrow.Left, Arrow.Right, Arrow.Left, Arrow.Right
        };

        const int Play = 0;
        const int Options = 1;
        const int Quit = 2;

        PixelText _titleTop;
        PixelText _titleBottom;
        MenuList _list;
        PixelText _tagline;
        int _codeProgress;
        bool _revealing;

        protected override void Build()
        {
            _titleTop = UIFactory.Text("Title Funky", Root, "FUNKY", 26f, MenuPalette.Bone);
            Place(_titleTop.rectTransform, -150f);
            _titleBottom = UIFactory.Text("Title Thursday", Root, "THURSDAY", 26f, MenuPalette.Blood);
            Place(_titleBottom.rectTransform, -370f);

            _tagline = UIFactory.Text("Tagline", Root, Tagline, 5f, MenuPalette.Ash);
            Place(_tagline.rectTransform, -590f);

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

        const string Tagline = "A THURSDAY. BUT FUNKY.";

        public override void Open()
        {
            base.Open();
            _codeProgress = 0;
            _revealing = false;
            _tagline.Text = Tagline;
            _tagline.color = MenuPalette.Ash;
            _tagline.FitToText();
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
            TrackSecretCode();

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

        void TrackSecretCode()
        {
            if (_revealing || Menu.Library == null || Menu.Library.SecretIndex < 0) return;

            Arrow? pressed = MenuInput.UpPressed ? Arrow.Up
                : MenuInput.DownPressed ? Arrow.Down
                : MenuInput.LeftPressed ? Arrow.Left
                : MenuInput.RightPressed ? Arrow.Right
                : (Arrow?)null;
            if (pressed == null) return;

            if (pressed == SecretCode[_codeProgress]) _codeProgress++;
            else _codeProgress = pressed == SecretCode[0] ? 1 : 0;

            if (_codeProgress == SecretCode.Length) StartCoroutine(Reveal());
        }

        IEnumerator Reveal()
        {
            _revealing = true;
            _list.Unfocus();
            SaveData.RevealSecret();
            MenuSfx.Play(MenuSfx.Sound.Secret);

            _tagline.Text = "SOMETHING STIRS BENEATH THE FOURTH NIGHT";
            _tagline.color = MenuPalette.Spectral;
            _tagline.FitToText();

            for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime)
            {
                _tagline.enabled = Mathf.Repeat(t, 0.2f) < 0.12f;
                yield return null;
            }
            _tagline.enabled = true;
            Menu.OpenSecret();
        }

        static void Place(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}

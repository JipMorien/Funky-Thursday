using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.UI.Menus
{
    /// <summary>Options: note offset, volume, timing leniency, ghost tapping, unlock all. Left/Right change a value.</summary>
    public sealed class OptionsScreen : MenuScreen
    {
        const int Offset = 0;
        const int Volume = 1;
        const int Timing = 2;
        const int Ghost = 3;
        const int Unlock = 4;
        const int Back = 5;

        MenuList _list;
        PixelText _hint;

        protected override void Build()
        {
            PixelText header = UIFactory.Text("Header", Root, "OPTIONS", 12f, MenuPalette.Bone);
            RectTransform h = header.rectTransform;
            h.anchorMin = h.anchorMax = h.pivot = new Vector2(0.5f, 1f);
            h.anchoredPosition = new Vector2(0f, -120f);

            RectTransform listRect = UIFactory.Anchored("Options List", Root, new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(1200f, 600f));
            _list = listRect.gameObject.AddComponent<MenuList>();
            _list.Configure(6f, 96f);
            _list.SetItems(new[] { "", "", "", "", "", "BACK" });
            _list.Adjusted += HandleAdjust;
            _list.Confirmed += HandleConfirm;
            _list.SelectionChanged += _ => UpdateHint();
            _list.BackRequested += () => Menu.OpenMain();

            _hint = UIFactory.Text("Hint", Root, " ", 4f, MenuPalette.Ash);
            RectTransform hint = _hint.rectTransform;
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(0.5f, 0f);
            hint.anchoredPosition = new Vector2(0f, 110f);
        }

        public override void Open()
        {
            base.Open();
            RefreshLabels();
            _list.Select(0, notify: false, sound: false);
            _list.Focus();
            UpdateHint();
        }

        public override void Close()
        {
            _list.Unfocus();
            base.Close();
        }

        void HandleAdjust(int index, int direction)
        {
            switch (index)
            {
                case Offset:
                    GameSettings.AudioOffsetMs += direction * 5f;
                    break;
                case Volume:
                    GameSettings.Volume = Mathf.Round((GameSettings.Volume + direction * 0.1f) * 10f) / 10f;
                    break;
                case Timing:
                    int modes = (int)TimingMode.Demo + 1;
                    GameSettings.Timing = (TimingMode)(((int)GameSettings.Timing + direction + modes) % modes);
                    UpdateHint();
                    break;
                case Ghost:
                    GameSettings.GhostTapping = !GameSettings.GhostTapping;
                    break;
                case Unlock:
                    GameSettings.UnlockAll = !GameSettings.UnlockAll;
                    break;
            }
            RefreshLabels();
        }

        void HandleConfirm(int index)
        {
            if (index == Back) Menu.OpenMain();
            else if (index == Timing || index == Ghost || index == Unlock) HandleAdjust(index, 1);
        }

        void RefreshLabels()
        {
            float offset = GameSettings.AudioOffsetMs;
            _list.SetLabel(Offset, $"NOTE OFFSET   {(offset > 0 ? "+" : "")}{offset:0} MS");
            _list.SetLabel(Volume, $"VOLUME   {Mathf.RoundToInt(GameSettings.Volume * 100f)}%");
            _list.SetLabel(Timing, $"TIMING   {TimingProfile.Label(GameSettings.Timing)}");
            _list.SetLabel(Ghost, $"GHOST TAPPING   {(GameSettings.GhostTapping ? "ON" : "OFF")}");
            _list.SetLabel(Unlock, $"UNLOCK ALL NIGHTS   {(GameSettings.UnlockAll ? "ON" : "OFF")}");
        }

        static string TimingHint(TimingMode mode)
        {
            switch (mode)
            {
                case TimingMode.Relaxed: return "WIDER HIT WINDOWS · MISSES HURT LESS";
                case TimingMode.Demo: return "VERY WIDE WINDOWS · NO DRAIN · FOR SHOWCASES";
                default: return "THE REAL CHALLENGE · 45 / 90 / 135 MS";
            }
        }

        void UpdateHint()
        {
            string text;
            switch (_list.Selected)
            {
                case Offset: text = "LEFT / RIGHT · RAISE IT IF YOUR HITS LAND LATE"; break;
                case Volume: text = "LEFT / RIGHT · MASTER VOLUME"; break;
                case Timing: text = TimingHint(GameSettings.Timing); break;
                case Ghost: text = "ON: PRESSING WITH NO NOTE NEARBY IS FREE"; break;
                case Unlock: text = "PLAY ANY NIGHT WITHOUT CLEARING THE ONE BEFORE"; break;
                default: text = "ENTER OR ESC TO RETURN"; break;
            }
            _hint.Text = text;
            _hint.FitToText();
        }
    }
}

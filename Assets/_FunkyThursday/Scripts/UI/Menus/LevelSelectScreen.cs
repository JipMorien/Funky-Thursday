using System.Collections.Generic;
using FunkyThursday.Charts;
using FunkyThursday.Core;
using FunkyThursday.Data;
using FunkyThursday.Placeholders;
using UnityEngine;
using UnityEngine.UI;

namespace FunkyThursday.UI.Menus
{
    /// <summary>
    /// Level Select: the nights on the left, the highlighted opponent on the right (icon, name,
    /// difficulty, tempo, best result). The backdrop and music preview follow the selection. Locked
    /// nights show as ??? until the previous one is cleared; a secret night is left out of the list
    /// entirely until it has been revealed. List rows map to library indices through _rows.
    /// </summary>
    public sealed class LevelSelectScreen : MenuScreen
    {
        MenuList _list;
        Image _icon;
        PixelText _opponent;
        PixelText _song;
        PixelText _difficulty;
        PixelText _details;
        PixelText _best;
        PixelText _prompt;
        int _pendingIndex = -1;
        readonly List<int> _rows = new List<int>();

        protected override void Build()
        {
            PixelText header = UIFactory.Text("Header", Root, "CHOOSE YOUR NIGHT", 9f, MenuPalette.Bone, TextAnchor.UpperLeft);
            RectTransform h = header.rectTransform;
            h.anchorMin = h.anchorMax = h.pivot = new Vector2(0f, 1f);
            h.anchoredPosition = new Vector2(120f, -90f);

            RectTransform listRect = UIFactory.Anchored("Song List", Root, new Vector2(0f, 1f), new Vector2(128f, -260f), new Vector2(860f, 400f));
            _list = listRect.gameObject.AddComponent<MenuList>();
            _list.Configure(6f, 96f, TextAnchor.MiddleLeft);
            _list.SelectionChanged += row => ShowDetails(LibraryIndex(row));
            _list.Confirmed += row => Menu.StartSong(LibraryIndex(row));
            _list.BackRequested += () => Menu.OpenMain();

            // Detail panel on the right.
            RectTransform panel = UIFactory.Anchored("Details", Root, new Vector2(1f, 0.5f), new Vector2(-120f, 20f), new Vector2(760f, 760f), new Vector2(1f, 0.5f));
            Image panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = MenuPalette.Dim;
            panelBg.raycastTarget = false;

            _icon = UIFactory.Icon("Opponent Icon", panel, null, 256f);
            Top(_icon.rectTransform, -60f);
            _opponent = Line(panel, "Opponent", 6f, MenuPalette.Bone, -350f);
            _song = Line(panel, "Song", 4f, MenuPalette.Ash, -420f);
            _difficulty = Line(panel, "Difficulty", 5f, MenuPalette.Moss, -480f);
            _details = Line(panel, "Details", 4f, MenuPalette.Ash, -545f);
            _best = Line(panel, "Best", 4f, MenuPalette.Ash, -605f);
            _prompt = Line(panel, "Prompt", 4f, MenuPalette.Blood, -690f);

            PixelText footer = UIFactory.Text("Footer", Root, "ENTER PLAY · ESC BACK", 3f, MenuPalette.Faint);
            RectTransform f = footer.rectTransform;
            f.anchorMin = f.anchorMax = f.pivot = new Vector2(0.5f, 0f);
            f.anchoredPosition = new Vector2(0f, 48f);
        }

        public override void Open()
        {
            base.Open();
            Refresh();
            if (_pendingIndex >= 0)
            {
                _list.Select(RowOf(_pendingIndex), notify: false, sound: false);
                _pendingIndex = -1;
            }
            _list.Focus();
            ShowDetails(LibraryIndex(_list.Selected));
        }

        public override void Close()
        {
            _list.Unfocus();
            base.Close();
        }

        /// <summary>Highlights a song by library index, e.g. the one just played. Applied on the next Open if closed.</summary>
        public void SelectIndex(int index)
        {
            if (!IsOpen)
            {
                _pendingIndex = index;
                return;
            }
            _list.Select(RowOf(index), notify: false, sound: false);
            ShowDetails(LibraryIndex(_list.Selected));
        }

        void Refresh()
        {
            SongLibrary library = Menu.Library;
            var labels = new List<string>();
            _rows.Clear();
            for (int i = 0; i < library.Count; i++)
            {
                SongData song = library.Get(i);
                if (song == null || !library.IsVisible(i)) continue;
                bool unlocked = library.IsUnlocked(i);
                string mark = SaveData.IsCleared(song.id) ? " *" : "";
                string number = song.secret ? "X" : (i + 1).ToString();
                labels.Add(unlocked ? $"{number}  {song.displayName}{mark}" : $"{number}  ???");
                _rows.Add(i);
            }
            _list.SetItems(labels);
            for (int row = 0; row < _rows.Count; row++) _list.SetLocked(row, !library.IsUnlocked(_rows[row]));
        }

        int LibraryIndex(int row) => row >= 0 && row < _rows.Count ? _rows[row] : -1;

        int RowOf(int libraryIndex)
        {
            int row = _rows.IndexOf(libraryIndex);
            return row >= 0 ? row : 0;
        }

        void ShowDetails(int index)
        {
            SongLibrary library = Menu.Library;
            SongData song = library.Get(index);
            if (song == null) return;
            bool unlocked = library.IsUnlocked(index);

            Sprite icon = song.opponentPoses != null && song.opponentPoses.icon != null
                ? song.opponentPoses.icon
                : PixelArtFactory.GetWraithIcon(song.opponentCloak, song.opponentEyes, false);
            _icon.sprite = icon;
            _icon.color = unlocked ? Color.white : new Color(0f, 0f, 0f, 0.85f); // silhouette when locked

            SetText(_opponent, unlocked ? song.opponentName : "???");
            SetText(_song, unlocked ? song.displayName : "CLEAR THE PREVIOUS NIGHT");
            SetText(_difficulty, song.difficulty);
            _difficulty.color = MenuPalette.ForDifficulty(song.difficulty);

            string details = $"{Mathf.RoundToInt(song.Bpm)} BPM";
            try
            {
                if (song.chart == null) throw new ChartFormatException("no chart");
                Chart chart = song.LoadChart();
                int seconds = Mathf.RoundToInt((float)chart.LengthSeconds);
                details += $" · {seconds / 60}:{seconds % 60:00} · {chart.PlayerNoteCount} NOTES";
            }
            catch (ChartFormatException) { }
            SetText(_details, details);

            if (SaveData.HasResult(song.id))
            {
                var best = SaveData.GetBest(song.id);
                SetText(_best, best.cleared ? $"BEST: CLEARED · {best.misses} MISS · {best.maxCombo} COMBO" : "BEST: PERISHED");
            }
            else SetText(_best, "NOT PLAYED YET");

            SetText(_prompt, unlocked ? "ENTER TO FACE THEM" : "LOCKED");
            Menu.Preview(song, unlocked);
        }

        void Update()
        {
            // Slow blink on the call to action.
            _prompt.enabled = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.85f;
        }

        static PixelText Line(Transform parent, string name, float size, Color color, float y)
        {
            PixelText text = UIFactory.Text(name, parent, " ", size, color);
            Top(text.rectTransform, y);
            return text;
        }

        static void SetText(PixelText label, string value)
        {
            label.Text = value;
            label.FitToText();
        }

        static void Top(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FunkyThursday.UI
{
    /// <summary>
    /// A vertical list of pixel-text options, driven by the keyboard (arrows/WASD, Enter, Esc, and
    /// Left/Right for value rows) and the mouse. The selected row is highlighted, sways gently and
    /// gets arrow markers. Locked rows can be selected but refuse to confirm.
    /// </summary>
    public sealed class MenuList : MonoBehaviour
    {
        const float RepeatDelay = 0.35f;
        const float RepeatRate = 0.08f;

        sealed class Row
        {
            public PixelText Label;
            public string Text;
            public bool Locked;
        }

        public event Action<int> Confirmed;
        public event Action<int> SelectionChanged;
        public event Action<int, int> Adjusted; // index, direction (-1 / +1)
        public event Action BackRequested;

        readonly List<Row> _rows = new List<Row>();
        PixelText _markerLeft;
        PixelText _markerRight;
        RectTransform _rect;
        float _pixelSize = 6f;
        float _rowHeight = 64f;
        TextAnchor _alignment = TextAnchor.MiddleCenter;
        int _ignoreInputUntilFrame;
        float _repeatTimer;
        int _repeatDirection;

        public Color IdleColor { get; set; } = new Color32(0x9C, 0x8F, 0xA6, 0xFF);
        public Color SelectedColor { get; set; } = new Color32(0xDD, 0xD6, 0xC6, 0xFF);
        public Color LockedColor { get; set; } = new Color32(0x4A, 0x3E, 0x52, 0xFF);
        public Color MarkerColor { get; set; } = new Color32(0xD0, 0x36, 0x4F, 0xFF);

        public int Selected { get; private set; }
        public int Count => _rows.Count;

        /// <summary>Only the focused list reads the keyboard.</summary>
        public bool Focused { get; private set; }

        public void Configure(float pixelSize, float rowHeight, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            _pixelSize = pixelSize;
            _rowHeight = rowHeight;
            _alignment = alignment;
        }

        public void SetItems(IList<string> labels)
        {
            _rect = (RectTransform)transform;
            foreach (Row row in _rows) Destroy(row.Label.gameObject);
            _rows.Clear();

            for (int i = 0; i < labels.Count; i++)
            {
                PixelText label = UIFactory.Text($"Item {i}", transform, labels[i], _pixelSize, IdleColor, _alignment, raycast: true);
                RectTransform rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(AnchorX(), 1f);
                rect.pivot = new Vector2(AnchorX(), 1f);
                rect.anchoredPosition = new Vector2(0f, -i * _rowHeight);
                label.gameObject.AddComponent<MenuListItem>().Bind(this, i);
                _rows.Add(new Row { Label = label, Text = labels[i] });
            }

            if (_markerLeft == null)
            {
                _markerLeft = UIFactory.Text("Marker Left", transform, ">", _pixelSize, MarkerColor);
                _markerRight = UIFactory.Text("Marker Right", transform, "<", _pixelSize, MarkerColor);
            }

            _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, Mathf.Max(1, labels.Count) * _rowHeight);
            Select(Mathf.Clamp(Selected, 0, Mathf.Max(0, _rows.Count - 1)), notify: false, sound: false);
        }

        public void SetLabel(int index, string text)
        {
            if (index < 0 || index >= _rows.Count) return;
            _rows[index].Text = text;
            _rows[index].Label.Text = text;
            _rows[index].Label.FitToText();
            Refresh();
        }

        public void SetLocked(int index, bool locked)
        {
            if (index < 0 || index >= _rows.Count) return;
            _rows[index].Locked = locked;
            Refresh();
        }

        public void Focus()
        {
            Focused = true;
            _ignoreInputUntilFrame = Time.frameCount + 1; // the key that opened this list must not confirm it
            Refresh();
        }

        public void Unfocus()
        {
            Focused = false;
            Refresh();
        }

        public void Select(int index, bool notify = true, bool sound = true)
        {
            if (_rows.Count == 0) return;
            index = (index % _rows.Count + _rows.Count) % _rows.Count;
            bool changed = index != Selected;
            Selected = index;
            Refresh();
            if (changed && sound) MenuSfx.Play(MenuSfx.Sound.Scroll);
            if (changed && notify) SelectionChanged?.Invoke(index);
        }

        public void Confirm(int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            if (_rows[index].Locked)
            {
                MenuSfx.Play(MenuSfx.Sound.Locked);
                return;
            }
            MenuSfx.Play(MenuSfx.Sound.Confirm);
            Confirmed?.Invoke(index);
        }

        public void Adjust(int direction)
        {
            if (_rows.Count == 0) return;
            MenuSfx.Play(MenuSfx.Sound.Scroll);
            Adjusted?.Invoke(Selected, direction);
        }

        void Update()
        {
            if (!Focused || _rows.Count == 0) return;
            AnimateSelection();
            if (Time.frameCount <= _ignoreInputUntilFrame) return;

            if (MenuInput.UpPressed) StartRepeat(-1);
            else if (MenuInput.DownPressed) StartRepeat(1);
            else if (_repeatDirection != 0 && ((_repeatDirection < 0 && MenuInput.UpHeld) || (_repeatDirection > 0 && MenuInput.DownHeld)))
            {
                _repeatTimer -= Time.unscaledDeltaTime;
                if (_repeatTimer <= 0f)
                {
                    _repeatTimer = RepeatRate;
                    Select(Selected + _repeatDirection);
                }
            }
            else _repeatDirection = 0;

            if (MenuInput.LeftPressed) Adjust(-1);
            if (MenuInput.RightPressed) Adjust(1);
            if (MenuInput.ConfirmPressed) Confirm(Selected);
            if (MenuInput.BackPressed)
            {
                MenuSfx.Play(MenuSfx.Sound.Back);
                BackRequested?.Invoke();
            }
        }

        void StartRepeat(int direction)
        {
            _repeatDirection = direction;
            _repeatTimer = RepeatDelay;
            Select(Selected + direction);
        }

        void AnimateSelection()
        {
            if (Selected >= _rows.Count) return;
            float sway = Mathf.Round(Mathf.Sin(Time.unscaledTime * 6f) * 1.5f) * _pixelSize;
            PositionMarkers(sway);
        }

        void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool selected = i == Selected && Focused;
                row.Label.color = row.Locked ? LockedColor : selected ? SelectedColor : IdleColor;
            }

            bool show = Focused && _rows.Count > 0;
            if (_markerLeft != null)
            {
                _markerLeft.enabled = show;
                _markerRight.enabled = show;
                _markerLeft.color = _markerRight.color = MarkerColor;
                PositionMarkers(0f);
            }
        }

        void PositionMarkers(float sway)
        {
            if (_markerLeft == null || Selected >= _rows.Count) return;
            RectTransform label = _rows[Selected].Label.rectTransform;
            float width = label.sizeDelta.x;
            float gap = _pixelSize * 4f;

            // Label x-range relative to the list's anchor column.
            float left = -width * AnchorX();
            float right = left + width;
            float y = label.anchoredPosition.y;

            PlaceMarker(_markerLeft.rectTransform, left - gap - sway, y, 1f);
            PlaceMarker(_markerRight.rectTransform, right + gap + sway, y, 0f);
        }

        void PlaceMarker(RectTransform marker, float x, float y, float pivotX)
        {
            marker.anchorMin = marker.anchorMax = new Vector2(AnchorX(), 1f);
            marker.pivot = new Vector2(pivotX, 1f);
            marker.anchoredPosition = new Vector2(x, y);
        }

        float AnchorX()
        {
            switch (_alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    return 0f;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return 1f;
                default:
                    return 0.5f;
            }
        }

        // Called by MenuListItem.
        internal void PointerEntered(int index)
        {
            if (Focused) Select(index);
        }

        internal void PointerClicked(int index)
        {
            if (!Focused) return;
            Select(index, sound: false);
            Confirm(index);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace FunkyThursday.UI
{
    /// <summary>Small helpers so every screen builds its layout the same way from code.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>A rect stretched over its parent with optional insets (left, bottom, right, top).</summary>
        public static RectTransform Stretch(string name, Transform parent, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>A rect anchored at a normalised point of its parent, positioned and sized in canvas units.</summary>
        public static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            RectTransform rect = Stretch(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static PixelText Text(string name, Transform parent, string text, float pixelSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<PixelText>();
            label.Text = text;
            label.PixelSize = pixelSize;
            label.Alignment = alignment;
            label.color = color;
            label.raycastTarget = raycast;
            label.FitToText();
            return label;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, float size)
        {
            RectTransform rect = Rect(name, parent);
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}

using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Generates simple placeholder visuals at runtime so gameplay systems
    /// can be built and tested before any real art exists.
    /// </summary>
    public static class PlaceholderAssetFactory
    {
        /// <summary>
        /// Creates a solid-colour square sprite of the given size.
        /// </summary>
        public static Sprite CreateSolidSprite(Color color, int size = 64)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
        }

        /// <summary>
        /// Traditional FNF-style lane colours, in lane order:
        /// left, down, up, right. Change freely as real art comes in.
        /// </summary>
        public static Color[] CreateLaneColors()
        {
            return new Color[]
            {
                new Color(0.96f, 0.15f, 0.75f), // left  - magenta/purple
                new Color(0.10f, 0.85f, 0.95f), // down  - cyan/blue
                new Color(0.15f, 0.90f, 0.25f), // up    - green
                new Color(0.95f, 0.15f, 0.15f), // right - red
            };
        }
    }
}

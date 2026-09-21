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
        /// Draws a simple round-headed blob character (body + head) at
        /// runtime so the stage has someone to animate before any real
        /// character art exists. Drawn facing right by default; flip the
        /// SpriteRenderer's flipX for a character facing the other way.
        /// </summary>
        public static Sprite CreateCharacterSprite(Color bodyColor, Color accentColor, int width = 96, int height = 128)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            float headRadius = width * 0.30f;
            Vector2 headCenter = new Vector2(width * 0.5f, height * 0.78f);

            float bodyWidth = width * 0.42f;
            float bodyHeight = height * 0.5f;
            Rect bodyRect = new Rect(width * 0.5f - bodyWidth / 2f, height * 0.05f, bodyWidth, bodyHeight);
            float bodyCornerRadius = bodyWidth * 0.4f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideHead = Vector2.Distance(new Vector2(x, y), headCenter) <= headRadius;
                    bool insideBody = IsInsideRoundedRect(x, y, bodyRect, bodyCornerRadius);

                    if (insideHead)
                    {
                        pixels[y * width + x] = accentColor;
                    }
                    else if (insideBody)
                    {
                        pixels[y * width + x] = bodyColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.12f),
                100f);
        }

        private static bool IsInsideRoundedRect(int x, int y, Rect rect, float cornerRadius)
        {
            if (x < rect.xMin || x > rect.xMax || y < rect.yMin || y > rect.yMax)
            {
                return false;
            }

            float clampedX = Mathf.Clamp(x, rect.xMin + cornerRadius, rect.xMax - cornerRadius);
            float clampedY = Mathf.Clamp(y, rect.yMin + cornerRadius, rect.yMax - cornerRadius);

            return Vector2.Distance(new Vector2(x, y), new Vector2(clampedX, clampedY)) <= cornerRadius;
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

        /// <summary>
        /// Z-axis rotation (degrees) that points a single "up"-facing arrow
        /// sprite in each lane's direction, in lane order: left, down, up,
        /// right. FNF-style: one arrow asset reused for all 4 lanes instead
        /// of 4 separate directional sprites.
        /// </summary>
        public static readonly float[] LaneArrowRotationsZ = { 90f, 180f, 0f, -90f };
    }
}

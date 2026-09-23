using System.Collections.Generic;
using UnityEngine;

namespace FunkyThursday.UI
{
    /// <summary>
    /// A hand-drawn 5×7 bitmap font baked into a point-filtered atlas at runtime, so every menu is
    /// crisp pixel text with no font asset to import. Upper-case only; lower case is drawn as upper case.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;
        const int Cell = 8;
        const int Columns = 16;

        // Rows top to bottom, '#' = ink.
        static readonly (char c, string rows)[] Glyphs =
        {
            ('A', ".###.|#...#|#...#|#####|#...#|#...#|#...#"),
            ('B', "####.|#...#|#...#|####.|#...#|#...#|####."),
            ('C', ".###.|#...#|#....|#....|#....|#...#|.###."),
            ('D', "####.|#...#|#...#|#...#|#...#|#...#|####."),
            ('E', "#####|#....|#....|####.|#....|#....|#####"),
            ('F', "#####|#....|#....|####.|#....|#....|#...."),
            ('G', ".###.|#...#|#....|#.###|#...#|#...#|.####"),
            ('H', "#...#|#...#|#...#|#####|#...#|#...#|#...#"),
            ('I', ".###.|..#..|..#..|..#..|..#..|..#..|.###."),
            ('J', "..###|...#.|...#.|...#.|...#.|#..#.|.##.."),
            ('K', "#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#"),
            ('L', "#....|#....|#....|#....|#....|#....|#####"),
            ('M', "#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#"),
            ('N', "#...#|#...#|##..#|#.#.#|#..##|#...#|#...#"),
            ('O', ".###.|#...#|#...#|#...#|#...#|#...#|.###."),
            ('P', "####.|#...#|#...#|####.|#....|#....|#...."),
            ('Q', ".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#"),
            ('R', "####.|#...#|#...#|####.|#.#..|#..#.|#...#"),
            ('S', ".####|#....|#....|.###.|....#|....#|####."),
            ('T', "#####|..#..|..#..|..#..|..#..|..#..|..#.."),
            ('U', "#...#|#...#|#...#|#...#|#...#|#...#|.###."),
            ('V', "#...#|#...#|#...#|#...#|#...#|.#.#.|..#.."),
            ('W', "#...#|#...#|#...#|#.#.#|#.#.#|#.#.#|.#.#."),
            ('X', "#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#"),
            ('Y', "#...#|#...#|.#.#.|..#..|..#..|..#..|..#.."),
            ('Z', "#####|....#|...#.|..#..|.#...|#....|#####"),
            ('0', ".###.|#...#|#..##|#.#.#|##..#|#...#|.###."),
            ('1', "..#..|.##..|..#..|..#..|..#..|..#..|.###."),
            ('2', ".###.|#...#|....#|...#.|..#..|.#...|#####"),
            ('3', "####.|....#|....#|.###.|....#|....#|####."),
            ('4', "...#.|..##.|.#.#.|#..#.|#####|...#.|...#."),
            ('5', "#####|#....|####.|....#|....#|#...#|.###."),
            ('6', "..##.|.#...|#....|####.|#...#|#...#|.###."),
            ('7', "#####|....#|...#.|..#..|.#...|.#...|.#..."),
            ('8', ".###.|#...#|#...#|.###.|#...#|#...#|.###."),
            ('9', ".###.|#...#|#...#|.####|....#|...#.|.##.."),
            ('.', ".....|.....|.....|.....|.....|.##..|.##.."),
            (',', ".....|.....|.....|.....|.##..|..#..|.#..."),
            ('!', "..#..|..#..|..#..|..#..|..#..|.....|..#.."),
            ('?', ".###.|#...#|....#|...#.|..#..|.....|..#.."),
            (':', ".....|.##..|.##..|.....|.##..|.##..|....."),
            ('-', ".....|.....|.....|.###.|.....|.....|....."),
            ('+', ".....|..#..|..#..|#####|..#..|..#..|....."),
            ('/', "....#|....#|...#.|..#..|.#...|#....|#...."),
            ('\'', "..#..|..#..|.#...|.....|.....|.....|....."),
            ('%', "##..#|##..#|...#.|..#..|.#...|#..##|#..##"),
            ('(', "...#.|..#..|.#...|.#...|.#...|..#..|...#."),
            (')', ".#...|..#..|...#.|...#.|...#.|..#..|.#..."),
            ('<', "...#.|..#..|.#...|#....|.#...|..#..|...#."),
            ('>', ".#...|..#..|...#.|....#|...#.|..#..|.#..."),
            ('&', ".##..|#..#.|#.#..|.#...|#.#.#|#..#.|.##.#"),
            ('*', ".....|#.#.#|.###.|#####|.###.|#.#.#|....."),
            ('·', ".....|.....|.....|.##..|.##..|.....|....."),
            ('×', ".....|#...#|.#.#.|..#..|.#.#.|#...#|....."),
        };

        static Texture2D _atlas;
        static Dictionary<char, Rect> _uvs;

        public static Texture2D Atlas
        {
            get
            {
                if (_atlas == null) Build();
                return _atlas;
            }
        }

        public static bool TryGetUV(char c, out Rect uv)
        {
            if (_atlas == null) Build();
            return _uvs.TryGetValue(char.ToUpperInvariant(c), out uv);
        }

        /// <summary>Width in canvas units of one line of text.</summary>
        public static float MeasureLine(string line, float pixelSize, int letterSpacing)
        {
            if (string.IsNullOrEmpty(line)) return 0f;
            return (line.Length * (GlyphWidth + letterSpacing) - letterSpacing) * pixelSize;
        }

        static void Build()
        {
            int rows = Mathf.CeilToInt(Glyphs.Length / (float)Columns);
            int width = Columns * Cell;
            int height = rows * Cell;
            var pixels = new Color32[width * height];
            _uvs = new Dictionary<char, Rect>(Glyphs.Length);

            for (int i = 0; i < Glyphs.Length; i++)
            {
                char c = Glyphs[i].c;
                string rowData = Glyphs[i].rows;
                string[] lines = rowData.Split('|');
                int cellX = (i % Columns) * Cell;
                int cellTop = height - (i / Columns) * Cell - 1; // texture y grows upward

                for (int row = 0; row < GlyphHeight; row++)
                {
                    for (int x = 0; x < GlyphWidth; x++)
                    {
                        if (lines[row][x] != '#') continue;
                        int px = cellX + x;
                        int py = cellTop - row;
                        pixels[py * width + px] = new Color32(255, 255, 255, 255);
                    }
                }

                float u = cellX / (float)width;
                float v = (cellTop - GlyphHeight + 1) / (float)height;
                _uvs[c] = new Rect(u, v, GlyphWidth / (float)width, GlyphHeight / (float)height);
            }

            _atlas = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "PixelFontAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _atlas.SetPixels32(pixels);
            _atlas.Apply(false, true);
        }
    }
}

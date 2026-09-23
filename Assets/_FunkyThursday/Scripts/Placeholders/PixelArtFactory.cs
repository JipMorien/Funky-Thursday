using System.Collections.Generic;
using FunkyThursday.Core;
using FunkyThursday.Visuals;
using UnityEngine;

namespace FunkyThursday.Placeholders
{
    public enum ArrowStyle
    {
        Note,
        ReceptorIdle,
        ReceptorPressed,
        ReceptorConfirm
    }

    /// <summary>
    /// Builds point-filtered pixel-art sprites at runtime so every scene is playable before any
    /// Higgsfield asset exists. Results are cached; destroyed entries are rebuilt on demand, which
    /// keeps it safe with "Enter Play Mode Options" (domain reload disabled).
    /// </summary>
    public static class PixelArtFactory
    {
        public const int ArrowSize = 16;
        public const float ArrowPixelsPerUnit = 16f;
        public const float CharacterPixelsPerUnit = 16f;

        public const int BackdropWidth = 160;
        public const int BackdropHeight = 90;
        public const int BackdropGroundHeight = 12;
        public const float BackdropPixelsPerUnit = BackdropHeight / 10f; // fills an orthographic size of 5

        // Drawn pointing up, row 0 = top. '#' outline, 'o' fill, 'h' highlight, '.' transparent.
        static readonly string[] ArrowUpMask =
        {
            "................",
            ".......##.......",
            "......#oo#......",
            ".....#oooo#.....",
            "....#oohooo#....",
            "...#oohhoooo#...",
            "..#oooooooooo#..",
            ".#oooooooooooo#.",
            ".####oooooo####.",
            "....#oooooo#....",
            "....#oooooo#....",
            "....#oooooo#....",
            "....#oooooo#....",
            "....#oooooo#....",
            "....########....",
            "................"
        };

        // Hooded wraith, row 0 = top. '#' outline, 'c' cloak, 'd' cloak shade, 'k' hood void, 'e' eyes.
        static readonly string[] WraithMask =
        {
            "......####......",
            ".....#cccc#.....",
            "....#cccccc#....",
            "...#cckkkkcc#...",
            "...#ckkkkkkc#...",
            "...#ckekkekc#...",
            "...#ckkkkkkc#...",
            "...#cckkkkcc#...",
            "..#cccckkcccc#..",
            ".#cccccccccddd#.",
            ".#cccccccccddd#.",
            ".#cccccccccddd#.",
            ".#cccccccccddd#.",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "#cccccccccccddd#",
            "################"
        };

        // Health-bar head icon, row 0 = top. Same legend as the wraith.
        static readonly string[] WraithIconMask =
        {
            "......####......",
            "....##cccc##....",
            "...#cccccccc#...",
            "..#cccccccccc#..",
            "..#ccckkkkccc#..",
            ".#cckkkkkkkkcc#.",
            ".#cckekkkkekcc#.",
            ".#cckkkkkkkkcc#.",
            ".#cckkkkkkkkcc#.",
            ".#ccckkkkkkccc#.",
            ".#cccckkkkcccc#.",
            "..#cccccccccc#..",
            "..#cccccccccc#..",
            "...#cccccccc#...",
            "....########....",
            "................"
        };

        // Sustain trail: one repeating body row plus a rounded tail cap, row 0 = top.
        const string HoldBodyRow = ".....#oooo#.....";
        static readonly string[] HoldEndMask =
        {
            ".....#oooo#.....",
            ".....#oooo#.....",
            ".....#oooo#.....",
            "......#oo#......",
            ".......##.......",
            "................"
        };

        static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        static readonly Dictionary<(NoteLane, ArrowStyle), Sprite> ArrowCache = new Dictionary<(NoteLane, ArrowStyle), Sprite>();
        static readonly Dictionary<ulong, Sprite> WraithCache = new Dictionary<ulong, Sprite>();
        static readonly Dictionary<(ulong, bool), Sprite> IconCache = new Dictionary<(ulong, bool), Sprite>();
        static readonly Dictionary<(NoteLane, bool), Sprite> HoldCache = new Dictionary<(NoteLane, bool), Sprite>();
        static Sprite _whitePixel;
        static Sprite _graveyardBackdrop;

        /// <summary>1×1 white sprite (1 world unit). Tint and scale it for bars, frames and flashes.</summary>
        public static Sprite WhitePixel
        {
            get
            {
                if (_whitePixel == null)
                {
                    _whitePixel = CreateSprite(new[] { new Color32(255, 255, 255, 255) }, 1, 1, 1f,
                        new Vector2(0.5f, 0.5f), "WhitePixel");
                }
                return _whitePixel;
            }
        }

        /// <summary>160×90 stormy graveyard that exactly fills a 16:9 camera with orthographic size 5.</summary>
        public static Sprite GraveyardBackdrop
        {
            get
            {
                if (_graveyardBackdrop == null) _graveyardBackdrop = BuildGraveyardBackdrop();
                return _graveyardBackdrop;
            }
        }

        public static Sprite GetArrow(NoteLane lane, ArrowStyle style)
        {
            var key = (lane, style);
            if (ArrowCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Sprite sprite = BuildArrow(lane, style);
            ArrowCache[key] = sprite;
            return sprite;
        }

        /// <summary>16×24 hooded figure, pivot at the feet.</summary>
        public static Sprite GetWraith(Color32 cloak, Color32 eyes)
        {
            ulong key = ((ulong)Pack(cloak) << 32) | Pack(eyes);
            if (WraithCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Sprite sprite = BuildWraith(cloak, eyes);
            WraithCache[key] = sprite;
            return sprite;
        }

        /// <summary>16×16 head for the health bar. The losing variant dims the eyes and cloak.</summary>
        public static Sprite GetWraithIcon(Color32 cloak, Color32 eyes, bool losing)
        {
            var key = (((ulong)Pack(cloak) << 32) | Pack(eyes), losing);
            if (IconCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Color32 iconCloak = losing ? GothicPalette.Darken(cloak, 0.35f) : cloak;
            Color32 iconEyes = losing ? GothicPalette.Ash : eyes;
            Sprite sprite = BuildFromMask(WraithIconMask, iconCloak, iconEyes, ArrowPixelsPerUnit,
                new Vector2(0.5f, 0.5f), losing ? "WraithIcon_Losing" : "WraithIcon");
            IconCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Sustain trail pieces for a lane, both pivoted at the top centre. The body is one pixel
        /// tall (1/16 unit) so it is stretched on Y; the tail cap is 6 pixels tall.
        /// </summary>
        public static Sprite GetHold(NoteLane lane, bool tail)
        {
            var key = (lane, tail);
            if (HoldCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Color32 fill = GothicPalette.Darken(GothicPalette.Lane(lane), 0.15f);
            string[] rows = tail ? HoldEndMask : new[] { HoldBodyRow };
            int width = rows[0].Length;
            int height = rows.Length;
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                string row = rows[height - 1 - y];
                for (int x = 0; x < width; x++)
                {
                    char c = row[x];
                    pixels[y * width + x] = c == '#' ? GothicPalette.Ink : c == 'o' ? fill : GothicPalette.Transparent;
                }
            }

            Sprite sprite = CreateSprite(pixels, width, height, ArrowPixelsPerUnit, new Vector2(0.5f, 1f),
                $"Hold_{lane}_{(tail ? "Tail" : "Body")}");
            HoldCache[key] = sprite;
            return sprite;
        }

        static Sprite BuildArrow(NoteLane lane, ArrowStyle style)
        {
            GetArrowColors(lane, style, out Color32 outline, out Color32 fill, out Color32 highlight);

            var pixels = new Color32[ArrowSize * ArrowSize];
            for (int y = 0; y < ArrowSize; y++)
            {
                for (int x = 0; x < ArrowSize; x++)
                {
                    char c = SampleArrow(lane, x, y);
                    Color32 color;
                    switch (c)
                    {
                        case '#': color = outline; break;
                        case 'o': color = fill; break;
                        case 'h': color = highlight; break;
                        default: color = GothicPalette.Transparent; break;
                    }
                    pixels[y * ArrowSize + x] = color;
                }
            }

            return CreateSprite(pixels, ArrowSize, ArrowSize, ArrowPixelsPerUnit,
                new Vector2(0.5f, 0.5f), $"Arrow_{lane}_{style}");
        }

        /// <summary>Maps a destination pixel (texture space, y up) back to the up-pointing mask.</summary>
        static char SampleArrow(NoteLane lane, int x, int y)
        {
            const int last = ArrowSize - 1;
            int sx, sy;
            switch (lane)
            {
                case NoteLane.Left: sx = y; sy = last - x; break;
                case NoteLane.Down: sx = last - x; sy = last - y; break;
                case NoteLane.Right: sx = last - y; sy = x; break;
                default: sx = x; sy = y; break;
            }
            return ArrowUpMask[last - sy][sx];
        }

        static void GetArrowColors(NoteLane lane, ArrowStyle style, out Color32 outline, out Color32 fill, out Color32 highlight)
        {
            Color32 laneColor = GothicPalette.Lane(lane);
            switch (style)
            {
                case ArrowStyle.ReceptorIdle:
                    outline = GothicPalette.Ash;
                    fill = GothicPalette.Stone;
                    highlight = GothicPalette.StoneLight;
                    break;
                case ArrowStyle.ReceptorPressed:
                    outline = GothicPalette.Lighten(laneColor, 0.15f);
                    fill = GothicPalette.Darken(laneColor, 0.55f);
                    highlight = GothicPalette.Darken(laneColor, 0.3f);
                    break;
                case ArrowStyle.ReceptorConfirm:
                    outline = GothicPalette.Lighten(laneColor, 0.7f);
                    fill = GothicPalette.Lighten(laneColor, 0.3f);
                    highlight = GothicPalette.Bone;
                    break;
                default:
                    outline = GothicPalette.Ink;
                    fill = laneColor;
                    highlight = GothicPalette.Lighten(laneColor, 0.45f);
                    break;
            }
        }

        static Sprite BuildWraith(Color32 cloak, Color32 eyes) =>
            BuildFromMask(WraithMask, cloak, eyes, CharacterPixelsPerUnit, new Vector2(0.5f, 0f), "Wraith");

        static Sprite BuildFromMask(string[] mask, Color32 cloak, Color32 eyes, float pixelsPerUnit, Vector2 pivot, string name)
        {
            int width = mask[0].Length;
            int height = mask.Length;
            Color32 shade = GothicPalette.Darken(cloak, 0.35f);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                string row = mask[height - 1 - y];
                for (int x = 0; x < width; x++)
                {
                    Color32 color;
                    switch (row[x])
                    {
                        case '#': color = GothicPalette.Ink; break;
                        case 'c': color = cloak; break;
                        case 'd': color = shade; break;
                        case 'k': color = GothicPalette.Void; break;
                        case 'e': color = eyes; break;
                        default: color = GothicPalette.Transparent; break;
                    }
                    pixels[y * width + x] = color;
                }
            }

            return CreateSprite(pixels, width, height, pixelsPerUnit, pivot, name);
        }

        static Sprite BuildGraveyardBackdrop()
        {
            const int w = BackdropWidth;
            const int h = BackdropHeight;
            const int ground = BackdropGroundHeight;
            var pixels = new Color32[w * h];

            // Dithered night sky with sparse stars.
            Color32[] sky = GothicPalette.Sky;
            for (int y = 0; y < h; y++)
            {
                float t = (h - 1 - y) / (float)(h - 1);
                float scaled = t * (sky.Length - 1);
                int band = Mathf.Min((int)scaled, sky.Length - 2);
                float frac = scaled - band;

                for (int x = 0; x < w; x++)
                {
                    Color32 color = frac > BayerThreshold(x, y) ? sky[band + 1] : sky[band];
                    if (y > h / 3)
                    {
                        int hash = Hash(x, y);
                        if ((hash & 1023) == 0) color = GothicPalette.Bone;
                        else if ((hash & 255) == 0) color = GothicPalette.Ash;
                    }
                    pixels[y * w + x] = color;
                }
            }

            DrawMoon(pixels, w, h, w / 2, 66, 11); // centred between the two strumlines

            // Rolling far hills.
            for (int x = 0; x < w; x++)
            {
                int top = 22 + Mathf.RoundToInt(3f * Mathf.Sin(x * 0.06f) + 2f * Mathf.Sin(x * 0.17f + 1.3f));
                for (int y = ground; y < top; y++) pixels[y * w + x] = GothicPalette.FarHills;
            }

            DrawChapel(pixels, w, 128);

            // Foreground ground.
            for (int y = 0; y < ground; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = GothicPalette.Void;

            int[] stoneX = { 8, 30, 51, 97, 140 };
            int[] stoneW = { 7, 5, 9, 6, 8 };
            int[] stoneH = { 10, 7, 12, 8, 11 };
            for (int i = 0; i < stoneX.Length; i++) DrawTombstone(pixels, w, stoneX[i], ground, stoneW[i], stoneH[i]);

            int[] crossX = { 20, 74, 115, 152 };
            int[] crossH = { 12, 15, 10, 13 };
            for (int i = 0; i < crossX.Length; i++) DrawCross(pixels, w, crossX[i], ground, crossH[i]);

            return CreateSprite(pixels, w, h, BackdropPixelsPerUnit, new Vector2(0.5f, 0.5f), "GraveyardBackdrop");
        }

        static void DrawMoon(Color32[] pixels, int w, int h, int cx, int cy, int r)
        {
            int halo = r + 2;
            for (int dy = -halo; dy <= halo; dy++)
            {
                for (int dx = -halo; dx <= halo; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (px < 0 || px >= w || py < 0 || py >= h) continue;

                    int d2 = dx * dx + dy * dy;
                    int index = py * w + px;

                    if (d2 <= r * r)
                    {
                        bool rim = d2 > (r - 2) * (r - 2) && dx + dy < 0;
                        bool crater = Sq(dx + 3) + Sq(dy - 2) <= 4 || Sq(dx - 4) + Sq(dy + 3) <= 2 || Sq(dx - 1) + Sq(dy - 5) <= 1;
                        pixels[index] = rim || crater ? GothicPalette.BoneShadow : GothicPalette.Bone;
                    }
                    else if (d2 <= halo * halo && BayerThreshold(px, py) < 0.5f)
                    {
                        pixels[index] = Color32.Lerp(pixels[index], GothicPalette.Bone, 0.18f);
                    }
                }
            }
        }

        static void DrawChapel(Color32[] pixels, int w, int x0)
        {
            // Body, pitched roof, cross and one ember-lit window on the far hill.
            for (int y = 20; y < 34; y++)
                for (int x = x0; x < x0 + 12; x++)
                    pixels[y * w + x] = GothicPalette.FarHills;

            for (int row = 0; row < 6; row++)
                for (int x = x0 + row; x < x0 + 12 - row; x++)
                    pixels[(34 + row) * w + x] = GothicPalette.FarHills;

            for (int y = 40; y < 45; y++)
            {
                pixels[y * w + x0 + 5] = GothicPalette.FarHills;
                pixels[y * w + x0 + 6] = GothicPalette.FarHills;
            }
            for (int x = x0 + 4; x < x0 + 8; x++) pixels[43 * w + x] = GothicPalette.FarHills;

            Color32 ember = GothicPalette.Darken(GothicPalette.Blood, 0.35f);
            for (int y = 27; y < 30; y++)
            {
                pixels[y * w + x0 + 5] = ember;
                pixels[y * w + x0 + 6] = ember;
            }
        }

        static void DrawTombstone(Color32[] pixels, int w, int x0, int ground, int width, int height)
        {
            for (int y = ground; y < ground + height; y++)
            {
                for (int x = x0; x < x0 + width; x++)
                {
                    bool roundedCorner = y == ground + height - 1 && (x == x0 || x == x0 + width - 1);
                    if (!roundedCorner) pixels[y * w + x] = GothicPalette.Void;
                }
            }
        }

        static void DrawCross(Color32[] pixels, int w, int cx, int ground, int height)
        {
            for (int y = ground; y < ground + height; y++)
            {
                pixels[y * w + cx] = GothicPalette.Void;
                pixels[y * w + cx + 1] = GothicPalette.Void;
            }
            int arm = ground + height - 4;
            for (int x = cx - 2; x <= cx + 3; x++) pixels[arm * w + x] = GothicPalette.Void;
        }

        static Sprite CreateSprite(Color32[] pixels, int width, int height, float pixelsPerUnit, Vector2 pivot, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        static float BayerThreshold(int x, int y) => (Bayer4[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;

        static int Sq(int v) => v * v;

        static uint Pack(Color32 c) => ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;

        static int Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return h ^ (h >> 16);
            }
        }
    }
}

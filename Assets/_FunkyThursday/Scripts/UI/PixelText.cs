using UnityEngine;
using UnityEngine.UI;

namespace FunkyThursday.UI
{
    /// <summary>
    /// uGUI text drawn with <see cref="PixelFont"/>: one textured quad per glyph (plus an optional
    /// drop shadow), tinted by the graphic's colour. Supports multiple lines and TextAnchor alignment.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelText : MaskableGraphic
    {
        [SerializeField, TextArea] string text = "";
        [Tooltip("Canvas units per font pixel. 4 at 1080p is a comfortable body size.")]
        [SerializeField, Min(0.5f)] float pixelSize = 4f;
        [SerializeField] TextAnchor alignment = TextAnchor.MiddleCenter;
        [SerializeField, Min(0)] int letterSpacing = 1;
        [SerializeField, Min(0)] int lineSpacing = 3;
        [SerializeField] bool shadow = true;
        [SerializeField] Color shadowColor = new Color32(0x12, 0x0A, 0x14, 0xFF);

        public string Text
        {
            get => text;
            set
            {
                value = value ?? "";
                if (text == value) return;
                text = value;
                SetVerticesDirty();
            }
        }

        public float PixelSize
        {
            get => pixelSize;
            set
            {
                pixelSize = Mathf.Max(0.5f, value);
                SetVerticesDirty();
            }
        }

        public TextAnchor Alignment
        {
            get => alignment;
            set
            {
                alignment = value;
                SetVerticesDirty();
            }
        }

        public bool Shadow
        {
            get => shadow;
            set
            {
                shadow = value;
                SetVerticesDirty();
            }
        }

        public override Texture mainTexture => PixelFont.Atlas;

        /// <summary>Size the text needs, in canvas units.</summary>
        public Vector2 PreferredSize
        {
            get
            {
                string[] lines = Lines();
                float width = 0f;
                foreach (string line in lines) width = Mathf.Max(width, PixelFont.MeasureLine(line, pixelSize, letterSpacing));
                float height = (lines.Length * (PixelFont.GlyphHeight + lineSpacing) - lineSpacing) * pixelSize;
                float extra = shadow ? pixelSize : 0f;
                return new Vector2(width + extra, height + extra);
            }
        }

        /// <summary>Resizes the RectTransform to fit the text exactly.</summary>
        public void FitToText() => rectTransform.sizeDelta = PreferredSize;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (string.IsNullOrEmpty(text)) return;

            string[] lines = Lines();
            Rect rect = GetPixelAdjustedRect();
            float lineHeight = (PixelFont.GlyphHeight + lineSpacing) * pixelSize;
            float blockHeight = (lines.Length * (PixelFont.GlyphHeight + lineSpacing) - lineSpacing) * pixelSize;
            float advance = (PixelFont.GlyphWidth + letterSpacing) * pixelSize;
            float glyphW = PixelFont.GlyphWidth * pixelSize;
            float glyphH = PixelFont.GlyphHeight * pixelSize;

            float top;
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    top = rect.yMax;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    top = rect.yMin + blockHeight;
                    break;
                default:
                    top = rect.center.y + blockHeight * 0.5f;
                    break;
            }

            // Shadow pass first so it sits underneath.
            int passes = shadow ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
            {
                bool isShadow = shadow && pass == 0;
                Color32 tint = isShadow ? (Color32)new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * color.a) : (Color32)color;
                Vector2 offset = isShadow ? new Vector2(pixelSize, -pixelSize) : Vector2.zero;

                for (int l = 0; l < lines.Length; l++)
                {
                    string line = lines[l];
                    float width = PixelFont.MeasureLine(line, pixelSize, letterSpacing);
                    float x = HorizontalStart(rect, width);
                    float y = Snap(top - l * lineHeight);
                    x = Snap(x);

                    for (int i = 0; i < line.Length; i++)
                    {
                        if (PixelFont.TryGetUV(line[i], out Rect uv))
                        {
                            Vector2 min = new Vector2(x + i * advance, y - glyphH) + offset;
                            Vector2 max = new Vector2(x + i * advance + glyphW, y) + offset;
                            AddQuad(vh, min, max, uv, tint);
                        }
                    }
                }
            }
        }

        float HorizontalStart(Rect rect, float width)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    return rect.xMin;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return rect.xMax - width;
                default:
                    return rect.center.x - width * 0.5f;
            }
        }

        float Snap(float v) => Mathf.Round(v / pixelSize) * pixelSize;

        string[] Lines() => (text ?? "").ToUpperInvariant().Split('\n');

        static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Rect uv, Color32 tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), tint, new Vector2(uv.xMin, uv.yMin));
            vh.AddVert(new Vector3(min.x, max.y), tint, new Vector2(uv.xMin, uv.yMax));
            vh.AddVert(new Vector3(max.x, max.y), tint, new Vector2(uv.xMax, uv.yMax));
            vh.AddVert(new Vector3(max.x, min.y), tint, new Vector2(uv.xMax, uv.yMin));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}

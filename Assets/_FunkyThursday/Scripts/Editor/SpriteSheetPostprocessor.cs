using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FunkyThursday.Visuals;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Imports everything under Art/ as crisp pixel art. A PNG with a sibling &lt;name&gt;.sheet.json
    /// (written by Tools/higgs_import.py) is sliced into frames with the right pivot and pixels-per-unit,
    /// and a matching &lt;name&gt;.asset SpriteAnimation is created or refreshed next to it.
    /// PNGs without a sidecar (icons) import as single point-filtered sprites.
    /// Slicing uses the Sprite Editor data provider (com.unity.2d.sprite) with stable sprite IDs, so
    /// re-importing a sheet keeps every reference to its frames intact.
    /// </summary>
    public sealed class SpriteSheetPostprocessor : AssetPostprocessor
    {
        const string ArtRoot = "Assets/_FunkyThursday/Art/";
        const string SheetSuffix = ".sheet.json";
        const float DefaultPixelsPerUnit = 16f;

        [Serializable]
        sealed class SheetMeta
        {
            public int frameWidth;
            public int frameHeight;
            public int columns;
            public int count;
            public float fps = 12f;
            public bool loop = true;
            public float pixelsPerUnit = DefaultPixelsPerUnit;
            public float pivotX = 0.5f;
            public float pivotY;
            public string kind;
        }

        void OnPreprocessTexture()
        {
            string path = Normalize(assetPath);
            if (!path.StartsWith(ArtRoot, StringComparison.Ordinal) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 8192;

            SheetMeta meta = ReadMeta(path);
            if (meta == null)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = DefaultPixelsPerUnit;
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = meta.pixelsPerUnit;
            SliceSheet(importer, path, meta);
        }

        static void SliceSheet(TextureImporter importer, string path, SheetMeta meta)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            int rows = Mathf.CeilToInt(meta.count / (float)meta.columns);
            string baseName = Path.GetFileNameWithoutExtension(path);
            var rects = new SpriteRect[meta.count];
            var ids = new List<SpriteNameFileIdPair>(meta.count);

            for (int i = 0; i < meta.count; i++)
            {
                int column = i % meta.columns;
                int row = i / meta.columns; // rows run top-down in the PNG, bottom-up in Unity
                string name = $"{baseName}_{i:000}";
                GUID id = StableId(path, i);

                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(column * meta.frameWidth, (rows - 1 - row) * meta.frameHeight, meta.frameWidth, meta.frameHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(meta.pivotX, meta.pivotY),
                    spriteID = id
                };
                ids.Add(new SpriteNameFileIdPair(name, id));
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(ids);
            provider.Apply();
        }

        /// <summary>Same frame, same ID on every import, so animations never lose their sprites.</summary>
        static GUID StableId(string path, int index) => new GUID(Hash128.Compute($"{path}#{index}").ToString());

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var sheets = new List<string>();
            var reimport = new List<string>();

            foreach (string raw in importedAssets)
            {
                string path = Normalize(raw);
                if (!path.StartsWith(ArtRoot, StringComparison.Ordinal)) continue;

                if (path.EndsWith(SheetSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    // A changed sidecar means the PNG must be sliced again.
                    string png = path.Substring(0, path.Length - SheetSuffix.Length) + ".png";
                    if (File.Exists(png) && !importedAssets.Any(a => Normalize(a) == png)) reimport.Add(png);
                }
                else if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && ReadMeta(path) != null)
                {
                    sheets.Add(path);
                }
            }

            if (sheets.Count == 0 && reimport.Count == 0) return;

            EditorApplication.delayCall += () =>
            {
                foreach (string png in reimport) AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceUpdate);
                foreach (string png in sheets) BuildAnimation(png);
                AssetDatabase.SaveAssets();
            };
        }

        static void BuildAnimation(string pngPath)
        {
            SheetMeta meta = ReadMeta(pngPath);
            if (meta == null) return;

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(pngPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length == 0) return;

            string animationPath = pngPath.Substring(0, pngPath.Length - ".png".Length) + ".asset";
            var animation = AssetDatabase.LoadAssetAtPath<SpriteAnimation>(animationPath);
            if (animation == null)
            {
                animation = ScriptableObject.CreateInstance<SpriteAnimation>();
                animation.SetFrames(frames, meta.fps, meta.loop);
                AssetDatabase.CreateAsset(animation, animationPath);
            }
            else
            {
                animation.SetFrames(frames, meta.fps, meta.loop);
                EditorUtility.SetDirty(animation);
            }
        }

        static SheetMeta ReadMeta(string pngPath)
        {
            string jsonPath = pngPath.Substring(0, pngPath.Length - ".png".Length) + SheetSuffix;
            if (!File.Exists(jsonPath)) return null;

            try
            {
                var meta = JsonUtility.FromJson<SheetMeta>(File.ReadAllText(jsonPath));
                bool valid = meta != null && meta.count > 0 && meta.columns > 0 && meta.frameWidth > 0 && meta.frameHeight > 0;
                if (!valid) Debug.LogWarning($"{jsonPath} is missing frame size, columns or count.");
                return valid ? meta : null;
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"{jsonPath} could not be read: {e.Message}");
                return null;
            }
        }

        static string Normalize(string path) => path.Replace('\\', '/');
    }
}

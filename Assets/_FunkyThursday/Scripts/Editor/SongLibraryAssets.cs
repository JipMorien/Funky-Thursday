using System.IO;
using FunkyThursday.Data;
using UnityEditor;
using UnityEngine;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Creates (or tops up) the four SongData assets and the SongLibrary under Data/Songs from the
    /// generated charts and music. Existing values are kept; only empty fields are filled.
    /// </summary>
    public static class SongLibraryAssets
    {
        public const string SongsFolder = "Assets/_FunkyThursday/Data/Songs";
        public const string LibraryPath = "Assets/_FunkyThursday/Data/SongLibrary.asset";
        const string ChartFolder = "Assets/_FunkyThursday/Charts";
        const string MusicFolder = "Assets/_FunkyThursday/Audio/Music";

        public struct Spec
        {
            public string Id;
            public string Name;
            public string Opponent;
            public string OpponentId;
            public string Difficulty;
            public Color32 Cloak;
            public Color32 Eyes;
            public Color32 Tint;
            public float PreviewBeat;
        }

        public static readonly Spec[] Specs =
        {
            new Spec { Id = "graveyard-gatekeeper", Name = "Graveyard Gatekeeper", Opponent = "The Gatekeeper", OpponentId = "gatekeeper", Difficulty = "Easy",
                Cloak = new Color32(0x1F, 0x3B, 0x2A, 255), Eyes = new Color32(0x9C, 0xFF, 0x6A, 255), Tint = new Color32(255, 255, 255, 255), PreviewBeat = 72 },
            new Spec { Id = "crypt-keeper", Name = "Crypt Keeper", Opponent = "The Crypt Keeper", OpponentId = "cryptkeeper", Difficulty = "Medium",
                Cloak = new Color32(0x4A, 0x45, 0x52, 255), Eyes = new Color32(0xFF, 0xD3, 0x6A, 255), Tint = new Color32(0xB8, 0xC4, 0xB0, 255), PreviewBeat = 104 },
            new Spec { Id = "cathedral-organist", Name = "Cathedral Organist", Opponent = "The Organist", OpponentId = "organist", Difficulty = "Hard",
                Cloak = new Color32(0x3A, 0x1A, 0x5C, 255), Eyes = new Color32(0xD0, 0x8C, 0xFF, 255), Tint = new Color32(0xC9, 0xB6, 0xF0, 255), PreviewBeat = 120 },
            new Spec { Id = "gothic-monarch", Name = "Gothic Monarch", Opponent = "The Gothic Monarch", OpponentId = "monarch", Difficulty = "Expert",
                Cloak = new Color32(0x5A, 0x14, 0x24, 255), Eyes = new Color32(0xFF, 0x4A, 0x3D, 255), Tint = new Color32(0xF0, 0xA0, 0xA8, 255), PreviewBeat = 136 },
        };

        static readonly int[] Bpms = { 100, 125, 145, 165 };

        [MenuItem("Funky Thursday/Create or Update Song Library")]
        public static SongLibrary EnsureLibrary()
        {
            Directory.CreateDirectory(SongsFolder);

            var library = AssetDatabase.LoadAssetAtPath<SongLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SongLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            var songs = new SongData[Specs.Length];
            for (int i = 0; i < Specs.Length; i++) songs[i] = EnsureSong(Specs[i], i);
            library.songs = songs;

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        static SongData EnsureSong(Spec spec, int index)
        {
            string path = $"{SongsFolder}/{index + 1:00}-{spec.Id}.asset";
            var song = AssetDatabase.LoadAssetAtPath<SongData>(path);
            bool created = song == null;
            if (created)
            {
                song = ScriptableObject.CreateInstance<SongData>();
                AssetDatabase.CreateAsset(song, path);
            }

            if (string.IsNullOrEmpty(song.id)) song.id = spec.Id;
            if (string.IsNullOrEmpty(song.displayName)) song.displayName = spec.Name;
            if (string.IsNullOrEmpty(song.opponentName)) song.opponentName = spec.Opponent;
            if (string.IsNullOrEmpty(song.difficulty)) song.difficulty = spec.Difficulty;
            song.level = index + 1;

            if (song.chart == null) song.chart = AssetDatabase.LoadAssetAtPath<TextAsset>($"{ChartFolder}/level{index + 1}-{spec.Id}.json");
            if (song.instrumental == null) song.instrumental = AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicFolder}/{spec.Id}/Inst.ogg");
            if (song.vocals == null) song.vocals = AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicFolder}/{spec.Id}/Voices.ogg");

            if (created)
            {
                song.opponentCloak = spec.Cloak;
                song.opponentEyes = spec.Eyes;
                song.backdropTint = spec.Tint;
                song.previewStart = spec.PreviewBeat * 60f / Bpms[index];
            }

            if (song.chart == null || song.instrumental == null)
                Debug.LogWarning($"{spec.Name}: chart or Inst.ogg not found. Import the Phase 2 package first.");

            EditorUtility.SetDirty(song);
            return song;
        }
    }
}

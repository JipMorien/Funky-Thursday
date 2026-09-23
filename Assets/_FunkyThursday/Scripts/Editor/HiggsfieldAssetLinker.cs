using System.IO;
using FunkyThursday.Data;
using FunkyThursday.Visuals;
using UnityEditor;
using UnityEngine;

namespace FunkyThursday.EditorTools
{
    /// <summary>
    /// Menu: Funky Thursday → Link Higgsfield Art. Finds imported art by file name and stores it in
    /// the data assets: the player's poses and icons on the SongLibrary; each opponent's poses, icons,
    /// backdrop sheet and backdrop video on its SongData. No scene needs to be open, and slots are
    /// only filled when a file exists, so it's safe to run after every import.
    /// </summary>
    public static class HiggsfieldAssetLinker
    {
        public const string CharactersFolder = "Assets/_FunkyThursday/Art/Characters";
        public const string BackgroundsFolder = "Assets/_FunkyThursday/Art/Backgrounds";
        public const string PlayerId = "vesper";

        static readonly string[] Poses = { "idle", "left", "down", "up", "right", "miss" };

        [MenuItem("Funky Thursday/Link Higgsfield Art")]
        static void LinkMenu()
        {
            int linked = Link(SongLibraryAssets.EnsureLibrary());
            Debug.Log($"Linked {linked} Higgsfield asset(s) into the song data.");
        }

        /// <summary>Fills every slot it can find art for. Returns the number of slots filled.</summary>
        public static int Link(SongLibrary library)
        {
            if (library == null) return 0;

            int linked = FillPoses(library.playerPoses, PlayerId);
            EditorUtility.SetDirty(library);

            for (int i = 0; i < library.Count && i < SongLibraryAssets.Specs.Length; i++)
            {
                SongData song = library.Get(i);
                if (song == null) continue;
                string slug = SongLibraryAssets.Specs[i].Id;

                if (song.opponentPoses == null) song.opponentPoses = new CharacterPuppet.PoseAnimations();
                linked += FillPoses(song.opponentPoses, SongLibraryAssets.Specs[i].OpponentId);

                var backdrop = AssetDatabase.LoadAssetAtPath<SpriteAnimation>($"{BackgroundsFolder}/{slug}/bg_{slug}.asset");
                if (backdrop != null)
                {
                    song.backdrop = backdrop;
                    linked++;
                }

                string video = $"Video/bg_{slug}.mp4";
                if (File.Exists(Path.Combine(Application.streamingAssetsPath, video)))
                {
                    song.backdropVideo = video;
                    linked++;
                }

                EditorUtility.SetDirty(song);
            }

            AssetDatabase.SaveAssets();
            return linked;
        }

        static int FillPoses(CharacterPuppet.PoseAnimations poses, string id)
        {
            string folder = $"{CharactersFolder}/{id}";
            int linked = 0;

            foreach (string pose in Poses)
            {
                var animation = AssetDatabase.LoadAssetAtPath<SpriteAnimation>($"{folder}/{id}_{pose}.asset");
                if (animation == null) continue;
                switch (pose)
                {
                    case "idle": poses.idle = animation; break;
                    case "left": poses.left = animation; break;
                    case "down": poses.down = animation; break;
                    case "up": poses.up = animation; break;
                    case "right": poses.right = animation; break;
                    default: poses.miss = animation; break;
                }
                linked++;
            }

            var icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{id}_icon.png");
            if (icon != null)
            {
                poses.icon = icon;
                linked++;
            }

            var losing = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{id}_icon_losing.png");
            if (losing != null)
            {
                poses.iconLosing = losing;
                linked++;
            }

            return linked;
        }
    }
}

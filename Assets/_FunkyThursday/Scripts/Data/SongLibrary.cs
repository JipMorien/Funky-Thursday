using System;
using FunkyThursday.Core;
using FunkyThursday.Visuals;
using UnityEngine;

namespace FunkyThursday.Data
{
    /// <summary>The ordered list of levels plus the player's look. One asset shared by every scene.</summary>
    [CreateAssetMenu(fileName = "SongLibrary", menuName = "Funky Thursday/Song Library")]
    public sealed class SongLibrary : ScriptableObject
    {
        public SongData[] songs = Array.Empty<SongData>();

        [Header("Player")]
        public CharacterPuppet.PoseAnimations playerPoses = new CharacterPuppet.PoseAnimations();
        public Color playerCloak = new Color32(0x2B, 0x2F, 0x5E, 0xFF);
        public Color playerEyes = new Color32(0x7F, 0xE7, 0xFF, 0xFF);

        [Header("Progression")]
        [Tooltip("Each level unlocks when the previous one is cleared (Options → Unlock All overrides). " +
                 "Secret levels stay hidden until revealed, then play like any other level.")]
        public bool requireUnlocks = true;

        public int Count => songs != null ? songs.Length : 0;

        public SongData Get(int index) => index >= 0 && index < Count ? songs[index] : null;

        public bool IsUnlocked(int index)
        {
            SongData song = Get(index);
            if (song == null) return false;
            if (song.secret) return IsVisible(index);
            if (index == 0 || !requireUnlocks || GameSettings.UnlockAll) return true;
            return PreviousCleared(index);
        }

        /// <summary>
        /// Whether the Level Select lists this song. Normal songs always show (as ??? while locked);
        /// a secret one appears only after its previous level is cleared or the title code is entered.
        /// </summary>
        public bool IsVisible(int index)
        {
            SongData song = Get(index);
            if (song == null) return false;
            if (!song.secret) return true;
            return GameSettings.UnlockAll || SaveData.SecretRevealed || PreviousCleared(index);
        }

        /// <summary>Index of the first secret song, or -1.</summary>
        public int SecretIndex
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    if (songs[i] != null && songs[i].secret) return i;
                }
                return -1;
            }
        }

        bool PreviousCleared(int index)
        {
            SongData previous = Get(index - 1);
            return previous == null || SaveData.IsCleared(previous.id);
        }
    }
}

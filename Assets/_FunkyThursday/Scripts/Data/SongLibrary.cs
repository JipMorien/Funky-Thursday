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
        [Tooltip("Each level unlocks when the previous one is cleared (Options → Unlock All overrides).")]
        public bool requireUnlocks = true;

        public int Count => songs != null ? songs.Length : 0;

        public SongData Get(int index) => index >= 0 && index < Count ? songs[index] : null;

        public bool IsUnlocked(int index)
        {
            if (index <= 0 || !requireUnlocks || GameSettings.UnlockAll) return index >= 0 && index < Count;
            SongData previous = Get(index - 1);
            return previous == null || SaveData.IsCleared(previous.id);
        }
    }
}

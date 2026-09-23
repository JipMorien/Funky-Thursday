using FunkyThursday.Gameplay;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>Per-song progress (cleared flag and best round) stored as JSON in PlayerPrefs.</summary>
    public static class SaveData
    {
        static string Key(string songId) => $"ft.best.{songId}";

        public static bool HasResult(string songId) => PlayerPrefs.HasKey(Key(songId));

        public static bool IsCleared(string songId) => HasResult(songId) && GetBest(songId).cleared;

        public static RoundStats GetBest(string songId)
        {
            string json = PlayerPrefs.GetString(Key(songId), "");
            if (string.IsNullOrEmpty(json)) return default;
            try
            {
                return JsonUtility.FromJson<RoundStats>(json);
            }
            catch (System.ArgumentException)
            {
                return default;
            }
        }

        /// <summary>Stores <paramref name="stats"/> if it beats the saved best. Returns true when it did.</summary>
        public static bool Submit(string songId, RoundStats stats)
        {
            if (string.IsNullOrEmpty(songId)) return false;
            if (HasResult(songId) && !stats.Beats(GetBest(songId))) return false;

            PlayerPrefs.SetString(Key(songId), JsonUtility.ToJson(stats));
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetSong(string songId)
        {
            PlayerPrefs.DeleteKey(Key(songId));
            PlayerPrefs.Save();
        }
    }
}

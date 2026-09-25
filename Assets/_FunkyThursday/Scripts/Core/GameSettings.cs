using System;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Player options, saved with PlayerPrefs (IndexedDB on WebGL). Changes apply immediately
    /// and raise <see cref="Changed"/> so open screens can refresh.
    /// </summary>
    public static class GameSettings
    {
        const string OffsetKey = "ft.settings.offsetMs";
        const string VolumeKey = "ft.settings.volume";
        const string GhostKey = "ft.settings.ghostTapping";
        const string UnlockKey = "ft.settings.unlockAll";
        const string TimingKey = "ft.settings.timing";

        public const float MinOffsetMs = -300f;
        public const float MaxOffsetMs = 300f;

        public static event Action Changed;

        /// <summary>Audio latency compensation fed to the Conductor. Positive = hits were landing late.</summary>
        public static float AudioOffsetMs
        {
            get => PlayerPrefs.GetFloat(OffsetKey, 0f);
            set => Save(() => PlayerPrefs.SetFloat(OffsetKey, Mathf.Clamp(Mathf.Round(value), MinOffsetMs, MaxOffsetMs)));
        }

        /// <summary>Master volume, 0-1.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set => Save(() => PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)));
        }

        public static bool GhostTapping
        {
            get => PlayerPrefs.GetInt(GhostKey, 1) == 1;
            set => Save(() => PlayerPrefs.SetInt(GhostKey, value ? 1 : 0));
        }

        /// <summary>Skip level progression, e.g. for demos.</summary>
        public static bool UnlockAll
        {
            get => PlayerPrefs.GetInt(UnlockKey, 0) == 1;
            set => Save(() => PlayerPrefs.SetInt(UnlockKey, value ? 1 : 0));
        }

        /// <summary>Hit-window leniency; see <see cref="TimingProfile"/>.</summary>
        public static TimingMode Timing
        {
            get
            {
                int value = PlayerPrefs.GetInt(TimingKey, (int)TimingMode.Standard);
                return value >= (int)TimingMode.Standard && value <= (int)TimingMode.Demo ? (TimingMode)value : TimingMode.Standard;
            }
            set => Save(() => PlayerPrefs.SetInt(TimingKey, (int)value));
        }

        /// <summary>Pushes the volume to the AudioListener. Call once per scene.</summary>
        public static void ApplyAudio() => AudioListener.volume = Volume;

        static void Save(Action write)
        {
            write();
            PlayerPrefs.Save();
            ApplyAudio();
            Changed?.Invoke();
        }
    }
}

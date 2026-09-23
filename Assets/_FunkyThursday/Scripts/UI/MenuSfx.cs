using UnityEngine;

namespace FunkyThursday.UI
{
    /// <summary>Synthesised menu blips (scroll, confirm, back, locked), played through one shared source.</summary>
    public static class MenuSfx
    {
        public enum Sound
        {
            Scroll,
            Confirm,
            Back,
            Locked
        }

        const int SampleRate = 44100;
        static AudioSource _source;
        static AudioClip[] _clips;

        public static void Play(Sound sound)
        {
            EnsureReady();
            _source.PlayOneShot(_clips[(int)sound]);
        }

        static void EnsureReady()
        {
            if (_source == null)
            {
                var go = new GameObject("Menu Sfx");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.volume = 0.5f;
            }

            if (_clips == null || _clips[0] == null)
            {
                _clips = new[]
                {
                    Tone("Scroll", 0.05f, 880f, 880f, 60f),
                    Tone("Confirm", 0.22f, 660f, 1320f, 14f),
                    Tone("Back", 0.14f, 520f, 260f, 20f),
                    Tone("Locked", 0.16f, 140f, 110f, 18f),
                };
            }
        }

        static AudioClip Tone(string name, float seconds, float fromHz, float toHz, float decay)
        {
            int n = (int)(seconds * SampleRate);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(fromHz, toHz, t / seconds);
                phase += 2f * Mathf.PI * f / SampleRate;
                float square = Mathf.Sin(phase) >= 0f ? 1f : -1f; // chiptune square
                data[i] = square * 0.25f * Mathf.Exp(-t * decay);
            }
            var clip = AudioClip.Create($"Sfx_{name}", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

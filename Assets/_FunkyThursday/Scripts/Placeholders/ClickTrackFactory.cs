using System;
using UnityEngine;

namespace FunkyThursday.Placeholders
{
    /// <summary>
    /// Synthesises a metronome AudioClip (high click every beat, low bell toll on each downbeat)
    /// so the Conductor can be tested before any real song exists.
    /// </summary>
    public static class ClickTrackFactory
    {
        const int SampleRate = 44100;
        const float ClickSeconds = 0.045f;
        const float TollSeconds = 0.35f;

        public static AudioClip Create(float bpm, int beats, int beatsPerBar = 4)
        {
            if (bpm <= 0f) throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be positive.");
            if (beats <= 0) throw new ArgumentOutOfRangeException(nameof(beats), "Beat count must be positive.");
            if (beatsPerBar <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerBar));

            double secondsPerBeat = 60.0 / bpm;
            int totalSamples = (int)Math.Ceiling(secondsPerBeat * beats * SampleRate);
            var data = new float[totalSamples];

            for (int beat = 0; beat < beats; beat++)
            {
                int start = (int)Math.Round(beat * secondsPerBeat * SampleRate);
                bool downbeat = beat % beatsPerBar == 0;

                AddTone(data, start, ClickSeconds, downbeat ? 1568f : 1046.5f, downbeat ? 0.45f : 0.3f, 90f);
                if (downbeat) AddTone(data, start, TollSeconds, 98f, 0.4f, 10f);
            }

            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);

            var clip = AudioClip.Create($"ClickTrack_{bpm:0}bpm", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddTone(float[] data, int start, float seconds, float frequency, float amplitude, float decay)
        {
            int length = (int)(seconds * SampleRate);
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                data[start + i] += Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay) * amplitude;
            }
        }
    }
}

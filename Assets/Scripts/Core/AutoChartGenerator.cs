using System.Collections.Generic;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// Tunable parameters for AutoChartGenerator's onset detector.
    /// </summary>
    [System.Serializable]
    public class AutoChartSettings
    {
        [Tooltip("Size in seconds of each energy analysis window. Smaller = more precise timing, slower to process.")]
        public float windowSizeSeconds = 0.02f;

        [Tooltip("How many seconds of recent history the local average energy is computed over.")]
        public float historySizeSeconds = 1.0f;

        [Tooltip("A window must exceed the local average energy by this factor to be considered an onset. Lower = more notes.")]
        public float sensitivity = 1.0f;

        [Tooltip("Minimum seconds between two detected onsets, to avoid rapid-fire duplicate hits on the same sound.")]
        public float minSpacingSeconds = 0.12f;

        [Tooltip("Snap detected onset/release times onto a BPM-relative grid, so notes line up rhythmically instead of landing on raw audio jitter.")]
        public bool quantizeToGrid = true;

        [Tooltip("Grid resolution in subdivisions per beat when quantizing (1 = only full beats, matching the beat " +
                 "pulse exactly; 2 = eighth notes, can land between pulses; 4 = sixteenth notes).")]
        public int gridDivisionsPerBeat = 1;

        [Tooltip("Minimum held duration (after quantizing) for a sound to become a sustain/hold note instead of " +
                 "a plain tap. Sounds too short to time precisely as a tap, but long enough to hold, land here - " +
                 "like a long tile in Piano Tiles.")]
        public float minSustainSeconds = 0.3f;

        public static AutoChartSettings Default => new AutoChartSettings();
    }

    /// <summary>
    /// Generates a playable ChartData straight from a song's audio, using a
    /// simple energy-based onset detector: it tracks a short-term energy
    /// window against a longer rolling average and flags an event wherever
    /// the short-term energy spikes above that average by a set factor. The
    /// same signal's falling edge (when energy drops back under the average)
    /// marks the release, so sustained sounds - a held chord, a long vocal
    /// note, a drone - naturally produce a note with a matching hold length
    /// instead of just a single tap at the onset.
    /// This is a classic, easily-explainable beat detection approach (no
    /// FFT or ML involved) - good enough to rough out a chart automatically,
    /// though it won't be as musical as a hand-authored one.
    /// </summary>
    public static class AutoChartGenerator
    {
        /// <summary>
        /// Generates a chart. If tempoMap is provided (see DetectTempoMap), quantization
        /// follows the song's actual per-segment tempo instead of a single flat bpm -
        /// use this for songs that speed up/slow down mid-track.
        /// </summary>
        public static ChartData Generate(AudioClip clip, string songName, float bpm, AutoChartSettings settings = null, TempoMap tempoMap = null)
        {
            settings ??= AutoChartSettings.Default;

            List<(float time, float sustainLength)> rawEvents = GetRawOnsetEvents(clip, settings);
            if (rawEvents == null)
            {
                return null;
            }

            List<NoteData> notes;

            if (!settings.quantizeToGrid)
            {
                notes = BuildNotesFromEvents(rawEvents);
            }
            else if (tempoMap != null && tempoMap.changePoints.Count > 0)
            {
                notes = BuildNotesFromEvents(QuantizeEventsWithTempoMap(rawEvents, tempoMap, settings));
            }
            else if (bpm > 0f)
            {
                notes = BuildNotesFromEvents(QuantizeEvents(rawEvents, bpm, settings));
            }
            else
            {
                notes = BuildNotesFromEvents(rawEvents);
            }

            int sustainCount = notes.FindAll(n => n.sustainLength > 0f).Count;
            UnityEngine.Debug.Log($"[AutoChartGenerator] Generated {notes.Count} notes from '{clip.name}' ({sustainCount} sustained).");

            return new ChartData
            {
                song = songName,
                bpm = bpm,
                notes = notes,
            };
        }

        /// <summary>
        /// Estimates the song's BPM from its own audio, by looking at the
        /// spacing between detected onsets. Each inter-onset gap is folded
        /// (by repeatedly doubling/halving) into the [minBpm, maxBpm) range
        /// as a tempo "vote", and the most commonly voted-for tempo wins -
        /// a simple, classic histogram approach to tempo estimation.
        /// Returns null if the clip couldn't be read or had too few onsets
        /// to make a reasonable estimate.
        /// </summary>
        public static float? DetectBpm(AudioClip clip, AutoChartSettings settings = null, float minBpm = 60f, float maxBpm = 200f)
        {
            settings ??= AutoChartSettings.Default;

            List<float> onsetTimes = GetRawOnsetTimes(clip, settings);
            if (onsetTimes == null || onsetTimes.Count < 2)
            {
                UnityEngine.Debug.LogWarning("[AutoChartGenerator] Not enough detected onsets to estimate BPM.");
                return null;
            }

            float? bpm = EstimateBpmFromOnsets(onsetTimes, minBpm, maxBpm, out int votes);
            if (!bpm.HasValue)
            {
                UnityEngine.Debug.LogWarning("[AutoChartGenerator] Could not derive any BPM candidates from onset spacing.");
                return null;
            }

            UnityEngine.Debug.Log($"[AutoChartGenerator] Detected BPM: {bpm.Value:F0} ({votes}/{onsetTimes.Count - 1} interval votes) for '{clip.name}'.");
            return bpm;
        }

        /// <summary>
        /// Estimates a piecewise tempo map by independently detecting BPM in
        /// successive time windows (default 15s) and starting a new segment
        /// only where the tempo shifts by more than changeThresholdBpm - use
        /// this instead of DetectBpm for songs that speed up or slow down.
        /// Windows with too few onsets to trust (below minVotesToTrust) are
        /// skipped rather than guessed, carrying the last trusted tempo
        /// forward instead of introducing a spurious change point.
        /// </summary>
        public static TempoMap DetectTempoMap(
            AudioClip clip,
            AutoChartSettings settings = null,
            float windowSeconds = 15f,
            float minBpm = 60f,
            float maxBpm = 200f,
            int minVotesToTrust = 3,
            float changeThresholdBpm = 3f)
        {
            settings ??= AutoChartSettings.Default;

            List<float> onsetTimes = GetRawOnsetTimes(clip, settings);
            if (onsetTimes == null || onsetTimes.Count < 4)
            {
                UnityEngine.Debug.LogWarning("[AutoChartGenerator] Too few detected onsets to build a tempo map.");
                return null;
            }

            float duration = clip.length;
            int windowCount = Mathf.Max(1, Mathf.CeilToInt(duration / windowSeconds));

            List<(float startTime, float bpm)> trustedWindows = new List<(float, float)>();
            float? lastTrustedBpm = null;

            for (int w = 0; w < windowCount; w++)
            {
                float windowStart = w * windowSeconds;
                float windowEnd = Mathf.Min(duration, windowStart + windowSeconds);

                List<float> onsetsInWindow = onsetTimes.FindAll(t => t >= windowStart && t < windowEnd);
                float? estimate = EstimateBpmFromOnsets(onsetsInWindow, minBpm, maxBpm, out int votes);

                if (!estimate.HasValue || votes < minVotesToTrust)
                {
                    // Not enough evidence in this window (e.g. a quiet stretch) -
                    // carry the last trusted tempo forward instead of guessing.
                    if (lastTrustedBpm.HasValue)
                    {
                        trustedWindows.Add((windowStart, lastTrustedBpm.Value));
                    }
                    continue;
                }

                lastTrustedBpm = estimate.Value;
                trustedWindows.Add((windowStart, estimate.Value));
            }

            if (trustedWindows.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[AutoChartGenerator] No window had enough onsets to trust a tempo estimate; no tempo map produced.");
                return null;
            }

            TempoMap map = new TempoMap();
            float currentSegmentBpm = trustedWindows[0].bpm;
            map.changePoints.Add(new TempoChangePoint { timeSeconds = 0f, bpm = currentSegmentBpm });

            for (int i = 1; i < trustedWindows.Count; i++)
            {
                float candidateBpm = trustedWindows[i].bpm;
                if (Mathf.Abs(candidateBpm - currentSegmentBpm) > changeThresholdBpm)
                {
                    currentSegmentBpm = candidateBpm;
                    map.changePoints.Add(new TempoChangePoint { timeSeconds = trustedWindows[i].startTime, bpm = currentSegmentBpm });
                }
            }

            List<string> segmentSummaries = map.changePoints.ConvertAll(cp => $"{cp.bpm:F0} bpm @ {cp.timeSeconds:F1}s");
            UnityEngine.Debug.Log($"[AutoChartGenerator] Tempo map for '{clip.name}': {map.changePoints.Count} segment(s) - {string.Join(", ", segmentSummaries)}");

            return map;
        }

        private static float? EstimateBpmFromOnsets(List<float> onsetTimes, float minBpm, float maxBpm, out int voteCount)
        {
            voteCount = 0;

            if (onsetTimes == null || onsetTimes.Count < 2)
            {
                return null;
            }

            Dictionary<int, int> votesByBpm = new Dictionary<int, int>();

            for (int i = 1; i < onsetTimes.Count; i++)
            {
                float interval = onsetTimes[i] - onsetTimes[i - 1];
                if (interval <= 0.05f)
                {
                    continue;
                }

                float candidateBpm = 60f / interval;
                while (candidateBpm < minBpm)
                {
                    candidateBpm *= 2f;
                }
                while (candidateBpm >= maxBpm)
                {
                    candidateBpm /= 2f;
                }

                int bucket = Mathf.RoundToInt(candidateBpm);
                votesByBpm.TryGetValue(bucket, out int count);
                votesByBpm[bucket] = count + 1;
            }

            if (votesByBpm.Count == 0)
            {
                return null;
            }

            int bestBpm = 0;
            int bestVotes = -1;
            foreach (KeyValuePair<int, int> candidate in votesByBpm)
            {
                if (candidate.Value > bestVotes)
                {
                    bestVotes = candidate.Value;
                    bestBpm = candidate.Key;
                }
            }

            voteCount = bestVotes;
            return bestBpm;
        }

        private static List<float> GetRawOnsetTimes(AudioClip clip, AutoChartSettings settings)
        {
            List<(float time, float sustainLength)> events = GetRawOnsetEvents(clip, settings);
            return events?.ConvertAll(e => e.time);
        }

        private static List<(float time, float sustainLength)> GetRawOnsetEvents(AudioClip clip, AutoChartSettings settings)
        {
            if (clip == null)
            {
                UnityEngine.Debug.LogError("[AutoChartGenerator] No AudioClip provided.");
                return null;
            }

            if (clip.loadType == AudioClipLoadType.Streaming)
            {
                UnityEngine.Debug.LogError(
                    $"[AutoChartGenerator] '{clip.name}' uses the Streaming load type, which does not support " +
                    "reading sample data. Set its Load Type to 'Decompress On Load' or 'Compressed In Memory' in the import settings.");
                return null;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    UnityEngine.Debug.LogError($"[AutoChartGenerator] Could not load audio data for '{clip.name}'.");
                    return null;
                }
            }

            float[] monoSamples = ExtractMonoSamples(clip);
            return DetectOnsetEvents(monoSamples, clip.frequency, settings);
        }

        private static float[] ExtractMonoSamples(AudioClip clip)
        {
            int channels = clip.channels;
            float[] raw = new float[clip.samples * channels];
            clip.GetData(raw, 0);

            if (channels == 1)
            {
                return raw;
            }

            float[] mono = new float[clip.samples];
            for (int i = 0; i < clip.samples; i++)
            {
                float sum = 0f;
                int baseIndex = i * channels;
                for (int c = 0; c < channels; c++)
                {
                    sum += raw[baseIndex + c];
                }
                mono[i] = sum / channels;
            }

            return mono;
        }

        /// <summary>
        /// Walks the same energy-vs-rolling-average signal used for plain
        /// onset detection, but keeps each event "open" for as long as the
        /// signal stays above threshold, closing it - and recording how long
        /// it stayed open - the moment energy drops back down. A quick
        /// percussive hit closes on the very next window (sustainLength stays
        /// ~0, ending up a tap after quantization); a sustained note stays
        /// open for many windows, giving it a real hold length.
        /// </summary>
        private static List<(float time, float sustainLength)> DetectOnsetEvents(float[] samples, int sampleRate, AutoChartSettings settings)
        {
            List<(float time, float sustainLength)> events = new List<(float time, float sustainLength)>();

            int windowSize = Mathf.Max(1, Mathf.RoundToInt(settings.windowSizeSeconds * sampleRate));
            int historyWindowCount = Mathf.Max(1, Mathf.RoundToInt(settings.historySizeSeconds / settings.windowSizeSeconds));
            int minSpacingSamples = Mathf.RoundToInt(settings.minSpacingSeconds * sampleRate);

            Queue<float> energyHistory = new Queue<float>(historyWindowCount);
            float historySum = 0f;

            bool aboveThreshold = false;
            int samplesSinceLastOnset = minSpacingSamples;
            int openEventIndex = -1;

            for (int start = 0; start + windowSize <= samples.Length; start += windowSize)
            {
                float energy = 0f;
                for (int i = 0; i < windowSize; i++)
                {
                    float s = samples[start + i];
                    energy += s * s;
                }

                float localAverage = energyHistory.Count > 0 ? historySum / energyHistory.Count : energy;
                bool isLoudEnough = energyHistory.Count >= historyWindowCount && energy > localAverage * settings.sensitivity;
                float currentTime = (float)start / sampleRate;

                if (isLoudEnough)
                {
                    if (!aboveThreshold && samplesSinceLastOnset >= minSpacingSamples)
                    {
                        events.Add((currentTime, 0f));
                        openEventIndex = events.Count - 1;
                        samplesSinceLastOnset = 0;
                    }
                    aboveThreshold = true;
                }
                else
                {
                    if (aboveThreshold && openEventIndex >= 0)
                    {
                        (float time, float sustainLength) ev = events[openEventIndex];
                        ev.sustainLength = currentTime - ev.time;
                        events[openEventIndex] = ev;
                        openEventIndex = -1;
                    }
                    aboveThreshold = false;
                }

                samplesSinceLastOnset += windowSize;

                energyHistory.Enqueue(energy);
                historySum += energy;
                if (energyHistory.Count > historyWindowCount)
                {
                    historySum -= energyHistory.Dequeue();
                }
            }

            // The clip ended while still "loud" (e.g. a note ringing out to the end) - close it at the clip's end.
            if (openEventIndex >= 0)
            {
                (float time, float sustainLength) ev = events[openEventIndex];
                ev.sustainLength = ((float)samples.Length / sampleRate) - ev.time;
                events[openEventIndex] = ev;
            }

            return events;
        }

        private static List<(float time, float sustainLength)> QuantizeEvents(
            List<(float time, float sustainLength)> events, float bpm, AutoChartSettings settings)
        {
            int divisionsPerBeat = Mathf.Max(1, settings.gridDivisionsPerBeat);
            float secondsPerBeat = 60f / bpm;
            float gridStep = secondsPerBeat / divisionsPerBeat;

            List<(float time, float sustainLength)> quantized = new List<(float, float)>(events.Count);
            foreach ((float time, float sustainLength) e in events)
            {
                float qStart = Mathf.Round(e.time / gridStep) * gridStep;
                float qEnd = Mathf.Round((e.time + e.sustainLength) / gridStep) * gridStep;
                quantized.Add((qStart, ClampSustain(qEnd - qStart, settings.minSustainSeconds)));
            }

            return DedupeByMinSpacing(quantized, settings.minSpacingSeconds);
        }

        private static List<(float time, float sustainLength)> QuantizeEventsWithTempoMap(
            List<(float time, float sustainLength)> events, TempoMap tempoMap, AutoChartSettings settings)
        {
            int divisionsPerBeat = Mathf.Max(1, settings.gridDivisionsPerBeat);

            List<(float time, float sustainLength)> quantized = new List<(float, float)>(events.Count);
            foreach ((float time, float sustainLength) e in events)
            {
                float startBeats = tempoMap.TimeToBeats(e.time);
                float endBeats = tempoMap.TimeToBeats(e.time + e.sustainLength);

                float qStartBeats = Mathf.Round(startBeats * divisionsPerBeat) / divisionsPerBeat;
                float qEndBeats = Mathf.Round(endBeats * divisionsPerBeat) / divisionsPerBeat;

                float qStart = tempoMap.BeatsToTime(qStartBeats);
                float qEnd = tempoMap.BeatsToTime(qEndBeats);
                quantized.Add((qStart, ClampSustain(qEnd - qStart, settings.minSustainSeconds)));
            }

            return DedupeByMinSpacing(quantized, settings.minSpacingSeconds);
        }

        private static float ClampSustain(float sustainLength, float minSustainSeconds)
        {
            sustainLength = Mathf.Max(0f, sustainLength);
            return sustainLength < minSustainSeconds ? 0f : sustainLength;
        }

        private static List<(float time, float sustainLength)> DedupeByMinSpacing(
            List<(float time, float sustainLength)> events, float minSpacingSeconds)
        {
            events.Sort((a, b) => a.time.CompareTo(b.time));

            // Quantizing can collapse two nearby events onto the same grid slot; drop the duplicates.
            for (int i = events.Count - 1; i > 0; i--)
            {
                if (events[i].time - events[i - 1].time < minSpacingSeconds * 0.5f)
                {
                    events.RemoveAt(i);
                }
            }

            return events;
        }

        private static List<NoteData> BuildNotesFromEvents(List<(float time, float sustainLength)> events)
        {
            const int laneCount = 4;
            List<NoteData> notes = new List<NoteData>(events.Count);

            for (int i = 0; i < events.Count; i++)
            {
                notes.Add(new NoteData
                {
                    time = events[i].time,
                    lane = i % laneCount,
                    sustainLength = events[i].sustainLength,
                });
            }

            return notes;
        }
    }
}

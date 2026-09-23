using System;
using System.Collections.Generic;
using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Charts
{
    public sealed class ChartFormatException : Exception
    {
        public ChartFormatException(string message) : base(message) { }
    }

    /// <summary>
    /// Parses chart JSON, validates every note and converts beats to seconds. Fatal problems throw
    /// <see cref="ChartFormatException"/>; recoverable ones (duplicates, overlapping notes) are
    /// dropped with a warning so a hand-edited chart still loads.
    /// </summary>
    public static class ChartLoader
    {
        public const int SupportedFormatVersion = 1;
        const double DuplicateToleranceSeconds = 0.001;

        public static Chart Load(TextAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            return Parse(asset.text, asset.name);
        }

        public static Chart Parse(string json, string sourceName = "chart")
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ChartFormatException($"{sourceName}: file is empty.");

            ChartData data;
            try
            {
                data = JsonUtility.FromJson<ChartData>(json);
            }
            catch (ArgumentException e)
            {
                throw new ChartFormatException($"{sourceName}: invalid JSON ({e.Message}).");
            }

            if (data == null) throw new ChartFormatException($"{sourceName}: could not read chart.");
            if (data.formatVersion > SupportedFormatVersion)
                throw new ChartFormatException($"{sourceName}: format version {data.formatVersion} is newer than supported ({SupportedFormatVersion}).");
            if (data.bpm <= 0f) throw new ChartFormatException($"{sourceName}: bpm must be positive.");
            if (data.scrollSpeed <= 0f) data.scrollSpeed = 1f;
            if (data.ai == null) data.ai = new ChartAiSettings();
            if (data.notes == null) data.notes = Array.Empty<ChartNoteData>();

            double secondsPerBeat = 60.0 / data.bpm;
            double offset = data.offsetMs * 0.001;
            var notes = new List<ChartNote>(data.notes.Length);

            for (int i = 0; i < data.notes.Length; i++)
            {
                ChartNoteData raw = data.notes[i];
                if (raw.lane < 0 || raw.lane >= Lanes.Count)
                    throw new ChartFormatException($"{sourceName}: note {i} has lane {raw.lane}; expected 0-3.");
                if (raw.side != (int)StrumSide.Opponent && raw.side != (int)StrumSide.Player)
                    throw new ChartFormatException($"{sourceName}: note {i} has side {raw.side}; expected 0 or 1.");
                if (raw.beat < 0f || raw.hold < 0f || float.IsNaN(raw.beat) || float.IsNaN(raw.hold))
                    throw new ChartFormatException($"{sourceName}: note {i} has a negative or invalid beat/hold.");

                notes.Add(new ChartNote(
                    offset + raw.beat * secondsPerBeat,
                    raw.hold * secondsPerBeat,
                    raw.beat,
                    (NoteLane)raw.lane,
                    (StrumSide)raw.side));
            }

            notes.Sort((a, b) =>
            {
                int byTime = a.Time.CompareTo(b.Time);
                if (byTime != 0) return byTime;
                int bySide = a.Side.CompareTo(b.Side);
                return bySide != 0 ? bySide : a.Lane.CompareTo(b.Lane);
            });

            List<ChartNote> clean = RemoveConflicts(notes, sourceName);
            int playerNotes = 0;
            foreach (ChartNote note in clean)
            {
                if (note.Side == StrumSide.Player) playerNotes++;
            }

            if (data.lengthBeats <= 0f && clean.Count > 0)
            {
                ChartNote last = clean[clean.Count - 1];
                data.lengthBeats = (float)((last.EndTime - offset) / secondsPerBeat) + data.beatsPerBar;
            }

            return new Chart(data, clean, playerNotes);
        }

        /// <summary>Drops exact duplicates and notes that start inside a hold on the same lane.</summary>
        static List<ChartNote> RemoveConflicts(List<ChartNote> sorted, string sourceName)
        {
            var result = new List<ChartNote>(sorted.Count);
            var laneBusyUntil = new double[2, Lanes.Count];
            var lastStart = new double[2, Lanes.Count];
            for (int s = 0; s < 2; s++)
            {
                for (int l = 0; l < Lanes.Count; l++)
                {
                    laneBusyUntil[s, l] = double.NegativeInfinity;
                    lastStart[s, l] = double.NegativeInfinity;
                }
            }

            int dropped = 0;
            foreach (ChartNote note in sorted)
            {
                int s = (int)note.Side;
                int l = (int)note.Lane;
                bool duplicate = Math.Abs(note.Time - lastStart[s, l]) < DuplicateToleranceSeconds;
                bool insideHold = note.Time < laneBusyUntil[s, l];

                if (duplicate || insideHold)
                {
                    dropped++;
                    continue;
                }

                result.Add(note);
                lastStart[s, l] = note.Time;
                laneBusyUntil[s, l] = note.EndTime;
            }

            if (dropped > 0) Debug.LogWarning($"{sourceName}: dropped {dropped} duplicate or overlapping note(s).");
            return result;
        }
    }
}

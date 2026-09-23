using System.Collections.Generic;
using FunkyThursday.Core;

namespace FunkyThursday.Charts
{
    /// <summary>A validated chart note with its time resolved to seconds.</summary>
    public readonly struct ChartNote
    {
        public readonly double Time;
        public readonly double HoldSeconds;
        public readonly float Beat;
        public readonly NoteLane Lane;
        public readonly StrumSide Side;

        public ChartNote(double time, double holdSeconds, float beat, NoteLane lane, StrumSide side)
        {
            Time = time;
            HoldSeconds = holdSeconds;
            Beat = beat;
            Lane = lane;
            Side = side;
        }

        public bool IsHold => HoldSeconds > 0.0;
        public double EndTime => Time + HoldSeconds;
    }

    /// <summary>Runtime chart: metadata plus notes sorted by time. Built by <see cref="ChartLoader"/>.</summary>
    public sealed class Chart
    {
        public Chart(ChartData data, IReadOnlyList<ChartNote> notes, int playerNoteCount)
        {
            Data = data;
            Notes = notes;
            PlayerNoteCount = playerNoteCount;
        }

        public ChartData Data { get; }
        public IReadOnlyList<ChartNote> Notes { get; }
        public int PlayerNoteCount { get; }

        public string Song => Data.song;
        public float Bpm => Data.bpm;
        public float ScrollSpeed => Data.scrollSpeed;
        public double SecondsPerBeat => 60.0 / Data.bpm;
        public double LengthSeconds => Data.offsetMs * 0.001 + Data.lengthBeats * SecondsPerBeat;
    }
}

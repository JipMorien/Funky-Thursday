using System;

namespace FunkyThursday.Charts
{
    /// <summary>
    /// Raw chart JSON as written by Tools/song_forge.py (or by hand). Field names match the file
    /// exactly because JsonUtility maps by name. Times are in beats so charts survive BPM edits.
    /// </summary>
    [Serializable]
    public sealed class ChartData
    {
        public int formatVersion = 1;
        public string song;
        public int level;
        public string stage;
        public string opponentName;
        public string difficulty;
        public float bpm;
        public int beatsPerBar = 4;
        public float scrollSpeed = 1f;

        /// <summary>Audio time (ms) of beat 0. Positive when the audio has leading silence.</summary>
        public float offsetMs;

        public float lengthBeats;
        public ChartAiSettings ai = new ChartAiSettings();
        public ChartNoteData[] notes = Array.Empty<ChartNoteData>();
    }

    [Serializable]
    public sealed class ChartAiSettings
    {
        /// <summary>Health taken from the player each time the opponent hits a note (0 = classic FNF).</summary>
        public float healthDrainPerNote;

        /// <summary>The opponent's drain never pushes player health below this value.</summary>
        public float drainFloor;
    }

    [Serializable]
    public struct ChartNoteData
    {
        public float beat;
        public int lane;   // 0 Left, 1 Down, 2 Up, 3 Right
        public int side;   // 0 Opponent, 1 Player
        public float hold; // sustain length in beats, 0 for a tap
    }
}

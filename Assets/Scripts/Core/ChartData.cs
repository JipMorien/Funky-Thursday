using System.Collections.Generic;

namespace FunkyThursday.Core
{
    /// <summary>
    /// A single note entry from a chart JSON file.
    /// </summary>
    [System.Serializable]
    public class NoteData
    {
        /// <summary>Time in seconds from song start that this note must be hit.</summary>
        public float time;

        /// <summary>Lane index: 0 = left, 1 = down, 2 = up, 3 = right.</summary>
        public int lane;

        /// <summary>Length in seconds of the hold tail. 0 for a plain tap note.</summary>
        public float sustainLength;
    }

    /// <summary>
    /// A full chart: song metadata plus its list of notes, matching the
    /// on-disk JSON format read by ChartLoader.
    /// </summary>
    [System.Serializable]
    public class ChartData
    {
        public string song;
        public float bpm;
        public List<NoteData> notes;
    }
}

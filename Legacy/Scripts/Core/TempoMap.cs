using System.Collections.Generic;
using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>
    /// A single point where the song's tempo becomes (or continues at) a
    /// given BPM, starting at timeSeconds and lasting until the next point.
    /// </summary>
    [System.Serializable]
    public class TempoChangePoint
    {
        public float timeSeconds;
        public float bpm;
    }

    /// <summary>
    /// A piecewise-constant tempo over the course of a song, for tracks
    /// that speed up or slow down instead of holding one BPM throughout.
    /// changePoints must be sorted ascending by timeSeconds, with the first
    /// entry's timeSeconds normally at (or before) 0.
    /// </summary>
    [System.Serializable]
    public class TempoMap
    {
        public List<TempoChangePoint> changePoints = new List<TempoChangePoint>();

        /// <summary>Converts a song time in seconds into an absolute beat position.</summary>
        public float TimeToBeats(float time)
        {
            if (changePoints == null || changePoints.Count == 0)
            {
                return 0f;
            }

            float beats = 0f;

            for (int i = 0; i < changePoints.Count; i++)
            {
                float segmentStart = changePoints[i].timeSeconds;
                float segmentEnd = (i + 1 < changePoints.Count) ? changePoints[i + 1].timeSeconds : float.PositiveInfinity;
                float secPerBeat = 60f / changePoints[i].bpm;

                if (i == 0 && time < segmentStart)
                {
                    // Before the map's first point: extrapolate backward at its tempo.
                    return (time - segmentStart) / secPerBeat;
                }

                if (time < segmentEnd || float.IsPositiveInfinity(segmentEnd))
                {
                    beats += (time - segmentStart) / secPerBeat;
                    return beats;
                }

                beats += (segmentEnd - segmentStart) / secPerBeat;
            }

            return beats;
        }

        /// <summary>Converts an absolute beat position back into a song time in seconds.</summary>
        public float BeatsToTime(float beats)
        {
            if (changePoints == null || changePoints.Count == 0)
            {
                return 0f;
            }

            float accumulatedBeats = 0f;

            for (int i = 0; i < changePoints.Count; i++)
            {
                float segmentStart = changePoints[i].timeSeconds;
                float segmentEnd = (i + 1 < changePoints.Count) ? changePoints[i + 1].timeSeconds : float.PositiveInfinity;
                float secPerBeat = 60f / changePoints[i].bpm;
                bool segmentIsUnbounded = float.IsPositiveInfinity(segmentEnd);
                float segmentBeats = segmentIsUnbounded ? float.PositiveInfinity : (segmentEnd - segmentStart) / secPerBeat;

                if (i == 0 && beats < 0f)
                {
                    return segmentStart + beats * secPerBeat;
                }

                if (segmentIsUnbounded || beats <= accumulatedBeats + segmentBeats)
                {
                    float beatsIntoSegment = beats - accumulatedBeats;
                    return segmentStart + beatsIntoSegment * secPerBeat;
                }

                accumulatedBeats += segmentBeats;
            }

            TempoChangePoint last = changePoints[changePoints.Count - 1];
            return last.timeSeconds;
        }

        public float GetBpmAtTime(float time)
        {
            if (changePoints == null || changePoints.Count == 0)
            {
                return 0f;
            }

            float bpm = changePoints[0].bpm;
            for (int i = 0; i < changePoints.Count; i++)
            {
                if (changePoints[i].timeSeconds > time)
                {
                    break;
                }
                bpm = changePoints[i].bpm;
            }
            return bpm;
        }
    }
}

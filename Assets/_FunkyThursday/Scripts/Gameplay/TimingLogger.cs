using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// FMOD spike: records every hit's timing offset and the conductor's clock behaviour, and appends
    /// one CSV row per finished round to &lt;persistentDataPath&gt;/fmod-spike-timing.csv. Play the same
    /// song with Options → Audio Engine on UNITY and on FMOD, then compare the rows.
    /// Rounds without player hits (botplay) are skipped.
    /// </summary>
    public sealed class TimingLogger : MonoBehaviour
    {
        public const string FileName = "fmod-spike-timing.csv";

        const string Header = "date,engine,song,timing_mode,audio_offset_ms,result,hits,misses," +
                              "mean_offset_ms,sd_offset_ms,mean_abs_offset_ms," +
                              "clock_updates,mean_clock_step_ms,mean_clock_error_ms,max_clock_error_ms,hard_resyncs";

        [SerializeField] GameplayController controller;
        [SerializeField] HitJudge judge;
        [SerializeField] Conductor conductor;

        readonly List<double> _offsets = new List<double>();
        int _misses;

        public static string LogPath => Path.Combine(Application.persistentDataPath, FileName);

        void OnEnable()
        {
            if (judge != null) judge.Judged += HandleJudged;
            if (controller != null) controller.RoundEnded += HandleRoundEnded;
            if (conductor != null) conductor.BeatHit += HandleBeat;
        }

        void OnDisable()
        {
            if (judge != null) judge.Judged -= HandleJudged;
            if (controller != null) controller.RoundEnded -= HandleRoundEnded;
            if (conductor != null) conductor.BeatHit -= HandleBeat;
        }

        /// <summary>The count-in beats are negative: a new round has started, so start a fresh sample.</summary>
        void HandleBeat(int beat)
        {
            if (beat < 0 && (_offsets.Count > 0 || _misses > 0)) Clear();
        }

        void HandleJudged(HitResult result)
        {
            if (result.Kind != HitKind.Tap) return;
            if (result.Judgement == Judgement.Miss) _misses++;
            else _offsets.Add(result.OffsetMs);
        }

        void HandleRoundEnded(GameplayController.RoundState state, RoundStats stats)
        {
            if (_offsets.Count == 0)
            {
                Clear();
                return;
            }

            double mean = 0.0, meanAbs = 0.0;
            foreach (double o in _offsets)
            {
                mean += o;
                meanAbs += Math.Abs(o);
            }
            mean /= _offsets.Count;
            meanAbs /= _offsets.Count;

            double variance = 0.0;
            foreach (double o in _offsets) variance += (o - mean) * (o - mean);
            double sd = Math.Sqrt(variance / _offsets.Count);

            Conductor.ClockReport clock = conductor.ClockStats;
            string song = controller.CurrentSong != null ? controller.CurrentSong.id : "unknown";

            string row = string.Join(",",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                clock.backend,
                song,
                GameSettings.Timing.ToString(),
                F(GameSettings.AudioOffsetMs),
                state.ToString(),
                _offsets.Count.ToString(CultureInfo.InvariantCulture),
                _misses.ToString(CultureInfo.InvariantCulture),
                F(mean), F(sd), F(meanAbs),
                clock.clockUpdates.ToString(CultureInfo.InvariantCulture),
                F(clock.meanClockStepMs), F(clock.meanClockErrorMs), F(clock.maxClockErrorMs),
                clock.hardResyncs.ToString(CultureInfo.InvariantCulture));

            try
            {
                bool isNew = !File.Exists(LogPath);
                using (var writer = new StreamWriter(LogPath, append: true))
                {
                    if (isNew) writer.WriteLine(Header);
                    writer.WriteLine(row);
                }
                Debug.Log($"Timing logged ({clock.backend}): mean {F(mean)} ms, sd {F(sd)} ms, clock step {F(clock.meanClockStepMs)} ms -> {LogPath}");
            }
            catch (IOException e)
            {
                Debug.LogWarning($"Could not write {LogPath}: {e.Message}");
            }

            Clear();
        }

        void Clear()
        {
            _offsets.Clear();
            _misses = 0;
        }

        static string F(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    }
}

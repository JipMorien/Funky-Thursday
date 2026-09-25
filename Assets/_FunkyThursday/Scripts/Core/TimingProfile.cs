namespace FunkyThursday.Core
{
    /// <summary>How forgiving a round is. Chosen in Options → Timing.</summary>
    public enum TimingMode
    {
        /// <summary>FNF-style windows: 45 / 90 / 135 ms.</summary>
        Standard,
        /// <summary>Wider windows and softer misses, for newer players.</summary>
        Relaxed,
        /// <summary>Very wide windows, near-harmless misses and no opponent drain: showcase runs.</summary>
        Demo
    }

    /// <summary>
    /// Hit windows (ms either side of the note) and health scaling for one <see cref="TimingMode"/>.
    /// HitJudge and HealthSystem read it at the start of every round.
    /// </summary>
    public readonly struct TimingProfile
    {
        public readonly float PerfectWindowMs;
        public readonly float GoodWindowMs;
        public readonly float MissWindowMs;
        /// <summary>Multiplies every health loss from misses, dropped holds and ghost taps.</summary>
        public readonly float LossScale;
        /// <summary>Multiplies the opponent's per-note health drain.</summary>
        public readonly float DrainScale;

        TimingProfile(float perfect, float good, float miss, float lossScale, float drainScale)
        {
            PerfectWindowMs = perfect;
            GoodWindowMs = good;
            MissWindowMs = miss;
            LossScale = lossScale;
            DrainScale = drainScale;
        }

        public static TimingProfile For(TimingMode mode)
        {
            switch (mode)
            {
                case TimingMode.Relaxed: return new TimingProfile(70f, 125f, 170f, 0.6f, 0.5f);
                case TimingMode.Demo: return new TimingProfile(120f, 190f, 230f, 0.25f, 0f);
                default: return new TimingProfile(45f, 90f, 135f, 1f, 1f);
            }
        }

        public static string Label(TimingMode mode)
        {
            switch (mode)
            {
                case TimingMode.Relaxed: return "RELAXED";
                case TimingMode.Demo: return "DEMO";
                default: return "STANDARD";
            }
        }
    }
}

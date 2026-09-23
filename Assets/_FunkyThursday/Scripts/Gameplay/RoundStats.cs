using System;

namespace FunkyThursday.Gameplay
{
    /// <summary>The numbers a finished round reports. Phase 5 adds accuracy and rank.</summary>
    [Serializable]
    public struct RoundStats
    {
        public bool cleared;
        public int perfects;
        public int goods;
        public int misses;
        public int maxCombo;

        public int Hits => perfects + goods;
        public int Judged => perfects + goods + misses;

        /// <summary>True if this result should replace <paramref name="other"/> as the saved best.</summary>
        public bool Beats(RoundStats other)
        {
            if (cleared != other.cleared) return cleared;
            if (misses != other.misses) return misses < other.misses;
            if (perfects != other.perfects) return perfects > other.perfects;
            return maxCombo > other.maxCombo;
        }
    }
}

using UnityEngine;

namespace FunkyThursday.Core
{
    /// <summary>The four playable lanes, in strumline order (left to right).</summary>
    public enum NoteLane
    {
        Left = 0,
        Down = 1,
        Up = 2,
        Right = 3
    }

    /// <summary>Which strumline a chart note belongs to. Values match the chart JSON "side" field.</summary>
    public enum StrumSide
    {
        Opponent = 0,
        Player = 1
    }

    /// <summary>Result of a single player hit.</summary>
    public enum Judgement
    {
        Perfect,
        Good,
        Miss
    }

    /// <summary>Lane helpers shared by gameplay, AI and visuals.</summary>
    public static class Lanes
    {
        public const int Count = 4;

        /// <summary>Unit vector pointing the way the lane's arrow points.</summary>
        public static Vector2 Direction(NoteLane lane)
        {
            switch (lane)
            {
                case NoteLane.Left: return Vector2.left;
                case NoteLane.Down: return Vector2.down;
                case NoteLane.Up: return Vector2.up;
                case NoteLane.Right: return Vector2.right;
                default: return Vector2.zero;
            }
        }
    }
}

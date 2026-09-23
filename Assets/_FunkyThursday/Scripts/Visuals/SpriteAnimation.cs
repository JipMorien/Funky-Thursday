using UnityEngine;

namespace FunkyThursday.Visuals
{
    /// <summary>
    /// An ordered list of sprites plus a frame rate. Created automatically by SpriteSheetPostprocessor
    /// for every sheet that Tools/higgs_import.py writes; can also be made by hand via the Create menu.
    /// </summary>
    [CreateAssetMenu(fileName = "New Sprite Animation", menuName = "Funky Thursday/Sprite Animation")]
    public sealed class SpriteAnimation : ScriptableObject
    {
        [SerializeField] Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.1f)] float fps = 12f;
        [SerializeField] bool loop = true;

        public int FrameCount => frames != null ? frames.Length : 0;
        public float Fps => fps;
        public bool Loop => loop;
        public float Duration => FrameCount / fps;

        public static bool IsUsable(SpriteAnimation animation) => animation != null && animation.FrameCount > 0;

        /// <summary>Frame at <paramref name="seconds"/> since the start. Non-looping clips hold their last frame.</summary>
        public Sprite Sample(float seconds)
        {
            if (FrameCount == 0) return null;
            int index = Mathf.FloorToInt(Mathf.Max(0f, seconds) * fps);
            index = loop ? index % FrameCount : Mathf.Min(index, FrameCount - 1);
            return frames[index];
        }

        /// <summary>Frame at a 0-1 position through the clip (wraps). Used to lock loops to the beat.</summary>
        public Sprite SampleNormalized(float t)
        {
            if (FrameCount == 0) return null;
            int index = Mathf.FloorToInt(Mathf.Repeat(t, 1f) * FrameCount);
            return frames[Mathf.Clamp(index, 0, FrameCount - 1)];
        }

        public void SetFrames(Sprite[] newFrames, float newFps, bool newLoop)
        {
            frames = newFrames ?? new Sprite[0];
            fps = Mathf.Max(0.1f, newFps);
            loop = newLoop;
        }
    }
}

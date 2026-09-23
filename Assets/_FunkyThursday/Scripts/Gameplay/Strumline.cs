using System.Collections.Generic;
using FunkyThursday.Core;
using FunkyThursday.Placeholders;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Four receptors centred on this transform, plus the per-lane queues of notes that can still
    /// be judged on this side. Receptor visuals react to Held / Confirm / Holding state.
    /// </summary>
    public sealed class Strumline : MonoBehaviour
    {
        public const int ReceptorSortingOrder = 20;
        public const int NoteSortingOrder = 30;

        const float ConfirmSeconds = 0.15f;

        [SerializeField] StrumSide side = StrumSide.Player;
        [SerializeField, Min(0.5f)] float laneSpacing = 1.45f;
        [SerializeField, Min(0.1f)] float arrowScale = 1.3f;

        readonly SpriteRenderer[] _receptors = new SpriteRenderer[Lanes.Count];
        readonly float[] _confirmTimers = new float[Lanes.Count];
        readonly bool[] _held = new bool[Lanes.Count];
        readonly bool[] _holding = new bool[Lanes.Count];
        readonly List<Note>[] _lanes = new List<Note>[Lanes.Count];

        public StrumSide Side => side;
        public float ArrowScale => arrowScale;
        public float ReceptorY => transform.position.y;

        void Awake()
        {
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                _lanes[lane] = new List<Note>();

                var receptor = new GameObject($"Receptor {(NoteLane)lane}").AddComponent<SpriteRenderer>();
                receptor.transform.SetParent(transform, false);
                receptor.transform.localPosition = new Vector3(LocalLaneX(lane), 0f, 0f);
                receptor.transform.localScale = Vector3.one * arrowScale;
                receptor.sortingOrder = ReceptorSortingOrder;
                receptor.sprite = PixelArtFactory.GetArrow((NoteLane)lane, ArrowStyle.ReceptorIdle);
                _receptors[lane] = receptor;
            }
        }

        public float LaneX(NoteLane lane) => transform.position.x + LocalLaneX((int)lane);

        /// <summary>Judgeable notes in a lane, earliest first.</summary>
        public IReadOnlyList<Note> NotesIn(NoteLane lane) => _lanes[(int)lane];

        public void Add(Note note) => _lanes[(int)note.Data.Lane].Add(note);

        public bool Remove(Note note) => _lanes[(int)note.Data.Lane].Remove(note);

        public void Clear()
        {
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                _lanes[lane].Clear();
                _confirmTimers[lane] = 0f;
                _held[lane] = false;
                _holding[lane] = false;
            }
        }

        /// <summary>Key is physically down (player side only).</summary>
        public void SetHeld(NoteLane lane, bool held) => _held[(int)lane] = held;

        /// <summary>Flash the lit receptor, e.g. on a hit.</summary>
        public void Confirm(NoteLane lane) => _confirmTimers[(int)lane] = ConfirmSeconds;

        /// <summary>Keep the receptor lit for the duration of a sustain.</summary>
        public void SetHolding(NoteLane lane, bool holding) => _holding[(int)lane] = holding;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                float timer = _confirmTimers[lane] = Mathf.Max(0f, _confirmTimers[lane] - dt);
                bool lit = timer > 0f || _holding[lane];

                ArrowStyle style = lit ? ArrowStyle.ReceptorConfirm
                    : _held[lane] ? ArrowStyle.ReceptorPressed
                    : ArrowStyle.ReceptorIdle;

                float pulse = timer > 0f ? 1f + 0.12f * (timer / ConfirmSeconds)
                    : _holding[lane] ? 1.04f
                    : _held[lane] ? 0.92f
                    : 1f;

                SpriteRenderer receptor = _receptors[lane];
                receptor.sprite = PixelArtFactory.GetArrow((NoteLane)lane, style);
                receptor.transform.localScale = Vector3.one * (arrowScale * pulse);
            }
        }

        float LocalLaneX(int lane) => (lane - (Lanes.Count - 1) * 0.5f) * laneSpacing;
    }
}

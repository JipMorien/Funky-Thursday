using FunkyThursday.Charts;
using FunkyThursday.Placeholders;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// One pooled scrolling note: an arrow head plus an optional sustain trail. Owns no timing
    /// logic of its own; HitJudge and AIOpponent change its state, NoteSpawner positions it.
    /// </summary>
    public sealed class Note : MonoBehaviour
    {
        public enum NoteState
        {
            Incoming,   // scrolling toward the receptor, can be hit
            Holding,    // head was hit, sustain is being held
            Released    // missed or dropped: no longer judged, scrolls off screen
        }

        const float SpritePixelsPerUnit = PixelArtFactory.ArrowPixelsPerUnit;
        const float TailHeightPixels = 6f;
        static readonly Color ReleasedTint = new Color(1f, 1f, 1f, 0.35f);

        SpriteRenderer _head;
        SpriteRenderer _body;
        SpriteRenderer _tail;
        float _scale = 1f;
        double _trailStartTime;

        public ChartNote Data { get; private set; }
        public NoteState State { get; private set; }
        public bool IsHold => Data.IsHold;

        /// <summary>World Y of the lowest visible pixel (end of the trail). Used for off-screen culling.</summary>
        public float LowestY { get; private set; }

        /// <summary>Builds the renderer hierarchy for a new pooled note.</summary>
        public static Note Create(Transform parent, int sortingOrder)
        {
            var root = new GameObject("Note");
            root.transform.SetParent(parent, false);
            var note = root.AddComponent<Note>();

            note._head = CreatePart("Head", root.transform, sortingOrder);
            note._body = CreatePart("Hold Body", root.transform, sortingOrder - 1);
            note._tail = CreatePart("Hold Tail", root.transform, sortingOrder - 1);
            root.SetActive(false);
            return note;
        }

        public void Init(ChartNote data, float scale)
        {
            Data = data;
            State = NoteState.Incoming;
            _trailStartTime = data.Time;
            _scale = scale;
            transform.localScale = Vector3.one * scale;

            _head.sprite = PixelArtFactory.GetArrow(data.Lane, ArrowStyle.Note);
            _head.enabled = true;
            _body.sprite = PixelArtFactory.GetHold(data.Lane, tail: false);
            _tail.sprite = PixelArtFactory.GetHold(data.Lane, tail: true);
            _body.enabled = _tail.enabled = data.IsHold;

            SetTint(Color.white);
            gameObject.SetActive(true);
        }

        public void BeginHold()
        {
            State = NoteState.Holding;
            _head.enabled = false;
        }

        /// <summary>
        /// Stops judging this note; it fades and keeps scrolling until culled. A dropped sustain
        /// keeps only the part of its trail that was not yet held.
        /// </summary>
        public void Release(double songTime)
        {
            if (State == NoteState.Holding)
            {
                _trailStartTime = songTime;
                _head.enabled = false;
            }
            State = NoteState.Released;
            SetTint(ReleasedTint);
        }

        /// <summary>Places the note for the given song time. Upscroll: notes rise toward receptorY.</summary>
        public void UpdateVisual(double songTime, float x, float receptorY, float unitsPerSecond)
        {
            double headTime = State == NoteState.Holding ? songTime : _trailStartTime;
            float headY = receptorY - (float)(headTime - songTime) * unitsPerSecond;
            transform.position = new Vector3(x, headY, 0f);

            if (!Data.IsHold)
            {
                LowestY = headY - 0.5f * _scale;
                return;
            }

            float endY = receptorY - (float)(Data.EndTime - songTime) * unitsPerSecond;
            float lengthLocal = Mathf.Max(0f, (headY - endY) / _scale);
            float tailLocal = TailHeightPixels / SpritePixelsPerUnit;
            float bodyLocal = Mathf.Max(0f, lengthLocal - tailLocal);

            _body.enabled = bodyLocal > 0f;
            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = new Vector3(1f, bodyLocal * SpritePixelsPerUnit, 1f);

            _tail.transform.localPosition = new Vector3(0f, -bodyLocal, 0f);
            _tail.enabled = lengthLocal > 0f;

            LowestY = headY - Mathf.Max(lengthLocal, 0.5f) * _scale;
        }

        public void Hide() => gameObject.SetActive(false);

        void SetTint(Color color)
        {
            _head.color = color;
            _body.color = color;
            _tail.color = color;
        }

        static SpriteRenderer CreatePart(string name, Transform parent, int sortingOrder)
        {
            var part = new GameObject(name).AddComponent<SpriteRenderer>();
            part.transform.SetParent(parent, false);
            part.sortingOrder = sortingOrder;
            return part;
        }
    }
}

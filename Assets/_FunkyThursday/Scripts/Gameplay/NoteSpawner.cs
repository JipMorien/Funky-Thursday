using System.Collections.Generic;
using FunkyThursday.Charts;
using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Feeds chart notes into the two strumlines just before they scroll on screen, positions every
    /// live note each frame and recycles them through a pool. Judging is done elsewhere: HitJudge
    /// and AIOpponent call <see cref="Despawn"/> (hit) or <see cref="Release"/> (missed/dropped).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class NoteSpawner : MonoBehaviour
    {
        [SerializeField] Conductor conductor;
        [SerializeField] Strumline opponentStrumline;
        [SerializeField] Strumline playerStrumline;

        [Tooltip("World units per second at chart scroll speed 1.0.")]
        [SerializeField, Min(1f)] float baseScrollSpeed = 9f;

        [Tooltip("Extra world units beyond the screen edge before notes spawn or are culled.")]
        [SerializeField, Min(0f)] float screenMargin = 1.5f;

        readonly List<Note> _live = new List<Note>();
        readonly Stack<Note> _pool = new Stack<Note>();

        Chart _chart;
        Camera _camera;
        int _nextIndex;

        public float UnitsPerSecond { get; private set; }
        public Chart Chart => _chart;

        void Awake()
        {
            _camera = Camera.main;
        }

        public Strumline StrumlineFor(StrumSide side) => side == StrumSide.Player ? playerStrumline : opponentStrumline;

        /// <summary>Clears the field and prepares <paramref name="chart"/> for playback.</summary>
        public void Load(Chart chart)
        {
            Clear();
            _chart = chart;
            _nextIndex = 0;
            UnitsPerSecond = baseScrollSpeed * chart.ScrollSpeed;
        }

        public void Clear()
        {
            for (int i = _live.Count - 1; i >= 0; i--) Recycle(_live[i]);
            _live.Clear();
            opponentStrumline.Clear();
            playerStrumline.Clear();
        }

        /// <summary>Removes a note that was hit or finished holding.</summary>
        public void Despawn(Note note)
        {
            StrumlineFor(note.Data.Side).Remove(note);
            _live.Remove(note);
            Recycle(note);
        }

        /// <summary>Stops judging a missed or dropped note; it keeps scrolling until off screen.</summary>
        public void Release(Note note, double songTime)
        {
            StrumlineFor(note.Data.Side).Remove(note);
            note.Release(songTime);
        }

        void Update()
        {
            if (_chart == null || conductor.State != Conductor.PlaybackState.Playing) return;

            double time = conductor.SongPosition;
            double lead = SpawnLeadSeconds();
            IReadOnlyList<ChartNote> notes = _chart.Notes;

            while (_nextIndex < notes.Count && notes[_nextIndex].Time - time <= lead)
            {
                ChartNote data = notes[_nextIndex++];
                Strumline strumline = StrumlineFor(data.Side);
                Note note = Rent();
                note.Init(data, strumline.ArrowScale);
                note.UpdateVisual(time, strumline.LaneX(data.Lane), strumline.ReceptorY, UnitsPerSecond);
                strumline.Add(note);
                _live.Add(note);
            }
        }

        void LateUpdate()
        {
            if (_chart == null || conductor.State == Conductor.PlaybackState.Stopped) return;

            double time = conductor.SongPosition;
            float top = ScreenTop() + screenMargin;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Note note = _live[i];
                Strumline strumline = StrumlineFor(note.Data.Side);
                note.UpdateVisual(time, strumline.LaneX(note.Data.Lane), strumline.ReceptorY, UnitsPerSecond);

                if (note.State == Note.NoteState.Released && note.LowestY > top)
                {
                    _live.RemoveAt(i);
                    Recycle(note);
                }
            }
        }

        double SpawnLeadSeconds()
        {
            float receptorY = Mathf.Max(opponentStrumline.ReceptorY, playerStrumline.ReceptorY);
            float bottom = ScreenBottom() - screenMargin;
            return (receptorY - bottom) / UnitsPerSecond;
        }

        float ScreenTop() => _camera != null ? _camera.transform.position.y + _camera.orthographicSize : 5f;
        float ScreenBottom() => _camera != null ? _camera.transform.position.y - _camera.orthographicSize : -5f;

        Note Rent() => _pool.Count > 0 ? _pool.Pop() : Note.Create(transform, Strumline.NoteSortingOrder);

        void Recycle(Note note)
        {
            note.Hide();
            _pool.Push(note);
        }
    }
}

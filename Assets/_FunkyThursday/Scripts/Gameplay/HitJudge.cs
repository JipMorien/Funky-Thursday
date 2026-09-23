using System;
using System.Collections.Generic;
using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Gameplay
{
    /// <summary>One judged player action.</summary>
    public readonly struct HitResult
    {
        public readonly Judgement Judgement;
        public readonly NoteLane Lane;

        /// <summary>Press time minus note time. Negative = early. 0 for timed-out misses and drops.</summary>
        public readonly double OffsetMs;

        /// <summary>Sustain length of the note that was hit, so a character can hold its pose.</summary>
        public readonly double HoldSeconds;

        public readonly HitKind Kind;

        public HitResult(Judgement judgement, NoteLane lane, double offsetMs, double holdSeconds, HitKind kind)
        {
            Judgement = judgement;
            Lane = lane;
            OffsetMs = offsetMs;
            HoldSeconds = holdSeconds;
            Kind = kind;
        }
    }

    public enum HitKind
    {
        Tap,          // pressed a note (any judgement)
        TimedOut,     // note scrolled past without a press
        HoldDropped,  // released a sustain too early
        GhostTap      // pressed with nothing to hit (only raised when ghost tapping is off)
    }

    /// <summary>
    /// Judges the player's strumline: Perfect / Good / Miss hit windows, timed-out notes, sustains
    /// and optional ghost-tap penalties. Raises events; it does not touch health or score itself.
    /// </summary>
    public sealed class HitJudge : MonoBehaviour
    {
        [SerializeField] Conductor conductor;
        [SerializeField] NoteSpawner spawner;
        [SerializeField] Strumline strumline;
        [SerializeField] PlayerInput input;

        [Header("Hit Windows (ms, either side of the note)")]
        [SerializeField, Min(1f)] float perfectWindowMs = 45f;
        [SerializeField, Min(1f)] float goodWindowMs = 90f;
        [SerializeField, Min(1f)] float missWindowMs = 135f;

        [Header("Sustains")]
        [Tooltip("Releasing this close to the end of a sustain still counts as completing it.")]
        [SerializeField, Min(0f)] float holdReleaseGraceMs = 100f;

        [Header("Rules")]
        [Tooltip("FNF default: pressing with no note nearby is free. Turn off to punish mashing.")]
        [SerializeField] bool ghostTapping = true;

        /// <summary>Every judged action, in order.</summary>
        public event Action<HitResult> Judged;

        /// <summary>Seconds of sustain held this frame (sum over lanes). Drives hold health.</summary>
        public event Action<float> SustainHeld;

        public bool GhostTapping
        {
            get => ghostTapping;
            set => ghostTapping = value;
        }

        public float PerfectWindowMs => perfectWindowMs;
        public float GoodWindowMs => goodWindowMs;
        public float MissWindowMs => missWindowMs;

        void OnValidate()
        {
            goodWindowMs = Mathf.Max(goodWindowMs, perfectWindowMs);
            missWindowMs = Mathf.Max(missWindowMs, goodWindowMs);
        }

        void OnDisable()
        {
            if (strumline == null) return;
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                strumline.SetHeld((NoteLane)lane, false);
                strumline.SetHolding((NoteLane)lane, false);
            }
        }

        void Update()
        {
            if (!conductor.IsPlaying) return;

            double time = conductor.SongPosition;
            float heldSeconds = 0f;

            for (int i = 0; i < Lanes.Count; i++)
            {
                var lane = (NoteLane)i;
                strumline.SetHeld(lane, input.IsHeld(lane));

                ExpireTimedOutNotes(lane, time);
                heldSeconds += UpdateSustain(lane, time);

                if (input.WasPressed(lane)) TryHit(lane, time);
            }

            if (heldSeconds > 0f) SustainHeld?.Invoke(heldSeconds);
        }

        void ExpireTimedOutNotes(NoteLane lane, double time)
        {
            IReadOnlyList<Note> notes = strumline.NotesIn(lane);
            while (notes.Count > 0)
            {
                Note first = notes[0];
                if (first.State != Note.NoteState.Incoming) break;
                if ((time - first.Data.Time) * 1000.0 <= missWindowMs) break;

                spawner.Release(first, time);
                Raise(new HitResult(Judgement.Miss, lane, 0.0, first.Data.HoldSeconds, HitKind.TimedOut));
            }
        }

        /// <summary>Returns seconds of sustain held this frame in this lane.</summary>
        float UpdateSustain(NoteLane lane, double time)
        {
            IReadOnlyList<Note> notes = strumline.NotesIn(lane);
            if (notes.Count == 0 || notes[0].State != Note.NoteState.Holding)
            {
                strumline.SetHolding(lane, false);
                return 0f;
            }

            Note note = notes[0];
            double end = note.Data.EndTime;

            if (time >= end)
            {
                strumline.SetHolding(lane, false);
                spawner.Despawn(note);
                return 0f;
            }

            if (!input.IsHeld(lane))
            {
                strumline.SetHolding(lane, false);
                if ((end - time) * 1000.0 <= holdReleaseGraceMs)
                {
                    spawner.Despawn(note);
                }
                else
                {
                    spawner.Release(note, time);
                    Raise(new HitResult(Judgement.Miss, lane, 0.0, 0.0, HitKind.HoldDropped));
                }
                return 0f;
            }

            strumline.SetHolding(lane, true);
            return Time.deltaTime;
        }

        void TryHit(NoteLane lane, double time)
        {
            Note target = null;
            foreach (Note note in strumline.NotesIn(lane))
            {
                if (note.State == Note.NoteState.Incoming)
                {
                    target = note;
                    break;
                }
            }

            double offsetMs = target != null ? (time - target.Data.Time) * 1000.0 : double.PositiveInfinity;
            double distance = Math.Abs(offsetMs);

            if (distance > missWindowMs)
            {
                if (!ghostTapping) Raise(new HitResult(Judgement.Miss, lane, 0.0, 0.0, HitKind.GhostTap));
                return;
            }

            Judgement judgement = distance <= perfectWindowMs ? Judgement.Perfect
                : distance <= goodWindowMs ? Judgement.Good
                : Judgement.Miss;

            if (judgement == Judgement.Miss)
            {
                spawner.Release(target, time);
            }
            else
            {
                strumline.Confirm(lane);
                if (target.IsHold)
                {
                    target.BeginHold();
                    strumline.SetHolding(lane, true);
                }
                else
                {
                    spawner.Despawn(target);
                }
            }

            Raise(new HitResult(judgement, lane, offsetMs, target.Data.HoldSeconds, HitKind.Tap));
        }

        void Raise(HitResult result) => Judged?.Invoke(result);
    }
}

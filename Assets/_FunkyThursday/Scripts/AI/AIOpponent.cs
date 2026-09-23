using System;
using System.Collections.Generic;
using FunkyThursday.Charts;
using FunkyThursday.Core;
using FunkyThursday.Gameplay;
using UnityEngine;

namespace FunkyThursday.AI
{
    /// <summary>
    /// Auto-plays one strumline with perfect timing: taps each note as it reaches the receptor and
    /// holds sustains to the end. Normally drives the opponent; point it at the player strumline
    /// (with HitJudge disabled) for botplay when testing charts.
    ///
    /// Harder levels can set a per-note health drain in the chart's "ai" block. Drain only happens
    /// while the player is above the chart's drain floor, so the opponent can pressure but never
    /// kill on its own.
    /// </summary>
    public sealed class AIOpponent : MonoBehaviour
    {
        [SerializeField] Conductor conductor;
        [SerializeField] NoteSpawner spawner;
        [SerializeField] Strumline strumline;

        [Header("Health Drain (overridden by the chart)")]
        [SerializeField, Min(0f)] float healthDrainPerNote;
        [SerializeField, Range(0f, 1f)] float drainFloor = 0.1f;

        /// <summary>Lane sung and the sustain length (0 for taps). Drives the character's pose.</summary>
        public event Action<NoteLane, double> Sang;

        /// <summary>Amount of player health to remove and the floor it must not cross.</summary>
        public event Action<float, float> DrainRequested;

        public Strumline Strumline => strumline;

        public void Configure(ChartAiSettings settings)
        {
            if (settings == null) return;
            healthDrainPerNote = Mathf.Max(0f, settings.healthDrainPerNote);
            drainFloor = Mathf.Clamp01(settings.drainFloor);
        }

        void OnDisable()
        {
            if (strumline == null) return;
            for (int lane = 0; lane < Lanes.Count; lane++) strumline.SetHolding((NoteLane)lane, false);
        }

        void Update()
        {
            if (!conductor.IsPlaying) return;

            double time = conductor.SongPosition;
            for (int i = 0; i < Lanes.Count; i++)
            {
                var lane = (NoteLane)i;
                IReadOnlyList<Note> notes = strumline.NotesIn(lane);
                bool holding = false;

                while (notes.Count > 0)
                {
                    Note note = notes[0];

                    if (note.State == Note.NoteState.Holding)
                    {
                        if (time < note.Data.EndTime)
                        {
                            holding = true;
                            break;
                        }
                        spawner.Despawn(note);
                        continue;
                    }

                    if (note.Data.Time > time) break;

                    strumline.Confirm(lane);
                    Sang?.Invoke(lane, note.Data.HoldSeconds);
                    if (healthDrainPerNote > 0f) DrainRequested?.Invoke(healthDrainPerNote, drainFloor);

                    if (note.IsHold && time < note.Data.EndTime)
                    {
                        note.BeginHold();
                        holding = true;
                        break;
                    }
                    spawner.Despawn(note);
                }

                strumline.SetHolding(lane, holding);
            }
        }
    }
}

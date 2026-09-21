using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using FunkyThursday.Core;

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Reads arrow keys and WASD (FNF-style: A/left, S/down, W/up, D/right)
    /// and matches each press against the nearest active note in that lane,
    /// within the note's own hit window.
    /// </summary>
    public class NoteInputHandler : MonoBehaviour
    {
        /// <summary>Fired for every successful hit (never for a ghost tap/miss); lets receptor/character visuals react without this class knowing about them.</summary>
        public static event Action<int, HitJudgment> OnLaneHit;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            CheckLane(0, Keyboard.current.leftArrowKey, Keyboard.current.aKey);
            CheckLane(1, Keyboard.current.downArrowKey, Keyboard.current.sKey);
            CheckLane(2, Keyboard.current.upArrowKey, Keyboard.current.wKey);
            CheckLane(3, Keyboard.current.rightArrowKey, Keyboard.current.dKey);
        }

        private void CheckLane(int lane, KeyControl arrowKey, KeyControl altKey)
        {
            if (arrowKey.wasPressedThisFrame || altKey.wasPressedThisFrame)
            {
                TryHitLane(lane);
            }
        }

        private void TryHitLane(int lane)
        {
            Conductor conductor = Conductor.Instance;
            if (conductor == null || !conductor.SongHasStarted)
            {
                return;
            }

            float songPosition = conductor.SongPositionInSeconds;

            Note closestNote = null;
            float closestDelta = float.MaxValue;

            foreach (Note note in Note.ActiveNotes)
            {
                if (note.Data.lane != lane || !note.Data.isPlayerNote)
                {
                    continue;
                }

                float delta = Mathf.Abs(note.Data.time - songPosition);
                if (delta < closestDelta)
                {
                    closestDelta = delta;
                    closestNote = note;
                }
            }

            if (closestNote != null && closestDelta <= closestNote.hitWindow)
            {
                HitJudgment judgment = closestNote.GetJudgment(closestDelta);
                ScoreManager.Instance?.RegisterHit(judgment);
                HealthManager.Instance?.ApplyJudgment(judgment);
                CharacterBopController.PlayerInstance?.PlaySing(lane);
                OnLaneHit?.Invoke(lane, judgment);
                closestNote.Hit();
                UnityEngine.Debug.Log($"[Input] {judgment} hit lane {lane} (offset {closestDelta * 1000f:F0} ms)");
            }
            else
            {
                UnityEngine.Debug.Log($"[Input] Miss press on lane {lane} - no note in range");
            }
        }
    }
}

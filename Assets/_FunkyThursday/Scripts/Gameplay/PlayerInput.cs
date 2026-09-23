#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define FT_NEW_INPUT
#endif

using FunkyThursday.Core;
using UnityEngine;
#if FT_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace FunkyThursday.Gameplay
{
    /// <summary>
    /// Samples the lane keys once per frame so every consumer sees the same state. Works with the
    /// legacy Input Manager, the Input System package, or both. Arrows and WASD, FNF defaults.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class PlayerInput : MonoBehaviour
    {
#if FT_NEW_INPUT
        static readonly Key[] PrimaryKeys = { Key.LeftArrow, Key.DownArrow, Key.UpArrow, Key.RightArrow };
        static readonly Key[] AltKeys = { Key.A, Key.S, Key.W, Key.D };
#else
        static readonly KeyCode[] PrimaryKeys = { KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.UpArrow, KeyCode.RightArrow };
        static readonly KeyCode[] AltKeys = { KeyCode.A, KeyCode.S, KeyCode.W, KeyCode.D };
#endif

        readonly bool[] _pressed = new bool[Lanes.Count];
        readonly bool[] _held = new bool[Lanes.Count];
        readonly bool[] _released = new bool[Lanes.Count];

        public bool PausePressed { get; private set; }
        public bool RestartPressed { get; private set; }

        /// <summary>Number keys 1-4, used by the Phase 2 test harness to switch levels.</summary>
        public int LevelKeyPressed { get; private set; } = -1;

        public bool WasPressed(NoteLane lane) => _pressed[(int)lane];
        public bool IsHeld(NoteLane lane) => _held[(int)lane];
        public bool WasReleased(NoteLane lane) => _released[(int)lane];

        void Update()
        {
#if FT_NEW_INPUT
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ClearAll();
                return;
            }

            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                var primary = keyboard[PrimaryKeys[lane]];
                var alt = keyboard[AltKeys[lane]];
                _pressed[lane] = primary.wasPressedThisFrame || alt.wasPressedThisFrame;
                _held[lane] = primary.isPressed || alt.isPressed;
                _released[lane] = (primary.wasReleasedThisFrame || alt.wasReleasedThisFrame) && !_held[lane];
            }

            PausePressed = keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            RestartPressed = keyboard.rKey.wasPressedThisFrame;
            LevelKeyPressed = keyboard.digit1Key.wasPressedThisFrame ? 0
                : keyboard.digit2Key.wasPressedThisFrame ? 1
                : keyboard.digit3Key.wasPressedThisFrame ? 2
                : keyboard.digit4Key.wasPressedThisFrame ? 3
                : -1;
#else
            for (int lane = 0; lane < Lanes.Count; lane++)
            {
                _pressed[lane] = Input.GetKeyDown(PrimaryKeys[lane]) || Input.GetKeyDown(AltKeys[lane]);
                _held[lane] = Input.GetKey(PrimaryKeys[lane]) || Input.GetKey(AltKeys[lane]);
                _released[lane] = (Input.GetKeyUp(PrimaryKeys[lane]) || Input.GetKeyUp(AltKeys[lane])) && !_held[lane];
            }

            PausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return);
            RestartPressed = Input.GetKeyDown(KeyCode.R);
            LevelKeyPressed = Input.GetKeyDown(KeyCode.Alpha1) ? 0
                : Input.GetKeyDown(KeyCode.Alpha2) ? 1
                : Input.GetKeyDown(KeyCode.Alpha3) ? 2
                : Input.GetKeyDown(KeyCode.Alpha4) ? 3
                : -1;
#endif
        }

#if FT_NEW_INPUT
        void ClearAll()
        {
            for (int lane = 0; lane < Lanes.Count; lane++) _pressed[lane] = _held[lane] = _released[lane] = false;
            PausePressed = RestartPressed = false;
            LevelKeyPressed = -1;
        }
#endif
    }
}

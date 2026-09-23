#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#define FT_NEW_INPUT
#endif

using UnityEngine;
#if FT_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace FunkyThursday.UI
{
    /// <summary>Menu navigation keys for both input backends: arrows/WASD, Enter/Space, Esc/Backspace.</summary>
    public static class MenuInput
    {
        public static bool UpPressed => Down(Nav.Up);
        public static bool DownPressed => Down(Nav.Down);
        public static bool LeftPressed => Down(Nav.Left);
        public static bool RightPressed => Down(Nav.Right);
        public static bool ConfirmPressed => Down(Nav.Confirm);
        public static bool BackPressed => Down(Nav.Back);

        public static bool UpHeld => Held(Nav.Up);
        public static bool DownHeld => Held(Nav.Down);
        public static bool LeftHeld => Held(Nav.Left);
        public static bool RightHeld => Held(Nav.Right);

        enum Nav { Up, Down, Left, Right, Confirm, Back }

#if FT_NEW_INPUT
        static bool Down(Nav key)
        {
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case Nav.Up: return k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
                case Nav.Down: return k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame;
                case Nav.Left: return k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
                case Nav.Right: return k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
                case Nav.Confirm: return k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
                default: return k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame;
            }
        }

        static bool Held(Nav key)
        {
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case Nav.Up: return k.upArrowKey.isPressed || k.wKey.isPressed;
                case Nav.Down: return k.downArrowKey.isPressed || k.sKey.isPressed;
                case Nav.Left: return k.leftArrowKey.isPressed || k.aKey.isPressed;
                case Nav.Right: return k.rightArrowKey.isPressed || k.dKey.isPressed;
                default: return false;
            }
        }
#else
        static bool Down(Nav key)
        {
            switch (key)
            {
                case Nav.Up: return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
                case Nav.Down: return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
                case Nav.Left: return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
                case Nav.Right: return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
                case Nav.Confirm: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
                default: return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
            }
        }

        static bool Held(Nav key)
        {
            switch (key)
            {
                case Nav.Up: return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
                case Nav.Down: return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
                case Nav.Left: return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
                case Nav.Right: return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
                default: return false;
            }
        }
#endif
    }
}

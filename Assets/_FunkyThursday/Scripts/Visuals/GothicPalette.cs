using FunkyThursday.Core;
using UnityEngine;

namespace FunkyThursday.Visuals
{
    /// <summary>
    /// The shared retro palette. Placeholders, UI and the Higgsfield prompts all reference these
    /// values so code-drawn and generated art live in the same colour world.
    /// </summary>
    public static class GothicPalette
    {
        public static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        // Neutrals
        public static readonly Color32 Void = Hex(0x0B0710);
        public static readonly Color32 Ink = Hex(0x120A14);
        public static readonly Color32 Stone = Hex(0x2A2230);
        public static readonly Color32 StoneLight = Hex(0x3C3244);
        public static readonly Color32 Ash = Hex(0x7A6C84);
        public static readonly Color32 Bone = Hex(0xD9D2C0);
        public static readonly Color32 BoneShadow = Hex(0xA89F8C);

        // Lane colours
        public static readonly Color32 Amethyst = Hex(0x8E4BC4);
        public static readonly Color32 Moonlit = Hex(0x3E7FC1);
        public static readonly Color32 GraveMoss = Hex(0x4E9A5A);
        public static readonly Color32 Blood = Hex(0xB3263A);

        // Scenery
        public static readonly Color32 FarHills = Hex(0x241530);
        public static readonly Color32[] Sky =
        {
            Hex(0x0B0710),
            Hex(0x150C1E),
            Hex(0x21112B),
            Hex(0x2E1733),
            Hex(0x3D1F3A)
        };

        // Characters
        public static readonly Color32 OpponentCloak = Hex(0x5A1424);
        public static readonly Color32 OpponentEyes = Hex(0xFF4A3D);
        public static readonly Color32 PlayerCloak = Hex(0x2B2F5E);
        public static readonly Color32 PlayerEyes = Hex(0x7FE7FF);

        public static Color32 Lane(NoteLane lane)
        {
            switch (lane)
            {
                case NoteLane.Left: return Amethyst;
                case NoteLane.Down: return Moonlit;
                case NoteLane.Up: return GraveMoss;
                case NoteLane.Right: return Blood;
                default: return Bone;
            }
        }

        public static Color32 Lighten(Color32 color, float amount) =>
            Color32.Lerp(color, new Color32(255, 255, 255, color.a), amount);

        public static Color32 Darken(Color32 color, float amount) =>
            Color32.Lerp(color, new Color32(0, 0, 0, color.a), amount);

        static Color32 Hex(uint rgb) =>
            new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}

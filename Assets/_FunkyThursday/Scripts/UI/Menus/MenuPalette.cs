using UnityEngine;

namespace FunkyThursday.UI.Menus
{
    /// <summary>Text colours for the menus, matching GothicPalette.</summary>
    public static class MenuPalette
    {
        public static readonly Color Bone = new Color32(0xDD, 0xD6, 0xC6, 0xFF);
        public static readonly Color Ash = new Color32(0x9C, 0x8F, 0xA6, 0xFF);
        public static readonly Color Faint = new Color32(0x6F, 0x62, 0x78, 0xFF);
        public static readonly Color Blood = new Color32(0xD0, 0x36, 0x4F, 0xFF);
        public static readonly Color Moss = new Color32(0x67, 0xB3, 0x74, 0xFF);
        public static readonly Color Moonlit = new Color32(0x5B, 0x98, 0xD6, 0xFF);
        public static readonly Color Amethyst = new Color32(0xA6, 0x6A, 0xD6, 0xFF);
        public static readonly Color Dim = new Color(0.043f, 0.027f, 0.063f, 0.72f);

        public static Color ForDifficulty(string difficulty)
        {
            switch ((difficulty ?? "").ToLowerInvariant())
            {
                case "easy": return Moss;
                case "medium": return Moonlit;
                case "hard": return Amethyst;
                case "expert": return Blood;
                default: return Ash;
            }
        }
    }
}

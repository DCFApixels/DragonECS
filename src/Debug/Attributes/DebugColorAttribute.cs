using System;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DebugColorAttribute : Attribute
    {
        private ColorRecord color;
        public byte r => color.r;
        public byte g => color.g;
        public byte b => color.b;
        /// <summary>normalized R channel </summary>
        public float rn => color.r / 255f;
        /// <summary>normalized G channel </summary>
        public float gn => color.g / 255f;
        /// <summary>normalized B channel </summary>
        public float bn => color.b / 255f;
        public DebugColorAttribute(byte r, byte g, byte b) => color = new ColorRecord(r, g, b);
        public DebugColorAttribute(DebugColor color) => this.color = new ColorRecord((int)color);

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
        private readonly struct ColorRecord // Union
        {
            [FieldOffset(0)] public readonly int full;
            [FieldOffset(3)] public readonly byte r;
            [FieldOffset(2)] public readonly byte g;
            [FieldOffset(1)] public readonly byte b;
            public ColorRecord(byte r, byte g, byte b) : this()
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
            public ColorRecord(int full) : this() => this.full = full;
        }
    }
    public enum DebugColor
    {
        /// <summary> Red. RGB is (255, 0, 0)</summary>
        Red = (255 << 24) + (000 << 16) + (000 << 8),
        /// <summary> Green. RGB is (0, 255, 0)</summary>
        Green = (000 << 24) + (255 << 16) + (000 << 8),
        /// <summary> Blue. RGB is (0, 0, 255)</summary>
        Blue = (000 << 24) + (000 << 16) + (255 << 8),

        /// <summary> Yellow. RGB is (255, 255, 0)</summary>
        Yellow = (255 << 24) + (255 << 16) + (000 << 8),
        /// <summary> Cyan. RGB is (0, 255, 255)</summary>
        Cyan = (000 << 24) + (255 << 16) + (255 << 8),
        /// <summary> Magenta. RGB is (255, 0, 255)</summary>
        Magenta = (255 << 24) + (000 << 16) + (255 << 8),

        /// <summary> Yellow. RGB is (255, 165, 0)</summary>
        Orange = (255 << 24) + (165 << 16) + (000 << 8),
        /// <summary> Yellow. RGB is (255, 69, 0)</summary>
        OrangeRed = (255 << 24) + (69 << 16) + (000 << 8),
        /// <summary> Lime. RGB is (125, 255, 0)</summary>
        Lime = (125 << 24) + (255 << 16) + (000 << 8),
        /// <summary> Lime. RGB is (127, 255, 212)</summary>
        Aquamarine = (127 << 24) + (255 << 16) + (212 << 8),
        /// <summary> Lime. RGB is (218, 165, 32)</summary>
        Goldenrod = (218 << 24) + (165 << 16) + (32 << 8),
        /// <summary> Yellow. RGB is (255, 105, 180)</summary>
        DeepPink = (255 << 24) + (105 << 16) + (180 << 8),
        /// <summary> Yellow. RGB is (220, 20, 60)</summary>
        Crimson = (220 << 24) + (20 << 16) + (60 << 8),
        /// <summary> Yellow. RGB is (138, 43, 226)</summary>
        BlueViolet = (138 << 24) + (43 << 16) + (226 << 8),
        /// <summary> Yellow. RGB is (255, 3, 62)</summary>
        AmericanRose = (255 << 24) + (3 << 16) + (62 << 8),

        /// <summary> Grey/Gray. RGB is (127, 127, 127)</summary>
        Gray = (127 << 24) + (127 << 16) + (127 << 8),
        /// <summary> Grey/Gray. RGB is (127, 127, 127)</summary>
        Grey = Gray,
        /// <summary> Grey/Gray. RGB is (192, 192, 192)</summary>
        Silver = (192 << 24) + (192 << 16) + (192 << 8),
        /// <summary> White. RGB is (255, 255, 255)</summary>
        White = -1,
        /// <summary> Black. RGB is (0, 0, 0)</summary>
        Black = 0,
    }
}
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

        public DebugColorAttribute(byte r, byte g, byte b)
        {
            color = new ColorRecord(r, g, b);
        }

        public DebugColorAttribute(DebugColor color)
        {
            this.color = new ColorRecord((int)color);
        }


        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
        private readonly struct ColorRecord
        {
            [FieldOffset(0)]
            public readonly int full;
            [FieldOffset(3)]
            public readonly byte r;
            [FieldOffset(2)]
            public readonly byte g;
            [FieldOffset(1)]
            public readonly byte b;

            public ColorRecord(byte r, byte g, byte b) : this()
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
            public ColorRecord(int full) : this()
            {
                this.full = full;
            }
        }
    }

    public enum DebugColor
    {
        /// <summary> Red. RGB is (255, 0, 0)</summary>
        Red = 255 << 8 * 3 + 000 << 8 * 2 + 000 << 8,
        /// <summary> Green. RGB is (0, 255, 0)</summary>
        Green = 000 << 8 * 3 + 255 << 8 * 2 + 000 << 8,
        /// <summary> Blue. RGB is (0, 0, 255)</summary>
        Blue = 000 << 8 * 3 + 000 << 8 * 2 + 255 << 8,

        /// <summary> Yellow. RGB is (255, 255, 0)</summary>
        Yellow = 255 << 8 * 3 + 255 << 8 * 2 + 000 << 8,
        /// <summary> Cyan. RGB is (0, 255, 255)</summary>
        Cyan = 000 << 8 * 3 + 255 << 8 * 2 + 255 << 8,
        /// <summary> Magenta. RGB is (255, 0, 255)</summary>
        Magenta = 255 << 8 * 3 + 000 << 8 * 2 + 000 << 8,

        /// <summary> Yellow. RGB is (255, 127, 0)</summary>
        Orange = (255 << 24) + (127 << 16) + (000 << 8),

        /// <summary> Grey/Gray. RGB is (127, 127, 127)</summary>
        Gray = 127 << 8 * 3 + 127 << 8 * 2 + 127 << 8,
        /// <summary> Grey/Gray. RGB is (127, 127, 127)</summary>
        Grey = Gray,
        /// <summary> White. RGB is (255, 255, 255)</summary>
        White = -1,
        /// <summary> Black. RGB is (0, 0, 0)</summary>
        Black = 0,
    }
}
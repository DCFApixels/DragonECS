using System;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DebugColorAttribute : Attribute
    {
        public readonly DebugColor color;
        public byte r => color.r;
        public byte g => color.g;
        public byte b => color.b;
        public DebugColorAttribute(byte r, byte g, byte b) => color = new DebugColor(r, g, b, 255);
        public DebugColorAttribute(int colorCode) => color = new DebugColor(colorCode, true);
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    public readonly struct DebugColor
    {
        public static readonly DebugColor BlackColor = new DebugColor(Black);
        /// <summary> color code Red. RGB is (255, 0, 0)</summary>
        public const int Red = (255 << 24) | (000 << 16) | (000 << 8) | 255;
        /// <summary> color code Green. RGB is (0, 255, 0)</summary>
        public const int Green = (000 << 24) | (255 << 16) | (000 << 8) | 255;
        /// <summary> color code Blue. RGB is (0, 0, 255)</summary>
        public const int Blue = (000 << 24) | (000 << 16) | (255 << 8) | 255;

        /// <summary> color code Yellow. RGB is (255, 255, 0)</summary>
        public const int Yellow = (255 << 24) | (255 << 16) | (000 << 8) | 255;
        /// <summary> color code Cyan. RGB is (0, 255, 255)</summary>
        public const int Cyan = (000 << 24) | (255 << 16) | (255 << 8) | 255;
        /// <summary> color code Magenta. RGB is (255, 0, 255)</summary>
        public const int Magenta = (255 << 24) | (000 << 16) | (255 << 8) | 255;

        /// <summary> color code Orange. RGB is (255, 165, 0)</summary>
        public const int Orange = (255 << 24) | (165 << 16) | (000 << 8) | 255;
        /// <summary> color code OrangeRed. RGB is (255, 69, 0)</summary>
        public const int OrangeRed = (255 << 24) | (69 << 16) | (000 << 8) | 255;
        /// <summary> color code Lime. RGB is (125, 255, 0)</summary>
        public const int Lime = (125 << 24) | (255 << 16) | (000 << 8) | 255;
        /// <summary> color code Aquamarine. RGB is (127, 255, 212)</summary>
        public const int Aquamarine = (127 << 24) | (255 << 16) | (212 << 8) | 255;
        /// <summary> color code Goldenrod. RGB is (218, 165, 32)</summary>
        public const int Goldenrod = (218 << 24) | (165 << 16) | (32 << 8) | 255;
        /// <summary> color code DeepPink. RGB is (255, 105, 180)</summary>
        public const int DeepPink = (255 << 24) | (105 << 16) | (180 << 8) | 255;
        /// <summary> color code Crimson. RGB is (220, 20, 60)</summary>
        public const int Crimson = (220 << 24) | (20 << 16) | (60 << 8) | 255;
        /// <summary> color code BlueViolet. RGB is (138, 43, 226)</summary>
        public const int BlueViolet = (138 << 24) | (43 << 16) | (226 << 8) | 255;
        /// <summary> color code AmericanRose. RGB is (255, 3, 62)</summary>
        public const int AmericanRose = (255 << 24) | (3 << 16) | (62 << 8) | 255;

        /// <summary> color code Grey/Gray. RGB is (127, 127, 127)</summary>
        public const int Gray = (127 << 24) | (127 << 16) | (127 << 8) | 255;
        /// <summary> color code Grey/Gray. RGB is (127, 127, 127)</summary>
        public const int Grey = Gray;
        /// <summary> color code Silver. RGB is (192, 192, 192)</summary>
        public const int Silver = (192 << 24) | (192 << 16) | (192 << 8) | 255;
        /// <summary> color code White. RGB is (255, 255, 255)</summary>
        public const int White = -1;
        /// <summary> color code Black. RGB is (0, 0, 0)</summary>
        public const int Black = 0;


        [FieldOffset(0)] public readonly int colorCode;
        [FieldOffset(3)] public readonly byte r;
        [FieldOffset(2)] public readonly byte g;
        [FieldOffset(1)] public readonly byte b;
        [FieldOffset(0)] public readonly byte a;
        public DebugColor(byte r, byte g, byte b) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
            a = 255;
        }
        public DebugColor(byte r, byte g, byte b, byte a) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public DebugColor(int colorCode) : this() => this.colorCode = colorCode;
        public DebugColor(int colorCode, bool withoutAlpha) : this() => this.colorCode = withoutAlpha ? colorCode | 255 : colorCode;
        public (byte, byte, byte) ToTupleRGB() => (r, g, b);
        public (byte, byte, byte, byte) ToTupleRGBA() => (r, g, b, a);

        public DebugColor UpContrastColor()
        {
            byte minChannel = Math.Min(Math.Min(r, g), b);
            byte maxChannel = Math.Max(Math.Max(r, g), b);
            if (maxChannel == minChannel)
                return default;
            float factor = 255f / (maxChannel - minChannel);
            return new DebugColor((byte)((r - minChannel) * factor), (byte)((g - minChannel) * factor), (byte)((b - minChannel) * factor));
        }
        public static DebugColor operator /(DebugColor a, float b)
        {
            return new DebugColor((byte)(a.r / b), (byte)(a.g / b), (byte)(a.b / b));
        }
        //public static explicit operator DebugColor(int colorCode) => new DebugColor(colorCode);
    }
}
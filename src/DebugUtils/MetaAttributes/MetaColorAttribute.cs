#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    public interface IMetaColor
    {
        #region Properties
        byte R { get; }
        byte G { get; }
        byte B { get; }
        byte A { get; }
        float FloatR { get; }
        float FloatG { get; }
        float FloatB { get; }
        float FloatA { get; }
        #endregion
    }
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaColorAttribute : EcsMetaAttribute, IMetaColor
    {
        public readonly MetaColor color;

        #region Properties
        public byte R
        {
            get { return color.r; }
        }
        public byte G
        {
            get { return color.g; }
        }
        public byte B
        {
            get { return color.b; }
        }
        public byte A
        {
            get { return color.a; }
        }
        public float FloatR
        {
            get { return R / (float)byte.MaxValue; }
        }
        public float FloatG
        {
            get { return G / (float)byte.MaxValue; }
        }
        public float FloatB
        {
            get { return B / (float)byte.MaxValue; }
        }
        public float FloatA
        {
            get { return A / (float)byte.MaxValue; }
        }
        #endregion

        #region Constructors
        public MetaColorAttribute(byte r, byte g, byte b)
        {
            color = new MetaColor(r, g, b, 255);
        }
        public MetaColorAttribute(uint colorCode)
        {
            color = new MetaColor(colorCode, true);
        }
        #endregion
    }
    [Serializable]
    [DataContract]
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    public readonly struct MetaColor : IMetaColor, IEquatable<MetaColor>
    {
        #region Consts
        public static readonly MetaColor BlackColor = new MetaColor(Black);
        /// <summary> color code Red. RGB is (255, 0, 0)</summary>
        public const uint Red = 0x_ff0000_ff;
        /// <summary> color code Green. RGB is (0, 255, 0)</summary>
        public const uint Green = 0x_00ff00_ff;
        /// <summary> color code Blue. RGB is (0, 0, 255)</summary>
        public const uint Blue = 0x_0000ff_ff;
        /// <summary> color code Yellow. RGB is (255, 255, 0)</summary>
        public const uint Yellow = 0x_ffff00_ff;
        /// <summary> color code Cyan. RGB is (0, 255, 255)</summary>
        public const uint Cyan = 0x_00ffff_ff;
        /// <summary> color code Magenta. RGB is (255, 0, 255)</summary>
        public const uint Magenta = 0x_ff00ff_ff;
        /// <summary> color code White. RGB is (255, 255, 255)</summary>
        public const uint White = 0x_ffffff_ff;
        /// <summary> color code Black. RGB is (0, 0, 0)</summary>
        public const uint Black = 0x_000000_ff;

        /// <summary> color code OrangeRed. RGB is (255, 69, 0)</summary>
        public const uint OrangeRed = 0x_ff4500_ff;
        /// <summary> color code Orange. RGB is (255, 165, 0)</summary>
        public const uint Orange = 0x_ffa500_ff;
        /// <summary> color code Purple. RGB is (128, 0, 128)</summary>
        public const uint Purple = 0x_800080_ff;
        /// <summary> color code Pink. RGB is (255, 192, 203)</summary>
        public const uint Pink = 0x_ffc0cb_ff;
        /// <summary> color code Brown. RGB is (165, 42, 42)</summary>
        public const uint Brown = 0x_a52a2a_ff;
        /// <summary> color code Grey/Gray. RGB is (128, 128, 128)</summary>
        public const uint Gray = 0x_808080_ff;
        /// <summary> color code Grey/Gray. RGB is (128, 128, 128)</summary>
        public const uint Grey = Gray;
        /// <summary> color code LightGray. RGB is (211, 211, 211)</summary>
        public const uint LightGray = 0x_d3d3d3_ff;
        /// <summary> color code DarkGray. RGB is (64, 64, 64)</summary>
        public const uint DarkGray = 0x_404040_ff;
        /// <summary> color code Lime. RGB is (125, 255, 0)</summary>
        public const uint Lime = 0x_7dff00_ff;
        /// <summary> color code Teal. RGB is (0, 128, 128)</summary>
        public const uint Teal = 0x_008080_ff;
        /// <summary> color code Olive. RGB is (128, 128, 0)</summary>
        public const uint Olive = 0x_808000_ff;
        /// <summary> color code Navy. RGB is (0, 0, 128)</summary>
        public const uint Navy = 0x_000080_ff;
        /// <summary> color code Maroon. RGB is (128, 0, 0)</summary>
        public const uint Maroon = 0x_800000_ff;
        /// <summary> color code Aquamarine. RGB is (127, 255, 212)</summary>
        public const uint Aquamarine = 0x_7fffd4_ff;
        /// <summary> color code Fuchsia. RGB is (255, 0, 255)</summary>
        public const uint Fuchsia = 0x_ff00ff_ff;
        /// <summary> color code Silver. RGB is (192, 192, 192)</summary>
        public const uint Silver = 0x_c0c0c0_ff;
        /// <summary> color code Gold. RGB is (255, 215, 0)</summary>
        public const uint Gold = 0x_ffd700_ff;
        /// <summary> color code Indigo. RGB is (75, 0, 130)</summary>
        public const uint Indigo = 0x_4b0082_ff;
        /// <summary> color code Violet. RGB is (238, 130, 238)</summary>
        public const uint Violet = 0x_ee82ee_ff;
        /// <summary> color code Coral. RGB is (255, 127, 80)</summary>
        public const uint Coral = 0x_ff7f50_ff;
        /// <summary> color code Salmon. RGB is (250, 128, 114)</summary>
        public const uint Salmon = 0x_fa8072_ff;
        /// <summary> color code Turquoise. RGB is (64, 224, 208)</summary>
        public const uint Turquoise = 0x_40e0d0_ff;
        /// <summary> color code SkyBlue. RGB is (135, 206, 235)</summary>
        public const uint SkyBlue = 0x_87ceeb_ff;
        /// <summary> color code Plum. RGB is (221, 160, 221)</summary>
        public const uint Plum = 0x_dda0dd_ff;
        /// <summary> color code Khaki. RGB is (213, 197, 138)</summary>
        public const uint Khaki = 0x_d5c58a_ff;
        /// <summary> color code Beige. RGB is (237, 232, 208)</summary>
        public const uint Beige = 0x_ede8d0_ff;
        /// <summary> color code Lavender. RGB is (230, 230, 250)</summary>
        public const uint Lavender = 0x_e6e6fa_ff;

        /// <summary> color code Goldenrod. RGB is (218, 165, 32)</summary>
        public const uint Goldenrod = 0x_daa520_ff;
        /// <summary> color code DeepPink. RGB is (255, 105, 180)</summary>
        public const uint DeepPink = 0x_ff69b4_ff;
        /// <summary> color code Crimson. RGB is (220, 20, 60)</summary>
        public const uint Crimson = 0x_dc143c_ff;
        /// <summary> color code BlueViolet. RGB is (138, 43, 226)</summary>
        public const uint BlueViolet = 0x_8a2be2_ff;
        /// <summary> color code AmericanRose. RGB is (255, 3, 62)</summary>
        public const uint AmericanRose = 0x_ff033e_ff;

        /// <summary> color code DragonRose. RGB is (255, 78, 133)</summary>
        public const uint DragonRose = 0x_ff4e85_ff;
        /// <summary> color code DragonCyan. RGB is (0, 255, 156)</summary>
        public const uint DragonCyan = 0x_00ff9c_ff;
        #endregion

        [FieldOffset(0), NonSerialized] public readonly uint colorCode;
        [FieldOffset(3), DataMember] public readonly byte r;
        [FieldOffset(2), DataMember] public readonly byte g;
        [FieldOffset(1), DataMember] public readonly byte b;
        [FieldOffset(0), DataMember] public readonly byte a;

        #region Properties
        byte IMetaColor.R
        {
            get { return r; }
        }
        byte IMetaColor.G
        {
            get { return g; }
        }
        byte IMetaColor.B
        {
            get { return b; }
        }
        byte IMetaColor.A
        {
            get { return a; }
        }
        public float FloatR
        {
            get { return r / (float)byte.MaxValue; }
        }
        public float FloatG
        {
            get { return g / (float)byte.MaxValue; }
        }
        public float FloatB
        {
            get { return b / (float)byte.MaxValue; }
        }
        public float FloatA
        {
            get { return a / (float)byte.MaxValue; }
        }
        #endregion

        #region Constructors
        public MetaColor(byte r, byte g, byte b) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
            a = 255;
        }
        public MetaColor(byte r, byte g, byte b, byte a) : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public MetaColor(uint colorCode) : this()
        {
            this.colorCode = colorCode;
        }
        public MetaColor(uint colorCode, bool withoutAlpha) : this()
        {
            this.colorCode = withoutAlpha ? colorCode | 255 : colorCode;
        }
        public static MetaColor FromHashCode(int hash)
        {
            return FromHashCode(hash, false);
        }
        public static MetaColor FromHashCode(int hash, bool withoutAlpha)
        {
            unchecked
            {
                uint colorCode = (uint)hash;
                colorCode = BitsUtility.NextXorShiftState(colorCode);
                return new MetaColor(colorCode, withoutAlpha);
            }
        }
        #endregion

        #region Deconstructs
        public void Deconstruct(out byte r, out byte g, out byte b)
        {
            r = this.r;
            g = this.g;
            b = this.b;
        }
        public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
        {
            r = this.r;
            g = this.g;
            b = this.b;
            a = this.a;
        }
        #endregion

        #region Methods
        public MetaColor Desaturate(float t)
        {
            byte r = this.r;
            byte g = this.g;
            byte b = this.b;
            //byte gray = (byte)(r * 0.299 + g * 0.587 + b * 0.114);
            byte gray = (byte)(r * 0.333333 + g * 0.333333 + b * 0.333333);
            r = (byte)(r + (gray - r) * (1 - t));
            g = (byte)(g + (gray - g) * (1 - t));
            b = (byte)(b + (gray - b) * (1 - t));
            return new MetaColor(r, g, b);
        }
        public MetaColor UpContrast()
        {
            byte minChannel = Math.Min(Math.Min(r, g), b);
            byte maxChannel = Math.Max(Math.Max(r, g), b);
            if (maxChannel == minChannel)
            {
                return default;
            }
            float factor = 255f / (maxChannel - minChannel);
            return new MetaColor((byte)((r - minChannel) * factor), (byte)((g - minChannel) * factor), (byte)((b - minChannel) * factor));
        }
        #endregion

        #region Operators
        public static implicit operator MetaColor((byte, byte, byte) a) { return new MetaColor(a.Item1, a.Item2, a.Item3); }
        public static implicit operator MetaColor((byte, byte, byte, byte) a) { return new MetaColor(a.Item1, a.Item2, a.Item3, a.Item4); }
        public static implicit operator MetaColor(uint a) { return new MetaColor(a); }
        public static bool operator ==(MetaColor a, MetaColor b) { return a.colorCode == b.colorCode; }
        public static bool operator !=(MetaColor a, MetaColor b) { return a.colorCode != b.colorCode; }

        public static MetaColor operator /(MetaColor a, float b)
        {
            return new MetaColor(
                (byte)(a.r / b),
                (byte)(a.g / b),
                (byte)(a.b / b));
        }
        public static MetaColor operator /(float b, MetaColor a)
        {
            return new MetaColor(
                (byte)(a.r / b),
                (byte)(a.g / b),
                (byte)(a.b / b));
        }
        public static MetaColor operator /(MetaColor a, MetaColor b)
        {
            return new MetaColor(
                (byte)(a.r / b.r),
                (byte)(a.g / b.g),
                (byte)(a.b / b.b));
        }

        public static MetaColor operator *(MetaColor a, float b)
        {
            return new MetaColor(
                (byte)(a.r * b),
                (byte)(a.g * b),
                (byte)(a.b * b));
        }

        public static MetaColor operator *(float b, MetaColor a)
        {
            return new MetaColor(
                (byte)(a.r * b),
                (byte)(a.g * b),
                (byte)(a.b * b));
        }
        public static MetaColor operator *(MetaColor a, MetaColor b)
        {
            return new MetaColor(
                (byte)(a.r * b.r),
                (byte)(a.g * b.g),
                (byte)(a.b * b.b));
        }

        public static MetaColor operator +(MetaColor a, byte b)
        {
            return new MetaColor(
                (byte)(a.r + b),
                (byte)(a.g + b),
                (byte)(a.b + b));
        }
        public static MetaColor operator +(byte b, MetaColor a)
        {
            return new MetaColor(
                (byte)(a.r + b),
                (byte)(a.g + b),
                (byte)(a.b + b));
        }
        public static MetaColor operator +(MetaColor a, MetaColor b)
        {
            return new MetaColor(
                (byte)(a.r + b.r),
                (byte)(a.g + b.g),
                (byte)(a.b + b.b));
        }

        public static MetaColor operator -(MetaColor a, byte b)
        {
            return new MetaColor(
                (byte)(a.r - b),
                (byte)(a.g - b),
                (byte)(a.b - b));
        }
        public static MetaColor operator -(byte b, MetaColor a)
        {
            return new MetaColor(
                (byte)(a.r - b),
                (byte)(a.g - b),
                (byte)(a.b - b));
        }
        public static MetaColor operator -(MetaColor a, MetaColor b)
        {
            return new MetaColor(
                (byte)(a.r - b.r),
                (byte)(a.g - b.g),
                (byte)(a.b - b.b));
        }
        #endregion

        #region Other
        public override string ToString() { return $"({r}, {g}, {b}, {a})"; }
        public bool Equals(MetaColor other) { return colorCode == other.colorCode; }
        public override bool Equals(object obj) { return obj is MetaColor other && Equals(other); }
        public override int GetHashCode() { return unchecked((int)colorCode); }
        #endregion
    }
}
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
        public MetaColorAttribute(int colorCode)
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


        /// <summary> RGB is (255, 78, 133) color code </summary>
        public const int DragonRose = (255 << 24) | (78 << 16) | (133 << 8) | 255;
        /// <summary> RGB is (0, 255, 156) color code </summary>
        public const int DragonCyan = (0 << 24) | (255 << 16) | (156 << 8) | 255;
        #endregion

        [FieldOffset(0), NonSerialized] public readonly int colorCode;
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
        public MetaColor(int colorCode) : this()
        {
            this.colorCode = colorCode;
        }
        public MetaColor(int colorCode, bool withoutAlpha) : this()
        {
            this.colorCode = withoutAlpha ? colorCode | 255 : colorCode;
        }
        public MetaColor(string stringCode) : this()
        {
            unchecked
            {
                const uint MAGIC_CONST = 0xA638_783E;
                uint colorCode = (uint)stringCode.GetHashCode();
                colorCode ^= MAGIC_CONST;
                colorCode = BitsUtility.NextXorShiftState(colorCode);
                this.colorCode = (int)colorCode | 255;
                this.colorCode = UpContrast().colorCode;
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
        public static implicit operator MetaColor(int a) { return new MetaColor(a); }
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
        public override int GetHashCode() { return colorCode; }
        #endregion
    }
}
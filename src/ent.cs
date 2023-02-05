using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly struct ent : IEquatable<long>, IEquatable<ent>
    {
        //private const int ID_BITS = 32;
        //private const int GEN_BITS = 16;
        //private const int WORLD_BITS = 8;
        //private const int COM_BITS = 8;

        public readonly long _full;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_full >> 32);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public short gen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)((_full << 32) >> 48);

        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public byte world
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((_full << 48) >> 56);

        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public byte com
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)((_full << 56) >> 56);

        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ent(int id, short gen, byte world, byte com)
        {
            _full = ((long)id) << 32;
            _full += ((long)gen) << 16;
            _full += ((long)world) << 8;
            _full += com;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ent(long value)
        {
            _full = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TypeCode GetTypeCode()
        {
            return TypeCode.Int64;
        }

        #region GetHashCode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return unchecked((int)(_full)) ^ (int)(_full >> 32);
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in ent other)
        {
            return _full == other._full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is ent other && Equals(in other);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ent other)
        {
            return _full == other._full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other)
        {
            return _full == other;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ent left, in ent right) => left.Equals(in right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ent left, in ent right) => !left.Equals(in right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(in ent eent) => eent._full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ent(in long value) => new ent(value);
        #endregion
    }
}

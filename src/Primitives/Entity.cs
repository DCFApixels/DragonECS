using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly struct ent : IEquatable<long>, IEquatable<ent>
    {
        public static readonly ent NULL = default;

        // id - 32 bits
        // gen - 16 bits
        // world - 16 bits
        public readonly long _full;

        #region Properties
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_full >> 32);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ushort gen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)((_full << 32) >> 48);

        }
        // но чтобы значене default было NULL сульностью, мир хранится в виде ID + 1
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ushort world
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)(((_full << 48) >> 48) - 1);

        }
        #endregion

        #region Constructors
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ent(int id, short gen, ushort world)
        {
            _full = ((long)id) << 32;
            _full += ((long)gen) << 16;
            _full += ++world; // сдвиг айдишников + 1
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ent(long value)
        {
            _full = value;
        }
        #endregion

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
        public static implicit operator int(in ent eent) => eent.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ent(in long value) => new ent(value);
        #endregion
    }

    public static class entExtensions
    {
        public static bool IsNull(this in ent self)
        {
            return self == ent.NULL;
        }
    }

    public readonly ref struct Entity
    {
        internal readonly IEcsWorld world;
        internal readonly int id;
        public Entity(IEcsWorld world, ent id)
        {
            this.world = world;
            this.id = id;
        }
    }
}

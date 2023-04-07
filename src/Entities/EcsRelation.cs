using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    /// <summary>Permanent relation entity identifier</summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    public readonly partial struct EcsRelation : IEquatable<long>, IEquatable<EcsRelation>
    {
        public static readonly EcsEntity NULL = default;
        // uniqueID - 32 bits
        // gen - 16 bits
        // world - 16 bits
        [FieldOffset(0)]
        internal readonly long full; //Union
        [FieldOffset(3)]
        public readonly int id;
        [FieldOffset(1)]
        public readonly short rightWorld;
        [FieldOffset(0)]
        public readonly short leftWorld;

        public ent Ent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ent(id);
        }

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsRelation(int id, short leftWorld, short rightWorld) : this()
        {
            this.id = id;
            this.leftWorld = leftWorld;
            this.rightWorld = rightWorld;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsRelation(long full) : this()
        {
            this.full = full;
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EcsRelation other) => full == other.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) => full == other;
        #endregion

        #region Object
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => unchecked((int)full) ^ (int)(full >> 32);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"Relation(id:{id} leftWorld:{leftWorld} rightWorld:{rightWorld})";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is EcsRelation other && full == other.full;
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in EcsRelation a, in EcsRelation b) => a.full == b.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in EcsRelation a, in EcsRelation b) => a.full != b.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(in EcsRelation a) => a.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ent(in EcsRelation a) => a.Ent;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator EcsRelation(in long a) => new EcsRelation(a);
        #endregion
    }
}

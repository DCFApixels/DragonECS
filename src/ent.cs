using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    // id - 32 bits
    // gen - 16 bits
    // world - 16 bits
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    public readonly struct ent : IEquatable<long>, IEquatable<ent>
    {
        public static readonly ent NULL = default;

        [FieldOffset(0)]
        private readonly long _full;
        [FieldOffset(3)]
        public readonly int id;
        [FieldOffset(1)]
        public readonly short gen;
        [FieldOffset(0)]
        public readonly short world;

        #region Constructors
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ent(int id, short gen, short world): this()
        {
            this.id = id;
            this.gen = gen;
            this.world = world;
        }
        internal ent(long full) : this()
        {
            _full = full;
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
        public override bool Equals(object obj)
        {
            return obj is ent other && _full == other._full;
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

        #region ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"ent(id:{id} gen:{gen} world:{world})";
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ent a, in ent b) => a._full == b._full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ent a, in ent b) => a._full != b._full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(in ent a) => a._full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(in ent a) => a.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ent(in long a) => new ent(a);
        #endregion
    }

    public static partial class entExtensions
    {
        private static EcsProfilerMarker _IsAliveMarker = new EcsProfilerMarker("ent.IsAlive");
        private static EcsProfilerMarker _IsNullMarker = new EcsProfilerMarker("ent.IsNull");
        private static EcsProfilerMarker _ReadMarker = new EcsProfilerMarker("ent.Read");
        private static EcsProfilerMarker _WriteMarker = new EcsProfilerMarker("ent.Write");
        private static EcsProfilerMarker _HasMarker = new EcsProfilerMarker("ent.Has");
        private static EcsProfilerMarker _DelMarker = new EcsProfilerMarker("ent.Del");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlive(this ref ent self)
        {
            //using (_IsAliveMarker.Auto())
            //{
                bool result = EcsWorld.Worlds[self.world].EntityIsAlive(self.id, self.gen);
                if (!result) self = ent.NULL;
                return result;
            //}
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(this in ent self)
        {
            //using (_IsNullMarker.Auto())
                return self == ent.NULL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T Read<T>(this in ent self)
            where T : struct
        {
            //using (_ReadMarker.Auto())
                return ref EcsWorld.Worlds[self.world].GetPool<T>().Read(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Write<T>(this in ent self)
            where T : struct
        {
            //using (_WriteMarker.Auto())
                return ref EcsWorld.Worlds[self.world].GetPool<T>().Write(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this in ent self)
            where T : struct
        {
            //using (_HasMarker.Auto())
                return EcsWorld.Worlds[self.world].GetPool<T>().Has(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotHas<T>(this in ent self) where T : struct => !Has<T>(in self);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Del<T>(this in ent self)
            where T : struct
        {
            //using (_DelMarker.Auto())
                EcsWorld.Worlds[self.world].GetPool<T>().Del(self.id);
        }
    }
}

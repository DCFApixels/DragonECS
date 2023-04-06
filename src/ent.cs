using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEntityRecord : ITabelRecord
    {
        public bool IsSpecific { get; }
        internal long Full { get; }
        public short Gen { get; }
        public short World { get; }
    }
    // uniqueID - 32 bits
    // gen - 16 bits
    // world - 16 bits
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    public readonly partial struct ent : 
        IEquatable<long>, 
        IEquatable<ent>,
        IEntityRecord
    {
        public static readonly ent NULL = default;

        [FieldOffset(0)]
        internal readonly long full; //Union
        [FieldOffset(3)]
        public readonly int id;
        [FieldOffset(1)]
        public readonly short gen;
        [FieldOffset(0)]
        public readonly short world;

        #region IEntityRecord
        bool IEntityRecord.IsSpecific => false;
        long IEntityRecord.Full => full;
        int ITabelRecord.Id => id;
        short IEntityRecord.Gen => gen;
        short IEntityRecord.World => world;
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ent(int id, short gen, short world) : this()
        {
            this.id = id;
            this.gen = gen;
            this.world = world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ent(long full) : this()
        {
            this.full = full;
        }
        #endregion

        #region GetHashCode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => unchecked((int)(full)) ^ (int)(full >> 32);
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is ent other && full == other.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ent other) => full == other.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) => full == other;
        #endregion

        #region ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"ent(uniqueID:{id} gen:{gen} world:{world})";
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ent a, in ent b) => a.full == b.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ent a, in ent b) => a.full != b.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(in ent a) => a.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(in ent a) => a.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ent(in long a) => new ent(a);
        #endregion
    }

    public readonly partial struct ent
    {
        private static EcsProfilerMarker _IsNullMarker = new EcsProfilerMarker("ent.IsNull");
        private static EcsProfilerMarker _ReadMarker = new EcsProfilerMarker("ent.Read");
        private static EcsProfilerMarker _WriteMarker = new EcsProfilerMarker("ent.Write");
        private static EcsProfilerMarker _HasMarker = new EcsProfilerMarker("ent.Has");
        private static EcsProfilerMarker _DelMarker = new EcsProfilerMarker("ent.Del");

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                //using (_IsNullMarker.Auto())
                return this == NULL;
            }
        }

        public ref readonly T Read<T>()
            where T : struct
        {
            //using (_ReadMarker.Auto())
            return ref EcsWorld.Worlds[world].GetPool<T>().Read(id);
        }
        public ref T Write<T>()
            where T : struct
        {
            //using (_WriteMarker.Auto())
            return ref EcsWorld.Worlds[world].GetPool<T>().Write(id);
        }
        public bool Has<T>()
            where T : struct
        {
            //using (_HasMarker.Auto())
            return EcsWorld.Worlds[world].GetPool<T>().Has(id);
        }
        public bool NotHas<T>()
            where T : struct
        {
            //using (_HasMarker.Auto())
            return EcsWorld.Worlds[world].GetPool<T>().Has(id);
        }
        public void Del<T>()
            where T : struct
        {
            //using (_DelMarker.Auto())
            EcsWorld.Worlds[world].GetPool<T>().Del(id);
        }
    } 

    public static partial class entExtensions
    {
        private static EcsProfilerMarker _IsAliveMarker = new EcsProfilerMarker("ent.IsAlive");

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
    }
}

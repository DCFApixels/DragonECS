#pragma warning disable IDE1006
#pragma warning disable CS8981
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    // [        id 32        |  gen 16  | world 16 ]
    /// <summary>Strong identifier/Permanent entity identifier</summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    [Serializable]
    public readonly struct entlong : IEquatable<long>, IEquatable<entlong>
    {
        public static readonly entlong NULL = default;
        [FieldOffset(0)]
        internal readonly long full; //Union
        [FieldOffset(0), NonSerialized]
        internal readonly int id;
        [FieldOffset(4), NonSerialized]
        internal readonly short gen;
        [FieldOffset(6), NonSerialized]
        internal readonly short world;

        #region Properties
        public bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => EcsWorld.GetWorld(world).IsAlive(id, gen);
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => full == 0L;
        }
        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) Throw.Ent_ThrowIsNotAlive(this);
#endif
                return id;
            }
        }
        public short Gen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) Throw.Ent_ThrowIsNotAlive(this);
#endif
                return gen;
            }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) Throw.Ent_ThrowIsNotAlive(this);
#endif
                return EcsWorld.GetWorld(world);
            }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) Throw.Ent_ThrowIsNotAlive(this);
#endif
                return world;
            }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(int id, short gen, short world) : this()
        {
            this.id = id;
            this.gen = gen;
            this.world = world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal entlong(long full) : this()
        {
            this.full = full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe entlong NewUnsafe(long id, long gen, long world)
        {
            long x = id << 48 | gen << 32 | id;
            return *(entlong*)&x;
        }
        #endregion

        #region TryGetters
        public bool TryGetID(out int id)
        {
            id = this.id;
            return IsAlive;
        }
        public bool TryGetWorld(out EcsWorld world)
        {
            world = EcsWorld.GetWorld(this.world);
            return IsAlive;
        }
        public bool TryGetWorldID(out int worldID)
        {
            worldID = world;
            return IsAlive;
        }
        public void Unpack(out EcsWorld world, out int id)
        {
            world = EcsWorld.GetWorld(this.world);
            id = this.id;
        }
        public void Unpack(out int worldID, out int id)
        {
            worldID = world;
            id = this.id;
        }
        public bool TryUnpack(out EcsWorld world, out int id)
        {
            world = EcsWorld.GetWorld(this.world);
            id = this.id;
            return IsAlive;
        }
        public bool TryUnpack(out int worldID, out int id)
        {
            worldID = world;
            id = this.id;
            return IsAlive;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(entlong a, entlong b) => a.full == b.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(entlong a, entlong b) => a.full != b.full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(entlong a) => a.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator entlong(long a) => new entlong(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(entlong a) => a.ID;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int gen, out int world)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!IsAlive) Throw.Ent_ThrowIsNotAlive(this);
#endif
            id = this.id;
            gen = this.gen;
            world = this.world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int world)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!IsAlive) Throw.Ent_ThrowIsNotAlive(this);
#endif
            id = this.id;
            world = this.world;
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => unchecked((int)full) ^ (int)(full >> 32);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"entity(id:{id} g:{gen} w:{world} {(IsNull ? "null" : IsAlive ? "alive" : "not alive")})";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is entlong other && full == other.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(entlong other) => full == other.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) => full == other;

        internal class DebuggerProxy
        {
            private List<object> _componentsList;
            private entlong _value;
            public long full => _value.full;
            public int id => _value.id;
            public int gen => _value.gen;
            public int world => _value.world;
            public EntState State => _value.IsNull ? EntState.Null : _value.IsAlive ? EntState.Alive : EntState.Dead;
            public EcsWorld EcsWorld => EcsWorld.GetWorld(world);
            public IEnumerable<object> components
            {
                get
                {
                    _value.World.GetComponents(_value.ID, _componentsList);
                    return _componentsList;
                }
            }
            public DebuggerProxy(entlong value)
            {
                _value = value;
                _componentsList = new List<object>();
            }
            public enum EntState { Null, Dead, Alive, }
        }
        #endregion
    }
}

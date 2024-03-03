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
            get { return EcsWorld.GetWorld(world).IsAlive(id, gen); }
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return full == 0L; }
        }
        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) { Throw.Ent_ThrowIsNotAlive(this); }
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
                if (!IsAlive) { Throw.Ent_ThrowIsNotAlive(this); }
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
                if (!IsAlive) { Throw.Ent_ThrowIsNotAlive(this); }
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
                if (!IsAlive) { Throw.Ent_ThrowIsNotAlive(this); }
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
        public void Unpack(out int id, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(this.world);
            id = this.id;
        }
        public void Unpack(out int id, out short gen, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(this.world);
            gen = this.gen;
            id = this.id;
        }
        public void Unpack(out int id, out int worldID)
        {
            worldID = world;
            id = this.id;
        }
        public void Unpack(out int id, out short gen, out short worldID)
        {
            worldID = world;
            gen = this.gen;
            id = this.id;
        }
        public bool TryUnpack(out int id, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(this.world);
            id = this.id;
            return IsAlive;
        }
        public bool TryUnpack(out int id, out short gen, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(this.world);
            gen = this.gen;
            id = this.id;
            return IsAlive;
        }
        public bool TryUnpack(out int id, out int worldID)
        {
            worldID = world;
            id = this.id;
            return IsAlive;
        }
        public bool TryUnpack(out int id, out short gen, out short worldID)
        {
            worldID = world;
            gen = this.gen;
            id = this.id;
            return IsAlive;
        }
        #endregion

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(entlong a, entlong b) { return a.full == b.full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(entlong a, entlong b) { return a.full != b.full; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(entlong a) { return a.full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator entlong(long a) { return new entlong(a); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(entlong a) { return a.ID; }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return unchecked((int)full) ^ (int)(full >> 32); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() { return $"entity(id:{id} g:{gen} w:{world} {(IsNull ? "null" : IsAlive ? "alive" : "not alive")})"; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) { return obj is entlong other && full == other.full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(entlong other) { return full == other.full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) { return full == other; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int gen, out int world)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!IsAlive) { Throw.Ent_ThrowIsNotAlive(this); }
#endif
            id = this.id;
            gen = this.gen;
            world = this.world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int world)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!IsAlive) { Throw.Ent_ThrowIsNotAlive(this); }
#endif
            id = this.id;
            world = this.world;
        }

        internal class DebuggerProxy
        {
            private List<object> _componentsList = new List<object>();
            private entlong _value;
            public long full { get { return _value.full; } }
            public int id { get { return _value.id; } }
            public int gen { get { return _value.gen; } }
            public int world { get { return _value.world; } }
            public EntState State { get { return _value.IsNull ? EntState.Null : _value.IsAlive ? EntState.Alive : EntState.Dead; } }
            public EcsWorld EcsWorld { get { return EcsWorld.GetWorld(world); } }
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
            }
            public DebuggerProxy(EntitySlotInfo value)
            {
                _value = new entlong(value.id, value.gen, value.world);
            }
            public enum EntState { Null, Dead, Alive, }
        }
        #endregion
    }


    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EntitySlotInfo : IEquatable<EntitySlotInfo>
    {
        public readonly int id;
        public readonly short gen;
        public readonly short world;

        #region Constructors
        public EntitySlotInfo(int id, short gen, short world)
        {
            this.id = id;
            this.gen = gen;
            this.world = world;
        }
        #endregion

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EntitySlotInfo a, EntitySlotInfo b)
        {
            return a.id == b.id &&
                a.gen == b.gen &&
                a.world == b.world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(EntitySlotInfo a, EntitySlotInfo b)
        {
            return a.id != b.id ||
                a.gen != b.gen ||
                a.world != b.world;
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return unchecked(id ^ gen ^ (world * EcsConsts.MAGIC_PRIME)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() { return $"slot(id:{id} g:{gen} w:{world})"; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) { return obj is EntitySlotInfo other && this == other; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EntitySlotInfo other) { return this == other; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int gen, out int world)
        {
            id = this.id;
            gen = this.gen;
            world = this.world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int world)
        {
            id = this.id;
            world = this.world;
        }
        #endregion


        internal class DebuggerProxy
        {
            private List<object> _componentsList = new List<object>();
            private entlong _value;
            public long full { get { return _value.full; } }
            public int id { get { return _value.id; } }
            public int gen { get { return _value.gen; } }
            public int world { get { return _value.world; } }
            public EntState State { get { return _value.IsNull ? EntState.Null : _value.IsAlive ? EntState.Alive : EntState.Dead; } }
            public EcsWorld EcsWorld { get { return EcsWorld.GetWorld(world); } }
            public IEnumerable<object> components
            {
                get
                {
                    _value.World.GetComponents(_value.ID, _componentsList);
                    return _componentsList;
                }
            }
            public DebuggerProxy(EntitySlotInfo value)
            {
                _value = new entlong(value.id, value.gen, value.world);
            }
            public enum EntState { Null, Dead, Alive, }
        }
    }
}

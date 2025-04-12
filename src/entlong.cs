#if DISABLE_DEBUG
#undef DEBUG
#endif
#pragma warning disable IDE1006
#pragma warning disable CS8981
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
//using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    // [        id 32        |  gen 16  | world 16 ]
    /// <summary>Strong identifier/Permanent entity identifier</summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    //[DataContract]
    [Serializable]
#if UNITY_EDITOR
    public
#else
    public readonly
#endif
        struct entlong : IEquatable<long>, IEquatable<entlong>, IComparable<entlong>
    {
        public static readonly entlong NULL = default;
        //[DataMember]
        [FieldOffset(0)]
#if UNITY_EDITOR
        [UnityEngine.SerializeField]
        internal
#else
        internal readonly
#endif
            long _full; //Union
        [FieldOffset(0), NonSerialized]
        internal readonly int _id;
        [FieldOffset(4), NonSerialized]
        internal readonly short _gen;
        [FieldOffset(6), NonSerialized]
        internal readonly short _world;

        #region Properties
        public bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EcsWorld.GetWorld(_world).IsAlive(_id, _gen); }
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _full == 0L; }
        }
        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
                if (IsAlive == false) { return EcsConsts.NULL_ENTITY_ID; }
#endif
                return _id;
            }
        }
        public short Gen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
                if (IsAlive == false) { return default; }
#endif
                return _gen;
            }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#endif
                return GetWorld_Internal();
            }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
                if (IsAlive == false) { return EcsConsts.NULL_WORLD_ID; }
#endif
                return _world;
            }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(int id, short gen, short world) : this()
        {
            _id = id;
            _gen = gen;
            _world = world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal entlong(long full) : this()
        {
            _full = full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe entlong NewUnsafe(long id, long gen, long world)
        {
            long x = id << 48 | gen << 32 | id;
            return *(entlong*)&x;
        }
        #endregion

        #region Unpacking
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetID(out int id)
        {
            id = _id;
            return IsAlive;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetWorld(out EcsWorld world)
        {
            world = EcsWorld.GetWorld(_world);
            return IsAlive;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetWorldID(out short worldID)
        {
            worldID = _world;
            return IsAlive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unpack(out int id)
        {
#if DEBUG
            if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
            if (IsAlive == false)
            {
                id = EcsConsts.NULL_ENTITY_ID;
                return;
            }
#endif
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unpack(out int id, out EcsWorld world)
        {
#if DEBUG
            if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
            if (IsAlive == false)
            {
                world = EcsWorld.GetWorld(EcsConsts.NULL_WORLD_ID);
                id = EcsConsts.NULL_ENTITY_ID;
                return;
            }
#endif
            world = EcsWorld.GetWorld(_world);
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unpack(out int id, out short gen, out EcsWorld world)
        {
#if DEBUG
            if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
            if (IsAlive == false)
            {
                world = EcsWorld.GetWorld(EcsConsts.NULL_WORLD_ID);
                gen = default;
                id = EcsConsts.NULL_ENTITY_ID;
                return;
            }
#endif
            world = EcsWorld.GetWorld(_world);
            gen = _gen;
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unpack(out int id, out short worldID)
        {
#if DEBUG
            if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
            if (IsAlive == false)
            {
                worldID = EcsConsts.NULL_WORLD_ID;
                id = EcsConsts.NULL_ENTITY_ID;
                return;
            }
#endif
            worldID = _world;
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unpack(out int id, out short gen, out short worldID)
        {
#if DEBUG
            if (IsAlive == false) { Throw.Ent_ThrowIsNotAlive(this); }
#elif DRAGONECS_STABILITY_MODE
            if (IsAlive == false)
            {
                worldID = EcsConsts.NULL_WORLD_ID;
                gen = default;
                id = EcsConsts.NULL_ENTITY_ID;
                return;
            }
#endif
            worldID = _world;
            gen = _gen;
            id = _id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id)
        {
            id = _id;
            return IsAlive;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out EcsWorld world)
        {
            world = GetWorld_Internal();
            id = _id;
            return IsAlive;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out short gen, out EcsWorld world)
        {
            world = GetWorld_Internal();
            gen = _gen;
            id = _id;
            return IsAlive;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out short worldID)
        {
            worldID = _world;
            id = _id;
            return IsAlive;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out short gen, out short worldID)
        {
            worldID = _world;
            gen = _gen;
            id = _id;
            return IsAlive;
        }
        #endregion

        #region Unpacking
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIDUnchecked()
        {
            return _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorldUnchecked()
        {
            return EcsWorld.GetWorld(_world);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetWorldIDUnchecked()
        {
            return _world;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(_world);
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out short gen, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(_world);
            gen = _gen;
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out short worldID)
        {
            worldID = _world;
            id = _id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out short gen, out short worldID)
        {
            worldID = _world;
            gen = _gen;
            id = _id;
        }
        #endregion

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(entlong a, entlong b) { return a._full == b._full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(entlong a, entlong b) { return a._full != b._full; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator entlong((int entityID, EcsWorld world) a) { return Combine_Internal(a.entityID, a.world); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator entlong((EcsWorld world, int entityID) a) { return Combine_Internal(a.entityID, a.world); }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator entlong((entlong entity, EcsWorld world) a) { return Combine_Internal(a.entity._id, a.world); }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator entlong((EcsWorld world, entlong entity) a) { return Combine_Internal(a.entity._id, a.world); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static entlong Combine_Internal(int entityID, EcsWorld world)
        {
            return world == null ? new entlong(entityID, 0, 0) : world.GetEntityLong(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(entlong a) { return a._full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator entlong(long a) { return new entlong(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(entlong a) { return a.ID; }
        #endregion

        #region Deconstruct
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out short gen, out short worldID) { Unpack(out id, out gen, out worldID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out EcsWorld world) { Unpack(out id, out world); }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EcsWorld GetWorld_Internal()
        {
#if DRAGONECS_STABILITY_MODE
            if (IsAlive == false) { EcsWorld.GetWorld(EcsConsts.NULL_WORLD_ID); }
#endif
            return EcsWorld.GetWorld(_world);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return unchecked((int)_full) ^ (int)(_full >> 32); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() { return $"entity(id:{_id} g:{_gen} w:{_world} {(IsNull ? "null" : IsAlive ? "alive" : "not alive")})"; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) { return obj is entlong other && _full == other._full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(entlong other) { return _full == other._full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) { return _full == other; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(entlong other)
        {
            // NOTE: Because _id cannot be less than 0,
            // the case “_id - other._id > MaxValue” is impossible.
            return _id - other._id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(entlong left, entlong right) { return left.CompareTo(right); }


        internal class DebuggerProxy
        {
            private List<object> _componentsList = new List<object>();
            private entlong _value;
            public long full { get { return _value._full; } }
            public int id { get { return _value._id; } }
            public short gen { get { return _value._gen; } }
            public short world { get { return _value._world; } }
            public EntState State { get { return _value.IsNull ? EntState.Null : _value.IsAlive ? EntState.Alive : EntState.Dead; } }
            public EcsWorld EcsWorld { get { return EcsWorld.GetWorld(world); } }
            public IEnumerable<object> components
            {
                get
                {
                    _value.World.GetComponentsFor(_value.ID, _componentsList);
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
}

namespace DCFApixels.DragonECS
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EntitySlotInfo : IEquatable<EntitySlotInfo>
    {
        private readonly long _full;
        public readonly int id;
        public readonly short gen;
        public readonly short world;

        #region Properties
        private EcsWorld World { get { return EcsWorld.GetWorld(world); } }
        private EntState State { get { return _full == 0 ? EntState.Null : World.IsAlive(id, gen) ? EntState.Alive : EntState.Dead; } }

        #endregion

        #region Constructors
        public EntitySlotInfo(long full)
        {
            unchecked
            {
                ulong ufull = (ulong)full;
                id = (int)((ufull >> 0) & 0x0000_0000_FFFF_FFFF);
                gen = (short)((ufull >> 32) & 0x0000_0000_0000_FFFF);
                world = (short)((ufull >> 48) & 0x0000_0000_0000_FFFF);
                _full = full;
            }
        }
        public EntitySlotInfo(int id, short gen, short world)
        {
            this.id = id;
            this.gen = gen;
            this.world = world;
            _full = ((long)world << 48 | (long)gen << 32 | (long)id);
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
        public override string ToString() { return $"slot(id:{id} g:{gen} w:{world} {(State == EntState.Null ? "null" : State == EntState.Alive ? "alive" : "not alive")})"; }
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
        public enum EntState { Null, Dead, Alive, }
        internal class DebuggerProxy
        {
            private List<object> _componentsList = new List<object>();
            private EntitySlotInfo _source;
            public long full { get { return _source._full; } }
            public int id { get { return _source.id; } }
            public short gen { get { return _source.gen; } }
            public short world { get { return _source.world; } }
            public EntState State { get { return _source.State; } }
            public EcsWorld World { get { return _source.World; } }
            public IEnumerable<object> Components
            {
                get
                {
                    if (State == EntState.Alive)
                    {
                        World.GetComponentsFor(id, _componentsList);
                        return _componentsList;
                    }
                    return Array.Empty<object>();
                }
            }
            public DebuggerProxy(EntitySlotInfo value)
            {
                _source = value;
            }
        }
        #endregion
    }
}
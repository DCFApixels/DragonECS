#if DISABLE_DEBUG
#undef DEBUG
#endif
#pragma warning disable IDE1006
#pragma warning disable CS8981
using DCFApixels.DragonECS.Core.Internal;
using DCFApixels.DragonECS.Core.Unchecked;
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
            get
            {
                return EcsWorld.TryGetWorld(_world, out EcsWorld world) && world.IsAlive(_id, _gen);
            }
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _full == 0L; }
        }
        public bool IsDeadOrNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return IsAlive == false; }
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

        #region Unpacking Try
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsWorld world, out int id)
        {
            if (world.ID != _world) { Throw.ArgumentDifferentWorldsException(); }
            id = _id;
            return world.IsAlive(_id, _gen);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsWorld world, out int id, out short gen)
        {
            if (world.ID != _world) { Throw.ArgumentDifferentWorldsException(); }
            gen = _gen;
            id = _id;
            return world.IsAlive(_id, _gen);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsMask mask, out int id)
        {
            if (mask.WorldID != _world) { Throw.ArgumentDifferentWorldsException(); }
            id = _id;
            return mask.World.IsAlive(_id, _gen) && mask.World.IsMatchesMask(mask, _id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsMask mask, out int id, out short gen)
        {
            if (mask.WorldID != _world) { Throw.ArgumentDifferentWorldsException(); }
            gen = _gen;
            id = _id;
            return mask.World.IsAlive(_id, _gen) && mask.World.IsMatchesMask(mask, _id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsAspect aspect, out int id)
        {
            if (aspect.World.ID != _world) { Throw.ArgumentDifferentWorldsException(); }
            id = _id;
            return aspect.World.IsAlive(_id, _gen) && aspect.IsMatches(_id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsAspect aspect, out int id, out short gen)
        {
            if (aspect.World.ID != _world) { Throw.ArgumentDifferentWorldsException(); }
            gen = _gen;
            id = _id;
            return aspect.World.IsAlive(_id, _gen) && aspect.IsMatches(_id);
        }
        #endregion

        #region Unpacking/Deconstruct
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
        public void Deconstruct(out int id, out short gen, out short worldID) { Unpack(out id, out gen, out worldID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out EcsWorld world) { Unpack(out id, out world); }
        #endregion

        #region Unpacking Unchecked
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
        public int CompareTo(entlong other) { return Compare(_id, other._id); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(entlong left, entlong right) { return left.CompareTo(right); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(int left, int right)
        {
            // NOTE: Because _id cannot be less than 0,
            // the case “_id - other._id > MaxValue” is impossible.
            return left - right;
        }

        internal class DebuggerProxy : EntityDebuggerProxy
        {
            public override long full => base.full;
            public override int id => base.id;
            public override short gen => base.gen;
            public override short worldID => base.worldID;
            public override EntitySlotInfo.StateFlag State => base.State;
            public override EcsWorld World => base.World;
            public override IEnumerable<object> Components { get => base.Components; set => base.Components = value; }
            public DebuggerProxy(entlong entity) : base(entity) { }
        }
        #endregion
    }
}
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
    /// <summary>
    /// Strong identifier / permanent entity identifier that packs entity ID, generation, and world ID into a single 64‑bit value.
    /// </summary>
    /// <remarks>[        id 32        |  gen 16  | world 16 ]</remarks>
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
        /// <summary>
        /// Indicates whether this entity identifier refers to a currently alive entity in its world.
        /// </summary>
        public bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return EcsWorld.TryGetWorld(_world, out EcsWorld world) && world.IsAlive(_id, _gen);
            }
        }

        /// <summary>
        /// Indicates whether this identifier is the null (default) value.
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _full == 0L; }
        }

        /// <summary>
        /// Indicates whether the entity is dead or the identifier is null.
        /// </summary>
        public bool IsDeadOrNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return IsAlive == false; }
        }

        /// <summary>
        /// Gets the entity ID.
        /// </summary>
        /// <remarks>Throws in DEBUG mode if the entity is not alive; in STABILITY_MODE returns <see cref="EcsConsts.NULL_ENTITY_ID"/></remarks>
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

        /// <summary>
        /// Gets the generation number of the entity..
        /// </summary>
        /// <remarks>Throws in DEBUG mode if not alive; in STABILITY_MODE returns default(short)</remarks>
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

        /// <summary>
        /// Gets the <see cref="EcsWorld"/> instance that owns this entity.
        /// </summary>
        /// <remarks>Throws in DEBUG mode if not alive.</remarks>
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

        /// <summary>
        /// Gets the world ID of the owning world.
        /// </summary>
        /// <remarks>Throws in DEBUG mode if not alive; in STABILITY_MODE returns <see cref="EcsConsts.NULL_WORLD_ID"/>.</remarks>
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
        /// <summary>
        /// Creates a new entity identifier from a world and an entity ID.
        /// The generation is automatically retrieved from the world.
        /// </summary>
        /// <param name="world">The world that owns the entity.</param>
        /// <param name="id">The entity ID.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(EcsWorld world, int id) : this()
        {
            if (world == null)
            {
                _id = 0;
                _gen = 0;
                _world = 0;
            }
            else
            {
                _id = id;
                _gen = world.GetGen(id);
                _world = world.ID;
            }
        }

        /// <summary>
        /// Creates a new entity identifier from an entity ID and a world (parameter order reversed).
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="world">The world that owns the entity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(int id, EcsWorld world) : this(world, id) { }

        /// <summary>
        /// Creates a new entity identifier using an existing entity and a world (used for validation).
        /// </summary>
        /// <param name="world">The world to associate with.</param>
        /// <param name="entity">The source entity.</param>
        /// <remarks>In DEBUG mode, throws if worlds differ; in STABILITY_MODE, returns null if they differ.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(EcsWorld world, entlong entity) : this()
        {
#if DEBUG
            if (world.ID != entity.WorldID) { Throw.ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (world.ID != entity.WorldID) { world = null; }
#endif
            if (world == null)
            {
                _id = 0;
                _gen = 0;
                _world = 0;
            }
            else
            {
                _id = entity._id;
                _gen = entity._gen;
                _world = entity._world;
            }
        }

        /// <summary>
        /// Creates a new entity identifier from an entity and a world (parameter order reversed).
        /// </summary>
        /// <param name="entity">The source entity.</param>
        /// <param name="world">The world to associate with.</param>
        /// <remarks>In DEBUG mode, throws if worlds differ; in STABILITY_MODE, returns null if they differ.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(entlong entity, EcsWorld world) : this(world, entity) { }

        /// <summary>
        /// Creates an entity identifier from explicit ID, generation, and world ID values.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="gen">The generation number.</param>
        /// <param name="world">The world ID.</param>
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
        #endregion

        #region Unpacking Try
        /// <summary>Attempts to retrieve the entity ID. Returns true if the entity is alive; otherwise false.</summary>
        /// <param name="id">Outputs the entity ID if successful.</param>
        /// <returns>True if the entity is alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetID(out int id)
        {
            id = _id;
            return IsAlive;
        }

        /// <summary>Attempts to retrieve the owning world. Returns true if the entity is alive.</summary>
        /// <param name="world">Outputs the world instance if successful.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetWorld(out EcsWorld world)
        {
            world = EcsWorld.GetWorld(_world);
            return IsAlive;
        }

        /// <summary>Attempts to retrieve the world ID. Returns true if the entity is alive.</summary>
        /// <param name="worldID">Outputs the world ID if successful.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetWorldID(out short worldID)
        {
            worldID = _world;
            return IsAlive;
        }

        /// <summary>Attempts to retrieve the generation number. Returns true if the entity is alive.</summary>
        /// <param name="gen">Outputs the generation if successful.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGen(out short gen)
        {
            gen = _gen;
            return IsAlive;
        }

        /// <summary>Attempts to unpack the entity ID. Returns true if the entity is alive.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id)
        {
            id = _id;
            return IsAlive;
        }

        /// <summary>Attempts to unpack the entity ID and the world. Returns true if alive.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="world">Outputs the world instance.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out EcsWorld world)
        {
            world = GetWorld_Internal();
            id = _id;
            return IsAlive;
        }

        /// <summary>Attempts to unpack the entity ID, generation, and world. Returns true if alive.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <param name="world">Outputs the world instance.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out short gen, out EcsWorld world)
        {
            world = GetWorld_Internal();
            gen = _gen;
            id = _id;
            return IsAlive;
        }

        /// <summary>Attempts to unpack the entity ID and world ID. Returns true if alive.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="worldID">Outputs the world ID.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out short worldID)
        {
            worldID = _world;
            id = _id;
            return IsAlive;
        }

        /// <summary>Attempts to unpack the entity ID, generation, and world ID. Returns true if alive.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <param name="worldID">Outputs the world ID.</param>
        /// <returns>True if alive; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(out int id, out short gen, out short worldID)
        {
            worldID = _world;
            gen = _gen;
            id = _id;
            return IsAlive;
        }

        /// <summary>Attempts to unpack the entity ID using a given world. Returns true if the world matches and the entity is alive.</summary>
        /// <param name="world">The world to validate against.</param>
        /// <returns>True if alive and world matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsWorld world)
        {
            if (world.ID != _world) { return false; }
            return world.IsAlive(_id, _gen);
        }

        /// <summary>Attempts to unpack the entity ID using a given world. Returns true if valid.</summary>
        /// <param name="world">The world to validate against.</param>
        /// <param name="id">Outputs the entity ID.</param>
        /// <returns>True if alive and world matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsWorld world, out int id)
        {
            if (world.ID != _world) { id = EcsConsts.NULL_ENTITY_ID; return false; }
            id = _id;
            return world.IsAlive(_id, _gen);
        }

        /// <summary>Attempts to unpack the entity ID and generation using a given world. Returns true if valid.</summary>
        /// <param name="world">The world to validate against.</param>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <returns>True if alive and world matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsWorld world, out int id, out short gen)
        {
            if (world.ID != _world) { gen = 0; id = EcsConsts.NULL_ENTITY_ID; return false; }
            gen = _gen;
            id = _id;
            return world.IsAlive(_id, _gen);
        }

        /// <summary>Attempts to unpack the entity ID using a given mask. Returns true if the entity is alive and matches the mask.</summary>
        /// <param name="mask">The mask to validate against.</param>
        /// <param name="id">Outputs the entity ID.</param>
        /// <returns>True if alive and mask matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsMask mask, out int id)
        {
            if (mask.WorldID != _world) { id = EcsConsts.NULL_ENTITY_ID; return false; }
            id = _id;
            return mask.World.IsAlive(_id, _gen) && mask.World.IsMatchesMask(mask, _id);
        }

        /// <summary>Attempts to unpack the entity ID and generation using a given mask. Returns true if valid.</summary>
        /// <param name="mask">The mask to validate against.</param>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <returns>True if alive and mask matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsMask mask, out int id, out short gen)
        {
            if (mask.WorldID != _world) { gen = 0; id = EcsConsts.NULL_ENTITY_ID; return false; }
            gen = _gen;
            id = _id;
            return mask.World.IsAlive(_id, _gen) && mask.World.IsMatchesMask(mask, _id);
        }

        /// <summary>Attempts to unpack the entity ID using a given aspect. Returns true if the entity is alive and matches the aspect.</summary>
        /// <param name="aspect">The aspect to validate against.</param>
        /// <param name="id">Outputs the entity ID.</param>
        /// <returns>True if alive and aspect matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsAspect aspect, out int id)
        {
            if (aspect.World.ID != _world) { id = EcsConsts.NULL_ENTITY_ID; return false; }
            id = _id;
            return aspect.World.IsAlive(_id, _gen) && aspect.IsMatches(_id);
        }

        /// <summary>Attempts to unpack the entity ID and generation using a given aspect. Returns true if valid.</summary>
        /// <param name="aspect">The aspect to validate against.</param>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <returns>True if alive and aspect matches; otherwise false.</returns>
        /// <remarks>This overload is faster than the parameterless version because it skips the world-ID lookup and performs fewer internal validity checks. Use it when the world/mask/aspect instance is already available.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(EcsAspect aspect, out int id, out short gen)
        {
            if (aspect.World.ID != _world) { gen = 0; id = EcsConsts.NULL_ENTITY_ID; return false; }
            gen = _gen;
            id = _id;
            return aspect.World.IsAlive(_id, _gen) && aspect.IsMatches(_id);
        }
        #endregion

        #region Unpacking/Deconstruct
        /// <summary>Unpacks the entity ID. Throws in DEBUG if not alive; in STABILITY_MODE returns default on failure.</summary>
        /// <param name="id">Outputs the entity ID.</param>
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

        /// <summary>Unpacks the entity ID and world. Throws in DEBUG if not alive; in STABILITY_MODE returns default on failure.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="world">Outputs the world instance.</param>
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

        /// <summary>Unpacks the entity ID, generation, and world. Throws in DEBUG if not alive; in STABILITY_MODE returns default on failure.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <param name="world">Outputs the world instance.</param>
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

        /// <summary>Unpacks the entity ID and world ID. Throws in DEBUG if not alive; in STABILITY_MODE returns default on failure.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="worldID">Outputs the world ID.</param>
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

        /// <summary>Unpacks the entity ID, generation, and world ID. Throws in DEBUG if not alive; in STABILITY_MODE returns default on failure.</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <param name="worldID">Outputs the world ID.</param>
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

        /// <summary>Deconstructs the entity into ID, generation, and world ID (with validation).</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="gen">Outputs the generation.</param>
        /// <param name="worldID">Outputs the world ID.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out short gen, out short worldID)
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

        /// <summary>Deconstructs the entity into ID and world (with validation).</summary>
        /// <param name="id">Outputs the entity ID.</param>
        /// <param name="world">Outputs the world instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out EcsWorld world)
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
        #endregion

        #region Unpacking Unchecked
        /// <summary>Gets the entity ID without any validation (fast path).</summary>
        /// <returns>The raw entity ID.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIDUnchecked()
        {
            return _id;
        }

        /// <summary>Gets the world instance without any validation (fast path).</summary>
        /// <returns>The raw world instance (may be null if world ID is invalid).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorldUnchecked()
        {
            return EcsWorld.GetWorld(_world);
        }

        /// <summary>Gets the world ID without any validation (fast path).</summary>
        /// <returns>The raw world ID.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetWorldIDUnchecked()
        {
            return _world;
        }

        /// <summary>Unpacks the entity ID and world without any validation (fast path).</summary>
        /// <param name="id">Outputs the raw entity ID.</param>
        /// <param name="world">Outputs the raw world instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(_world);
            id = _id;
        }

        /// <summary>Unpacks the entity ID, generation, and world without any validation (fast path).</summary>
        /// <param name="id">Outputs the raw entity ID.</param>
        /// <param name="gen">Outputs the raw generation.</param>
        /// <param name="world">Outputs the raw world instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out short gen, out EcsWorld world)
        {
            world = EcsWorld.GetWorld(_world);
            gen = _gen;
            id = _id;
        }

        /// <summary>Unpacks the entity ID and world ID without any validation (fast path).</summary>
        /// <param name="id">Outputs the raw entity ID.</param>
        /// <param name="worldID">Outputs the raw world ID.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackUnchecked(out int id, out short worldID)
        {
            worldID = _world;
            id = _id;
        }

        /// <summary>Unpacks the entity ID, generation, and world ID without any validation (fast path).</summary>
        /// <param name="id">Outputs the raw entity ID.</param>
        /// <param name="gen">Outputs the raw generation.</param>
        /// <param name="worldID">Outputs the raw world ID.</param>
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

        /// <summary>
        /// Provides a shorthand syntax to create an <see cref="entlong"/> from an entity ID and a world,
        /// equivalent to calling the constructor <c>new entlong(world, id)</c>.
        /// </summary>
        /// <param name="a">Tuple containing entity ID and world.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator entlong((int entityID, EcsWorld world) a) { return Combine_Internal(a.entityID, a.world); }

        /// <summary>
        /// Provides a shorthand syntax to create an <see cref="entlong"/> from a world and an entity ID,
        /// equivalent to calling the constructor <c>new entlong(world, id)</c>.
        /// </summary>
        /// <param name="a">Tuple containing world and entity ID.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator entlong((EcsWorld world, int entityID) a) { return Combine_Internal(a.entityID, a.world); }

        /// <summary>
        /// Provides a shorthand syntax to obtain an <see cref="entlong"/> from an existing entity and a world,
        /// with validation that the entity belongs to the specified world.
        /// Equivalent to constructing <c>new entlong(world, entity)</c> (which validates world match).
        /// </summary>
        /// <param name="a">Tuple containing entity and world.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator entlong((entlong entity, EcsWorld world) a) { return Combine_Internal(a.entity, a.world); }

        /// <summary>
        /// Provides a shorthand syntax to obtain an <see cref="entlong"/> from a world and an existing entity,
        /// with validation that the entity belongs to the specified world.
        /// Equivalent to constructing <c>new entlong(world, entity)</c>.
        /// </summary>
        /// <param name="a">Tuple containing world and entity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator entlong((EcsWorld world, entlong entity) a) { return Combine_Internal(a.entity, a.world); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(entlong a) { return a._full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator entlong(long a) { return new entlong(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(entlong a) { return a.ID; }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static entlong Combine_Internal(int entityID, EcsWorld world)
        {
            return world == null ? new entlong(entityID, 0, 0) : world.GetEntityLong(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static entlong Combine_Internal(entlong entity, EcsWorld world)
        {
#if DEBUG
            if (world.ID != entity.WorldID) { Throw.ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (world.ID != entity.WorldID) { return default; }
#endif
            return entity;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EcsWorld GetWorld_Internal()
        {
#if DRAGONECS_STABILITY_MODE
            if (IsAlive == false) { EcsWorld.GetWorld(EcsConsts.NULL_WORLD_ID); }
#endif
            return EcsWorld.GetWorld(_world);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A hash code derived from the packed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return unchecked((int)_full) ^ (int)(_full >> 32); }

        /// <summary>Returns a string representation of the identifier and its status.</summary>
        /// <returns>A string containing ID, generation, world ID, and "null"/"alive"/"not alive".</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() { return $"entity(id:{_id} g:{_gen} w:{_world} {(IsNull ? "null" : IsAlive ? "alive" : "not alive")})"; }

        /// <summary>Determines whether this instance equals another object.</summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if obj is an <see cref="entlong"/> with the same packed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) { return obj is entlong other && _full == other._full; }

        /// <summary>Determines whether this instance equals another <see cref="entlong"/>.</summary>
        /// <param name="other">The other identifier.</param>
        /// <returns>True if packed values are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(entlong other) { return _full == other._full; }

        /// <summary>Determines whether this instance equals a 64‑bit integer.</summary>
        /// <param name="other">The packed value.</param>
        /// <returns>True if packed values are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) { return _full == other; }

        /// <summary>Compares this instance to another <see cref="entlong"/> by their ID values.</summary>
        /// <param name="other">The other identifier.</param>
        /// <returns>A value indicating the relative order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(entlong other) { return Compare(_id, other._id); }

        /// <summary>Compares two <see cref="entlong"/> values by their ID.</summary>
        /// <param name="left">First value.</param>
        /// <param name="right">Second value.</param>
        /// <returns>A value indicating the relative order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(entlong left, entlong right) { return left.CompareTo(right); }

        /// <summary>Compares two integer entity IDs.</summary>
        /// <param name="left">First ID.</param>
        /// <param name="right">Second ID.</param>
        /// <returns>left - right (fast, no overflow check).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(int left, int right)
        {
            // NOTE: Because _id cannot be less than 0,
            // the case “_id - other._id > MaxValue” is impossible.
            return unchecked(left - right);
        }

        internal class DebuggerProxy : EntityDebuggerProxy
        {
            public override long full => base.full;
            public override int id => base.id;
            public override short gen => base.gen;
            public override short worldID => base.worldID;
            public override RawEntLong.StateFlag State => base.State;
            public override EcsWorld World => base.World;
            public override IEnumerable<object> Components { get => base.Components; set => base.Components = value; }
            public DebuggerProxy(entlong entity) : base(entity) { }
        }
        #endregion
    }
}
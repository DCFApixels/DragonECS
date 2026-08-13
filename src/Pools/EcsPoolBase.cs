#if DISABLE_DEBUG
#undef DEBUG
#endif
#if !DRAGONECS_DISABLE_POOLS_EVENTS
#define DRAGONECS_ENABLE_POOLS_EVENTS
#else
#undef DRAGONECS_ENABLE_POOLS_EVENTS
#endif

using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core
{
    /// <summary>
    /// Only used to implement a custom pool. In other contexts use IEcsPool or IEcsPool<T>.
    /// </summary>
    public interface IEcsPoolImplementation : IEcsPool
    {
        #region Methods
        /// <summary>
        /// Initialize pool implementation with a registrar for world and component id.
        /// </summary>
        /// <param name="registrar">Registrar bound to the target world and component id.</param>
        void OnInit(EcsWorld.ComponentsRegistrar registrar);
        /// <summary>
        /// Notify pool that world entity capacity has changed.
        /// </summary>
        /// <param name="newSize">New world capacity (number of entity slots).</param>
        void OnWorldResize(int newSize);
        /// <summary>
        /// Called when a deferred-delete buffer is released so the pool can remove components for affected entities.
        /// </summary>
        /// <param name="buffer">Span of entity ids to process.</param>
        void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer);
        /// <summary>
        /// Called when the owning world is being destroyed so the pool can free resources.
        /// </summary>
        void OnWorldDestroy();
        /// <summary>
        /// Debug-only notification that the pool lock state changed.
        /// </summary>
        /// <param name="locked">True when pool is locked.</param>
        void OnLockedChanged_Debug(bool locked);
        #endregion
    }

    /// <summary> 
    /// Only used to implement a custom pool. In other contexts use IEcsPool or IEcsPool<T>.
    /// </summary>
    /// <typeparam name="T"> Component type. </typeparam>
    public interface IEcsPoolImplementation<T> : IEcsPoolImplementation { }

    #region EcsPoolThrowHelper
    public static class EcsPoolThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowDifferentTypes()
        {
            Throw.Pool_DifferentComponentTypes();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAlreadyHasComponent<T>(int entityID)
        {
            Throw.Pool_AlreadyHasComponent<T>(entityID);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotHaveComponent<T>(int entityID)
        {
            Throw.Pool_DoesNotHaveComponent<T>(entityID);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowEntityIsNotAlive(EcsWorld world, int entityID)
        {
            Throw.Ent_ThrowIsNotAlive((world, entityID));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAlreadyHasComponent(Type type, int entityID)
        {
            Throw.Pool_AlreadyHasComponent(type, entityID);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotHaveComponent(Type type, int entityID)
        {
            Throw.Pool_DoesNotHaveComponent(type, entityID);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNullListener()
        {
            Throw.ArgumentNull("listener", "Listener cannot be null.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNullComponent()
        {
            Throw.ArgumentNull("component", "Component cannot be null.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowPoolLocked()
        {
            Throw.Pool_IsLocked();
        }
    }
    #endregion
}

namespace DCFApixels.DragonECS.Core.Internal
{
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "A placeholder type, an instance of this type replaces the null ref.")]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaID("DragonECS_460E547C9201227A4956AC297F67B484")]
    [DebuggerDisplay("-")]
    public sealed class EcsNullPool : IEcsPoolImplementation<NullComponent>
    {
        public static readonly EcsNullPool instance = new EcsNullPool();

        #region Properties
        int IEcsReadonlyPool.ComponentTypeID { get { return 0; } }
        Type IEcsReadonlyPool.ComponentType { get { return typeof(NullComponent); } }
        EcsWorld IEcsReadonlyPool.World
        {
            get
            {
#if DEBUG
                Throw.NullInstanceException("Attempted to access World on EcsNullPool placeholder instance.");
                return EcsWorld.GetWorld(0);
#else
                return EcsWorld.GetWorld(0);
#endif
            }
        }
        public int Count { get { return 0; } }
        public bool IsReadOnly { get { return true; } }
        #endregion

        #region Methods
        bool IEcsReadonlyPool.Has(int index)
        {
            return false;
        }
        void IEcsPool.Del(int entityID)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to delete a component from EcsNullPool placeholder instance.");
#endif
        }
        void IEcsPool.AddEmpty(int entityID)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to add an empty component to EcsNullPool placeholder instance.");
#endif
        }
        void IEcsPool.AddRaw(int entityID, object dataRaw)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to add raw component data to EcsNullPool placeholder instance.");
#endif
        }
        object IEcsReadonlyPool.GetRaw(int entityID)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to get raw component data from EcsNullPool placeholder instance.");
            return null;
#else
            return null;
#endif
        }
        void IEcsPool.SetRaw(int entity, object dataRaw)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to set raw component data on EcsNullPool placeholder instance.");
#endif
        }
        void IEcsPool.Copy(int fromEntityID, int toEntityID)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to copy a component from EcsNullPool placeholder instance.");
#endif
        }
        void IEcsPool.Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to copy a component from EcsNullPool placeholder instance to another world.");
#endif
        }
        void IEcsPool.ClearAll()
        {
#if DEBUG
            Throw.NullInstanceException("Attempted to clear EcsNullPool placeholder instance.");
#endif
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld.ComponentsRegistrar registrar) { }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        void IEcsPoolImplementation.OnWorldResize(int newSize) { }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsPoolImplementation.OnLockedChanged_Debug(bool locked) { }
        #endregion

        #region Listeners
#if !DRAGONECS_DISABLE_POOLS_EVENTS
        void IEcsReadonlyPool.AddListener(IEcsPoolEventListener listener) { }
        void IEcsReadonlyPool.RemoveListener(IEcsPoolEventListener listener) { }
#endif
        #endregion
    }
    public struct NullComponent { }
}

namespace DCFApixels.DragonECS
{
    #region Interfaces
    /// <summary>
    /// Public read‑only interface for a component pool.
    /// Provides basic methods and properties for checking component presence and accessing raw data.
    /// </summary>
    public interface IEcsReadonlyPool : IEcsMember
    {
        #region Properties
        /// <summary>
        /// Internal component type identifier for this pool.
        /// </summary>
        int ComponentTypeID { get; }

        /// <summary>
        /// Type of the component stored in this pool.
        /// </summary>
        Type ComponentType { get; }

        /// <summary>
        /// The world instance that owns this pool.
        /// </summary>
        EcsWorld World { get; }

        /// <summary>
        /// Number of components stored in the pool.
        /// </summary>
        int Count { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Check whether the specified entity has a component in this pool.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>True when the component is present.</returns>
        bool Has(int entityID);
        /// <summary>
        /// Returns a raw object of the component for the specified entity. Note that value types are boxed.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Raw component object.</returns>
        object GetRaw(int entityID);
        #endregion

#if !DRAGONECS_DISABLE_POOLS_EVENTS

        #region Add/Remove Listeners
        void AddListener(IEcsPoolEventListener listener);
        void RemoveListener(IEcsPoolEventListener listener);
        #endregion
#endif
    }

    public interface IEcsPool : IEcsReadonlyPool
    {
        #region Methods
        /// <summary>
        /// Add an empty component placeholder for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        void AddEmpty(int entityID);
        /// <summary>
        /// Add raw component data for the specified entity (object boxed or reference expected by pool implementation).
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <param name="dataRaw">Raw component data.</param>
        void AddRaw(int entityID, object dataRaw);
        /// <summary>
        /// Set raw component data for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <param name="dataRaw">Raw component data.</param>
        void SetRaw(int entityID, object dataRaw);
        /// <summary>
        /// Remove component for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        void Del(int entityID);
        /// <summary>
        /// Remove all components from this pool and unregister them from the world.
        /// </summary>
        void ClearAll();
        /// <summary>
        /// Copy component data from one entity to another inside the same world.
        /// </summary>
        /// <param name="fromEntityID">Source entity identifier.</param>
        /// <param name="toEntityID">Destination entity identifier.</param>
        void Copy(int fromEntityID, int toEntityID);
        /// <summary>
        /// Copy component data from one entity in this world to another entity in a different world.
        /// </summary>
        /// <param name="fromEntityID">Source entity identifier.</param>
        /// <param name="toWorld">Destination world.</param>
        /// <param name="toEntityID">Destination entity identifier.</param>
        void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID);
        #endregion
    }

    /// <summary> A pool for struct components. </summary>
    public interface IEcsStructPool<T> : IEcsPool where T : struct
    {
        #region Methods
        ref T Add(int entityID);
        ref readonly T Read(int entityID);
        ref T Get(int entityID);
        #endregion
    }

    /// <summary> A pool for reference components of type T that instantiates components itself. </summary>
    public interface IEcsClassPool<T> : IEcsPool where T : class
    {
        #region Methods
        T Add(int entityID);
        T Get(int entityID);
        #endregion
    }

    /// <summary> A pool for reference components of type T, which does not instantiate components itself but receives components from external sources. </summary>
    public interface IEcsHybridPool<T> : IEcsPool where T : class
    {
        #region Methods
        void Add(int entityID, T component);
        T Get(int entityID);
        #endregion
    }
    #endregion

    #region Extensions
    public static class IEcsPoolImplementationExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDummy(this IEcsPool self)
        {
            return self == null || self == EcsNullPool.instance;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NewEntity<T>(this IEcsStructPool<T> self) where T : struct
        {
            var e = self.World.NewEntity();
            return ref self.Add(e);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NewEntity<T>(this IEcsStructPool<T> self, out int entityID) where T : struct
        {
            entityID = self.World.NewEntity();
            return ref self.Add(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NewEntityLong<T>(this IEcsStructPool<T> self, out entlong entity) where T : struct
        {
            entity = self.World.NewEntityLong();
            return ref self.Add(entity.GetIDUnchecked());
        }
    }
    #endregion

    #region Callbacks Interface
    public interface IEcsPoolEventListener
    {
        /// <summary>Called after adding an entity to the pool, but before changing values</summary>
        void OnAdd(int entityID);
        /// <summary>Is called when EcsPool.Get or EcsPool.Add is called, but before changing values</summary>
        void OnGet(int entityID);
        /// <summary>Called after deleting an entity from the pool</summary>
        void OnDel(int entityID);
    }
    public static class PoolEventListExtensions
    {
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnAdd(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++) { self[i].OnAdd(entityID); }
        }
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnAddAndGet(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++)
            {
                self[i].OnAdd(entityID);
                self[i].OnGet(entityID);
            }
        }
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnGet(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++) { self[i].OnGet(entityID); }
        }
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDel(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++) { self[i].OnDel(entityID); }
        }

        //


        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeOnAdd(this StructList<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++) { self[i].OnAdd(entityID); }
        }
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeOnAddAndGet(this StructList<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++)
            {
                self[i].OnAdd(entityID);
                self[i].OnGet(entityID);
            }
        }
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeOnGet(this StructList<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++) { self[i].OnGet(entityID); }
        }
        [Conditional("DRAGONECS_ENABLE_POOLS_EVENTS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeOnDel(this StructList<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0; i < self.Count; i++) { self[i].OnDel(entityID); }
        }
    }
    #endregion
}

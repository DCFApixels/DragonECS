using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsReadonlyPool
    {
        #region Properties
        int ComponentTypeID { get; }
        Type ComponentType { get; }
        EcsWorld World { get; }
        int Count { get; }
        bool IsReadOnly { get; }
        #endregion

        #region Methods
        bool Has(int entityID);
        object GetRaw(int entityID);
        void Copy(int fromEntityID, int toEntityID);
        void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID);
        #endregion

        #region Add/Remove Listeners
        void AddListener(IEcsPoolEventListener listener);
        void RemoveListener(IEcsPoolEventListener listener);
        #endregion
    }
    public interface IEcsPool : IEcsReadonlyPool
    {
        #region Methods
        void AddRaw(int entityID, object dataRaw);
        void SetRaw(int entityID, object dataRaw);
        void Del(int entityID);
        void ClearAll();
        #endregion
    }
    /// <summary>A pool for struct components.</summary>
    public interface IEcsStructPool<T> : IEcsPool where T : struct
    {
        ref T Add(int entityID);
        ref readonly T Read(int entityID);
        ref T Get(int entityID);
    }
    /// <summary>A pool for reference components of type T that instantiates components itself.</summary>
    public interface IEcsClassPool<T> : IEcsPool where T : class
    {
        T Add(int entityID);
        T Get(int entityID);
    }
    /// <summary>A pool for reference components of type T, which does not instantiate components itself but receives components from external sources..</summary>
    public interface IEcsHybridPool<T> : IEcsPool where T : class
    {
        void Add(int entityID, T component);
        T Get(int entityID);
    }
    /// <summary>Only used to implement a custom pool. In other contexts use IEcsPool or IEcsPool<T>.</summary>
    public interface IEcsPoolImplementation : IEcsPool
    {
        void OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID);
        void OnWorldResize(int newSize);
        void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer);
        void OnWorldDestroy();
    }
    /// <summary>Only used to implement a custom pool. In other contexts use IEcsPool or IEcsPool<T>.</summary>
    /// <typeparam name="T">Component type</typeparam>
    public interface IEcsPoolImplementation<T> : IEcsPoolImplementation { }

    public static class EcsPoolThrowHalper
    {
        public static void ThrowAlreadyHasComponent<T>(int entityID)
        {
            throw new EcsFrameworkException($"Entity({entityID}) already has component {EcsDebugUtility.GetGenericTypeName<T>()}.");
        }
        public static void ThrowNotHaveComponent<T>(int entityID)
        {
            throw new EcsFrameworkException($"Entity({entityID}) has no component {EcsDebugUtility.GetGenericTypeName<T>()}.");
        }
        public static void ThrowAlreadyHasComponent(Type type, int entityID)
        {
            throw new EcsFrameworkException($"Entity({entityID}) already has component {EcsDebugUtility.GetGenericTypeName(type)}.");
        }
        public static void ThrowNotHaveComponent(Type type, int entityID)
        {
            throw new EcsFrameworkException($"Entity({entityID}) has no component {EcsDebugUtility.GetGenericTypeName(type)}.");
        }
    }
    public static class IEcsPoolImplementationExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDummy(this IEcsPool self)
        {
            return self == null || self == EcsNullPool.instance;
        }
    }

    #region Dummy EcsNullPool
    namespace Internal
    {
        public struct NullComponent { }
        public sealed class EcsNullPool : IEcsPoolImplementation<NullComponent>
        {
            public static readonly EcsNullPool instance = new EcsNullPool();

            #region Properties
            int IEcsReadonlyPool.ComponentTypeID { get { return 0; } }//TODO Првоерить что NullComponent всегда имеет id 0 
            Type IEcsReadonlyPool.ComponentType { get { return typeof(NullComponent); } }
            EcsWorld IEcsReadonlyPool.World
            {
                get
                {
#if (DEBUG && !DISABLE_DEBUG)
                    throw new NullInstanceException();
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
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#endif
            }
            void IEcsPool.AddRaw(int entityID, object dataRaw)
            {
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#endif
            }
            object IEcsReadonlyPool.GetRaw(int entityID)
            {
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#else
                return null;
#endif
            }
            void IEcsPool.SetRaw(int entity, object dataRaw)
            {
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#endif
            }
            void IEcsReadonlyPool.Copy(int fromEntityID, int toEntityID)
            {
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#endif
            }
            void IEcsReadonlyPool.Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
            {
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#endif
            }
            void IEcsPool.ClearAll()
            {
#if (DEBUG && !DISABLE_DEBUG)
                throw new NullInstanceException();
#endif
            }
            #endregion

            #region Callbacks
            void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID) { }
            void IEcsPoolImplementation.OnWorldDestroy() { }
            void IEcsPoolImplementation.OnWorldResize(int newSize) { }
            void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
            #endregion

            #region Listeners
            void IEcsReadonlyPool.AddListener(IEcsPoolEventListener listener) { }
            void IEcsReadonlyPool.RemoveListener(IEcsPoolEventListener listener) { }
            #endregion
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnAdd(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnAdd(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnAddAndGet(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnAdd(entityID);
                self[i].OnGet(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnGet(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnGet(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDel(this List<IEcsPoolEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnDel(entityID);
        }
    }
    #endregion
}

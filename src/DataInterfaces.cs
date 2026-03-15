#if DISABLE_DEBUG
#undef DEBUG
#endif
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core
{
    #region IEcsWorldComponent
    public interface IEcsWorldComponent<T>
    {
        void Init(ref T component, EcsWorld world);
        void OnDestroy(ref T component, EcsWorld world);
    }
    public static class EcsWorldComponent<T> where T : struct
    {
        public static readonly IEcsWorldComponent<T> CustomHandler;
        public static readonly bool IsCustom;
        static EcsWorldComponent()
        {
            T raw = default;
            if (raw is IEcsWorldComponent<T> handler)
            {
                IsCustom = true;
                CustomHandler = handler;
            }
            else
            {
                IsCustom = false;
                CustomHandler = new DummyHandler();
            }
        }
        private sealed class DummyHandler : IEcsWorldComponent<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Init(ref T component, EcsWorld world) { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnDestroy(ref T component, EcsWorld world) { }
        }
    }
    #endregion

    #region IEcsComponentLifecycle
    public interface IEcsComponentLifecycle<T>
    {
        void OnAdd(ref T component, short worldID, int entityID);
        void OnDel(ref T component, short worldID, int entityID);
    }
    public static class EcsComponentLifecycle<T> where T : struct
    {
        public static readonly IEcsComponentLifecycle<T> CustomHandler;
        public static readonly bool IsCustom;
        static EcsComponentLifecycle()
        {
            T raw = default;
            if (raw is IEcsComponentLifecycle<T> handler)
            {
                IsCustom = true;
                CustomHandler = handler;
            }
            else
            {
                IsCustom = false;
                CustomHandler = new DummyHandler();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnAdd(bool isCustom, IEcsComponentLifecycle<T> custom, ref T component, short worldID, int entityID)
        {
            if (isCustom)
            {
                custom.OnAdd(ref component, worldID, entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDel(bool isCustom, IEcsComponentLifecycle<T> custom, ref T component, short worldID, int entityID)
        {
            if (isCustom)
            {
                custom.OnDel(ref component, worldID, entityID);
            }
            else
            {
                component = default;
            }
        }
        private sealed class DummyHandler : IEcsComponentLifecycle<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnAdd(ref T component, short worldID, int entityID) { component = default; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnDel(ref T component, short worldID, int entityID) { component = default; }
        }
    }
    #endregion

    #region IEcsComponentCopy
    public interface IEcsComponentCopy<T>
    {
        void Copy(ref T from, ref T to);
    }
    public static class EcsComponentCopy<T> where T : struct
    {
        public static readonly IEcsComponentCopy<T> CustomHandler;
        public static readonly bool IsCustom;
        static EcsComponentCopy()
        {
            T raw = default;
            if (raw is IEcsComponentCopy<T> handler)
            {
                IsCustom = true;
                CustomHandler = handler;
            }
            else
            {
                IsCustom = false;
                CustomHandler = new DummyHandler();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(bool isCustom, IEcsComponentCopy<T> custom, ref T from, ref T to)
        {
            if (isCustom)
            {
                custom.Copy(ref from, ref to);
            }
            else
            {
                to = from;
            }
        }
        private sealed class DummyHandler : IEcsComponentCopy<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Copy(ref T from, ref T to) { to = from; }
        }
    }
    #endregion
}
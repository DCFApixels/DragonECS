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
    public static class EcsWorldComponentHandler<T>
    {
        public static readonly IEcsWorldComponent<T> instance;
        public static readonly bool isHasHandler;
        static EcsWorldComponentHandler()
        {
            T def = default;
            if (def is IEcsWorldComponent<T> intrf)
            {
                isHasHandler = true;
                instance = intrf;
            }
            else
            {
                isHasHandler = false;
                instance = new DummyHandler();
            }
        }
        private class DummyHandler : IEcsWorldComponent<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Init(ref T component, EcsWorld world) { }
            public void OnDestroy(ref T component, EcsWorld world) { }
        }
    }
    #endregion

    #region IEcsComponentReset
    public interface IEcsComponentLifecycle<T>
    {
        void Enable(ref T component);
        void Disable(ref T component);
    }
    public static class EcsComponentLifecycleHandler<T>
    {
        public static readonly IEcsComponentLifecycle<T> instance;
        public static readonly bool isHasHandler;
        static EcsComponentLifecycleHandler()
        {
            T def = default;
            if (def is IEcsComponentLifecycle<T> intrf)
            {
                isHasHandler = true;
                instance = intrf;
            }
            else
            {
                isHasHandler = false;
                instance = new DummyHandler();
            }
        }
        private sealed class DummyHandler : IEcsComponentLifecycle<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Enable(ref T component) { component = default; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Disable(ref T component) { component = default; }
        }
    }
    #endregion

    #region IEcsComponentCopy
    public interface IEcsComponentCopy<T>
    {
        void Copy(ref T from, ref T to);
    }
    public static class EcsComponentCopyHandler<T>
    {
        public static readonly IEcsComponentCopy<T> instance;
        public static readonly bool isHasHandler;
        static EcsComponentCopyHandler()
        {
            T def = default;
            if (def is IEcsComponentCopy<T> intrf)
            {
                isHasHandler = true;
                instance = intrf;
            }
            else
            {
                isHasHandler = false;
                instance = new DummyHandler();
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

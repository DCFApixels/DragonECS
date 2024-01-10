using System;
using System.Linq;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.Internal.DataInterfaceHalper;

namespace DCFApixels.DragonECS.Internal
{
    #region DataInterfaceHalper
    public static class DataInterfaceHalper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckFakeInstanceValide<T>(T fakeInstnace)
        {
#if DEBUG
            T nil = default;
            if (fakeInstnace.Equals(nil) == false)
            {
                throw new Exception("Не правильное применение интерфейса, менять нужно передаваемое по ref значение");
            }
#endif
        }
    }
    #endregion
}
namespace DCFApixels.DragonECS
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
            Type targetType = typeof(T);
            isHasHandler = targetType.GetInterfaces().Contains(typeof(IEcsWorldComponent<>).MakeGenericType(targetType));
            if (isHasHandler)
            {
                instance = (IEcsWorldComponent<T>)Activator.CreateInstance(typeof(WorldComponentHandler<>).MakeGenericType(targetType));
            }
            else
            {
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
    internal sealed class WorldComponentHandler<T> : IEcsWorldComponent<T>
        where T : struct, IEcsWorldComponent<T>
    {
        private T _fakeInstnace = default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init(ref T component, EcsWorld world)
        {
            _fakeInstnace.Init(ref component, world);
            CheckFakeInstanceValide(_fakeInstnace);
        }
        public void OnDestroy(ref T component, EcsWorld world)
        {
            _fakeInstnace.OnDestroy(ref component, world);
            CheckFakeInstanceValide(_fakeInstnace);
        }

    }
    #endregion

    #region IEcsComponentReset
    public interface IEcsComponentReset<T>
    {
        void Reset(ref T component);
    }
    public static class EcsComponentResetHandler<T>
    {
        public static readonly IEcsComponentReset<T> instance;
        public static readonly bool isHasHandler;
        static EcsComponentResetHandler()
        {
            Type targetType = typeof(T);
            isHasHandler = targetType.GetInterfaces().Contains(typeof(IEcsComponentReset<>).MakeGenericType(targetType));
            if (isHasHandler)
            {
                instance = (IEcsComponentReset<T>)Activator.CreateInstance(typeof(ComponentResetHandler<>).MakeGenericType(targetType));
            }
            else
            {
                instance = new DummyHandler();
            }
        }
        private sealed class DummyHandler : IEcsComponentReset<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset(ref T component) => component = default;
        }
    }
    internal sealed class ComponentResetHandler<T> : IEcsComponentReset<T>
        where T : IEcsComponentReset<T>
    {
        private T _fakeInstnace = default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ref T component)
        {
            _fakeInstnace.Reset(ref component);
            CheckFakeInstanceValide(_fakeInstnace);
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
            Type targetType = typeof(T);
            isHasHandler = targetType.GetInterfaces().Contains(typeof(IEcsComponentCopy<>).MakeGenericType(targetType));
            if (isHasHandler)
            {
                instance = (IEcsComponentCopy<T>)Activator.CreateInstance(typeof(ComponentCopyHandler<>).MakeGenericType(targetType));
            }
            else
            {
                instance = new DummyHandler();
            }
        }
        private sealed class DummyHandler : IEcsComponentCopy<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Copy(ref T from, ref T to) => to = from;
        }
    }
    internal sealed class ComponentCopyHandler<T> : IEcsComponentCopy<T>
        where T : IEcsComponentCopy<T>
    {
        private T _fakeInstnace = default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Copy(ref T from, ref T to)
        {
            _fakeInstnace.Copy(ref from, ref to);
            CheckFakeInstanceValide(_fakeInstnace);
        }
    }
    #endregion
}

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsComponentReset<T>
    {
        public void Reset(ref T component);
    }
    public static class EcsComponentResetHandler<T>
    {
        public static readonly IEcsComponentReset<T> instance;
        public static readonly bool isHasHandler;
        static EcsComponentResetHandler()
        {
            Type targetType = typeof(T);
            if (targetType.GetInterfaces().Contains(typeof(IEcsComponentReset<>).MakeGenericType(targetType)))
            {
                instance = (IEcsComponentReset<T>)Activator.CreateInstance(typeof(ComponentResetHandler<>).MakeGenericType(targetType));
                isHasHandler = true;
            }
            else
            {
                instance = new ComponentResetDummyHandler<T>();
                isHasHandler = false;
            }
        }
    }
    internal sealed class ComponentResetDummyHandler<T> : IEcsComponentReset<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ref T component) => component = default;
    }
    internal sealed class ComponentResetHandler<T> : IEcsComponentReset<T>
        where T : IEcsComponentReset<T>
    {
        private T _fakeInstnace = default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ref T component) => _fakeInstnace.Reset(ref component);
    }
}

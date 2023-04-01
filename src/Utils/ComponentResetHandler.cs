using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    internal static class ComponentResetHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEcsComponentReset<T> New<T>()
        {
            Type targetType = typeof(T);
            if (targetType.GetInterfaces().Contains(typeof(IEcsComponentReset<>).MakeGenericType(targetType)))
            {
                return (IEcsComponentReset<T>)Activator.CreateInstance(typeof(ComponentResetHandler<>).MakeGenericType(targetType));
            }
            return (IEcsComponentReset<T>)Activator.CreateInstance(typeof(ComponentResetDummy<>).MakeGenericType(targetType));
        }
    }
    internal sealed class ComponentResetDummy<T> : IEcsComponentReset<T>
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

using System.Runtime.CompilerServices;
using System;
using System.Linq;

namespace DCFApixels.DragonECS
{
    public interface IEcsComponentReset<T>
    {
        public void Reset(ref T component);


        private static IEcsComponentReset<T> _handler;
        public static IEcsComponentReset<T> Handler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if(_handler == null)
                {
                    Type targetType = typeof(T);
                    if (targetType.GetInterfaces().Contains(typeof(IEcsComponentReset<>).MakeGenericType(targetType)))
                        _handler = (IEcsComponentReset<T>)Activator.CreateInstance(typeof(ComponentResetHandler<>).MakeGenericType(targetType));
                    else
                        _handler = new ComponentResetDummy<T>();
                }
                return _handler;
            }
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

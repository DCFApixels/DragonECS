using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsPool
    {
        public IEcsWorld World { get; }
        public int ID { get; }
        public bool Has(int index);
        public void Write(int index);
        public void Del(int index);
    }

    public interface IEcsPool<T> : IEcsPool
        where T : struct
    {
        public ref readonly T Read(int entity);
        public new ref T Write(int entity);
    }

    public class EcsNullPool : IEcsPool
    {
        private readonly IEcsWorld _source;

        public EcsNullPool(IEcsWorld source)
        {
            _source = source;
        }

        public IEcsWorld World => _source;
        public int ID => -1;
        public void Del(int index) { }
        public bool Has(int index) => false;
        public void Write(int index) { }
    }
    
    public class EcsPool<T> : IEcsPool<T>
        where T : struct
    {
        private readonly int _id;
        private readonly IEcsWorld _source;
        private readonly SparseSet _sparseSet;
        private T[] _denseItems;

        private IEcsComponentReset<T> _componentResetHandler;

        #region Properites
        public IEcsWorld World => _source;
        public int ID => _id;
        #endregion

        #region Constructors
        public EcsPool(IEcsWorld source, int id, int capacity)
        {
            _source = source;
            _id = id;
            _sparseSet = new SparseSet(capacity, capacity);

            _denseItems =new T[capacity];

            _componentResetHandler = ComponentResetHandler.New<T>();
        }
        #endregion

        #region Read/Write/Has/Del
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entity)
        {
            return ref _denseItems[_sparseSet.IndexOf(entity)];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int entity)
        {
            if (!_sparseSet.Contains(entity))
            {
                _sparseSet.Add(entity);
                _sparseSet.Normalize(ref _denseItems);
                _source.OnEntityComponentAdded(entity, _id);
                _componentResetHandler.Reset(ref _denseItems[_sparseSet.IndexOf(entity)]);
            }
            return ref _denseItems[_sparseSet.IndexOf(entity)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entity)
        {
            return _sparseSet.IndexOf(entity) >= 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entity)
        {
            _sparseSet.RemoveAt(entity);
            _source.OnEntityComponentRemoved(entity, _id);
        }
        #endregion

        #region IEcsPool
        void IEcsPool.Write(int index)
        {
            Write(index);
        }
        #endregion

        #region Equals/GetHashCode
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode() => _source.GetHashCode() + ID;
        #endregion
    }

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

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

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
            if (_sparseSet.Contains(entity))
            {
                return ref _denseItems[_sparseSet.IndexOf(entity)];
            }
            else
            {
                _sparseSet.Add(entity);
                _sparseSet.Normalize(ref _denseItems);
                _source.OnEntityComponentAdded(entity, _id);
                int indexof = _sparseSet.IndexOf(entity);
                return ref _denseItems[indexof];
            }
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
}

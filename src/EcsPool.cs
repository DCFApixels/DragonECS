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
        public EcsWorld World { get; }
        public int ID { get; }
        public bool Has(int index);
        public void Add(int index);
        public void Del(int index);
    }

    public class EcsPool<T> : IEcsPool
        where T : struct
    {
        private readonly int _id;
        private readonly EcsWorld _source;
        private readonly SparseSet _sparseSet;
        private T[] _denseItems;

        #region Properites
        public EcsWorld World => _source;
        public int ID => _id;
        #endregion

        #region Constructors
        public EcsPool(EcsWorld source, int capacity)
        {
            _source = source;
            _sparseSet = new SparseSet(capacity);

            _denseItems =new T[capacity];
        }
        #endregion

        #region Read/Write/Has/Del
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int index)
        {
            return ref _denseItems[_sparseSet[index]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int index)
        {
            return ref _denseItems[_sparseSet[index]];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int index)
        {
            _sparseSet.Add(index);
            _sparseSet.Normalize(ref _denseItems);
            return ref _denseItems[_sparseSet.IndexOf(index)];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int index)
        {
            return _sparseSet.Contains(index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int index)
        {
            _sparseSet.Remove(index);
        }
        #endregion

        #region IEcsFieldPool
        void IEcsPool.Add(int index)
        {
            Add(index);
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

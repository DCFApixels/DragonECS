using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public interface IEcsFieldPool
    {
        public bool Has(int index);
        public void Add(int index);
    }
    public class EcsFieldPool<T> : IEcsFieldPool
    {
        private SparseSet _sparseSet;
        private T[] _denseItems;

        public EcsFieldPool(int capacity)
        {
            _denseItems = new T[capacity];
            _sparseSet = new SparseSet(capacity);
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _denseItems[_sparseSet[index]];
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

        #region IEcsFieldPool
        void IEcsFieldPool.Add(int index)
        {
            Add(index);
        }
        #endregion
    }
}

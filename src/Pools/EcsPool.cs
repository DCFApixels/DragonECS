using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class EcsPool<T> : IEcsPool
    {
        private int _id;
        private readonly EcsWorld _source;
        private readonly EcsType _type;
        private readonly SparseSet _sparseSet;
        private T[] _denseItems;

        #region Properites
        public EcsWorld World => _source;
        public int ID => _id;
        public EcsType Type => _type;
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _denseItems[_sparseSet[index]];
        }

        #endregion

        #region Constructors
        public EcsPool(EcsWorld source, EcsType type, int capacity)
        {
            _source = source;
            _type = type;
            _denseItems = new T[capacity];
            _sparseSet = new SparseSet(capacity);
        }
        #endregion

        #region Add/Has/Get/Del
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
    }
}

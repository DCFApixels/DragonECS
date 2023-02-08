using System.Collections;
using System.Collections.Generic;
using DCFApixels.DragonECS.Reflection;
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
        public EcsMemberBase Type { get; }
        public bool IsTagsPool { get; }
        public bool Has(int index);
        public void Add(int index);
        public void Del(int index);
    }

    public class EcsPool<T> : IEcsPool
        where T : struct
    {
        private int _id;
        private readonly EcsWorld _source;
        private readonly EcsMember<T> _type;
        private readonly SparseSet _sparseSet;
        private T[] _denseItems;

        private int _isTagsPoolMask;

        #region Properites
        public EcsWorld World => _source;
        public EcsMemberBase Type => _type;

        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _id;
        }
        public bool IsTagsPool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isTagsPoolMask < 0;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _denseItems[_sparseSet[index] | _isTagsPoolMask];
        }
        #endregion

        #region Constructors
        public EcsPool(EcsWorld source, mem<T> type, int capacity)
        {
            _source = source;
            _type = MemberDeclarator.GetMemberInfo(type);
            _sparseSet = new SparseSet(capacity);

            _isTagsPoolMask = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length <= 0 ? -1 : 0;

            _denseItems = IsTagsPool ? new T[1] : new T[capacity];
        }
        #endregion

        #region Add/Has/Get/Del
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int index)
        {
            _sparseSet.Add(index);
            if(IsTagsPool)
            {
                _sparseSet.Normalize(ref _denseItems);
                return ref _denseItems[_sparseSet.IndexOf(index)];
            }
            return ref _denseItems[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int index)
        {
            return _sparseSet.Contains(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int index)
        {
            if (!IsTagsPool) { this[index] = default; }
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
        public override int GetHashCode() => _type.GetHashCode();
        #endregion
    }
}

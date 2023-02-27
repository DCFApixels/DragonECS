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
        public void Write(int index);
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
            if (_sparseSet.Contains(index))
            {
                return ref _denseItems[_sparseSet[index]];
            }
            else
            {
                _sparseSet.Add(index);
                _sparseSet.Normalize(ref _denseItems);
                return ref _denseItems[_sparseSet.IndexOf(index)];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int index)
        {
            return _sparseSet.IndexOf(index) > 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int index)
        {
            _sparseSet.RemoveAt(index);
        }
        #endregion

        #region IEcsFieldPool
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

    public static partial class EntityExtensions
    {
        public static ref readonly T Read<T>(this in Entity self)
            where T : struct
        {
            return ref self.world.GetPool<T>().Read(self.id);
        }
        public static ref T Write<T>(this in Entity self)
            where T : struct
        {
            return ref self.world.GetPool<T>().Write(self.id);
        }
        public static bool Has<T>(this in Entity self)
            where T : struct
        {
            return self.world.GetPool<T>().Has(self.id);
        }
        public static void Del<T>(this in Entity self)
            where T : struct
        {
            self.world.GetPool<T>().Del(self.id);
        }
    }
}

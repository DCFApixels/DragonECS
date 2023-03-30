using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using delayedOp = System.Int32;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly ref struct EcsReadonlyGroup
    {
        private readonly EcsGroup _source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source) => _source = source;
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.Count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int entityID) => _source.Contains(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => _source.GetEnumerator();
    }

    // не может содержать значение 0
    // _delayedOps это int[] для отложенных операций, хранятся отложенные операции в виде int значения, если старший бит = 0 то это опреация добавленияб если = 1 то это операция вычитания

    // this collection can only store numbers greater than 0
    public class EcsGroup  
    {
        public const int DEALAYED_ADD = 0;
        public const int DEALAYED_REMOVE = int.MinValue;

        private IEcsWorld _source;

        private int[] _dense;
        private int[] _sparse;

        private int _count;

        private delayedOp[] _delayedOps;
        private int _delayedOpsCount;

        private int _lockCount;

        #region Properties
        public IEcsWorld World => _source;
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
        public EcsReadonlyGroup Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new EcsReadonlyGroup(this);
        }
        #endregion

        #region Constrcutors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup(IEcsWorld source, int denseCapacity = 64, int sparseCapacity = 256, int delayedOpsCapacity = 128)
        {
            _source = source;
            _dense = new int[denseCapacity];
            _sparse = new int[sparseCapacity];

            _delayedOps = new delayedOp[delayedOpsCapacity];

            _lockCount = 0;
            _delayedOpsCount = 0;

            _count = 0;
        }
        #endregion

        #region Contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int entityID)
        {
            return /*entityID > 0 && */ entityID < _sparse.Length && _sparse[entityID] > 0;
        }
        #endregion

        #region add/remove
        public void Add(int entityID)
        {
            if (_lockCount > 0) 
            {
                AddDelayedOp(entityID, DEALAYED_ADD);
                return;
            }

            if (Contains(entityID)) 
                return;

            if(++_count >= _dense.Length) 
                Array.Resize(ref _dense, _dense.Length << 1);

            if (entityID >= _sparse.Length)
            {
                int neadedSpace = _sparse.Length;
                while (entityID >= neadedSpace) 
                    neadedSpace <<= 1;
                Array.Resize(ref _sparse, neadedSpace);
            }

            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }

        public void Remove(int entityID)
        {
            if (_lockCount > 0)
            {
                AddDelayedOp(entityID, DEALAYED_REMOVE);
                return;
            }

            if (!Contains(entityID)) 
                return;

            _dense[_sparse[entityID]] = _dense[_count];
            _sparse[_dense[_count--]] = _sparse[entityID];
            _sparse[entityID] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddDelayedOp(int entityID, int isAddBitFlag)
        {
            if (_delayedOpsCount >= _delayedOps.Length)
            {
                Array.Resize(ref _delayedOps, _delayedOps.Length << 1);
            }
            _delayedOps[_delayedOpsCount++] = entityID | isAddBitFlag; // delayedOp = entityID add isAddBitFlag
        }
        #endregion

        //TODO добавить автосоритровку при каждом GetEnumerator

        #region AddGroup/RemoveGroup
        public void AddGroup(EcsReadonlyGroup group)
        {
            foreach (var item in group) Add(item.id);
        }
        public void RemoveGroup(EcsReadonlyGroup group)
        {
            foreach (var item in group) Remove(item.id);
        }
        public void AddGroup(EcsGroup group)
        {
            foreach (var item in group) Add(item.id);
        }
        public void RemoveGroup(EcsGroup group)
        {
            foreach (var item in group) Remove(item.id);
        }
        #endregion

        #region GetEnumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unlock()
        {
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_lockCount <= 0)
            {
                throw new Exception($"Invalid lock-unlock balance for {nameof(EcsGroup)}.");
            }
#endif
            if (--_lockCount <= 0)
            {
                for (int i = 0; i < _delayedOpsCount; i++)
                {
                    delayedOp op = _delayedOps[i];
                    if (op >= 0) //delayedOp.IsAdded
                    {
                        Add(op & int.MaxValue); //delayedOp.Entity
                    }
                    else
                    {
                        Remove(op & int.MaxValue); //delayedOp.Entity
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            _lockCount++;
            return new Enumerator(this);
        }
        #endregion

        #region Enumerator
        public struct Enumerator : IDisposable
        {
            private readonly EcsGroup _source;
            private int _pointer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
                _source = group;
                _pointer = 0;
            }

            private static EcsProfilerMarker _marker = new EcsProfilerMarker("EcsGroup.Enumerator.Current");

            public ent Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    using (_marker.Auto())
                        return _source.World.GetEntity(_source._dense[_pointer]);
                    // return _source._dense[_pointer];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_pointer <= _source.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _source.Unlock();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _pointer = -1;
            }
        }
        #endregion
    }
}

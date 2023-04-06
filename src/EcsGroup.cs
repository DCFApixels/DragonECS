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

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source) => _source = source;
        #endregion

        #region Properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.Count;
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.CapacityDense;
        }
        public int CapacitySparce
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.CapacitySparce;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int entityID) => _source.Contains(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => _source.GetEnumerator();
        #endregion
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
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dense.Length;
        }
        public int CapacitySparce
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sparse.Length;
        }
        public EcsReadonlyGroup Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new EcsReadonlyGroup(this);
        }
        #endregion

        #region Constrcutors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup(IEcsWorld source, int denseCapacity = 64, int delayedOpsCapacity = 128)
        {
            _source = source;
            source.RegisterGroup(this);
            _dense = new int[denseCapacity];
            _sparse = new int[source.EntitesCapacity];

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
            //TODO добавить проверку на больше 0 в #if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
#endif
            return /*entityID > 0 && entityID < _sparse.Length && */ _sparse[entityID] > 0;
        }
        #endregion

        #region IndexOf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int entityID)
        {
            return _sparse[entityID];
        }
        #endregion

        #region add/remove
        public void UncheckedAdd(int entityID) => AddInternal(entityID);
        public void Add(int entityID)
        {
            if (Contains(entityID)) return;
            AddInternal(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddInternal(int entityID)
        {
            if (_lockCount > 0)
            {
                AddDelayedOp(entityID, DEALAYED_ADD);
                return;
            }

            if (++_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length << 1);

            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }

        public void UncheckedRemove(int entityID) => RemoveInternal(entityID);
        public void Remove(int entityID)
        {
            if (!Contains(entityID)) return;
            RemoveInternal(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveInternal(int entityID)
        {
            if (_lockCount > 0)
            {
                AddDelayedOp(entityID, DEALAYED_REMOVE);
                return;
            }
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

        internal void OnWorldResize(int newSize)
        {
            Array.Resize(ref _sparse, newSize);
        }

        //TODO добавить метод Sort

        //TODO добавить автосоритровку при каждом GetEnumerator и проверить будет ли прирост производительности или ее падение.
        //Суть в том что так возможно можно будет более плотно подавать данные в проц

        #region AddGroup/RemoveGroup
        public void AddGroup(EcsReadonlyGroup group)
        { foreach (var item in group) Add(item.id); }
        public void RemoveGroup(EcsReadonlyGroup group)
        { foreach (var item in group) Remove(item.id); }
        public void AddGroup(EcsGroup group)
        { foreach (var item in group) Add(item.id); }
        public void RemoveGroup(EcsGroup group)
        { foreach (var item in group) Remove(item.id); }

        public void UncheckedAddGroup(EcsReadonlyGroup group)
        { foreach (var item in group) AddInternal(item.id); }
        public void UncheckedRemoveGroup(EcsReadonlyGroup group)
        { foreach (var item in group) RemoveInternal(item.id); }
        public void UncheckedAddGroup(EcsGroup group)
        { foreach (var item in group) AddInternal(item.id); }
        public void UncheckedRemoveGroup(EcsGroup group)
        { foreach (var item in group) RemoveInternal(item.id); }
        #endregion

        #region GetEnumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unlock()
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
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
                        UncheckedAdd(op & int.MaxValue); //delayedOp.Entity
                    else
                        UncheckedRemove(op & int.MaxValue); //delayedOp.Entity
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
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
                _source = group;
                _dense = group._dense;
                _count = group.Count;
                _index = 0;
            }
            public ent Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _source.World.GetEntity(_dense[_index]);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // <= потму что отсчет начинается с индекса 1
                return ++_index <= _count && _count < _dense.Length; //_count < _dense.Length дает среде понять что проверки на выход за границы не нужны
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _source.Unlock();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { }
        }
        #endregion
    }


    public static class EcsGroupExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize<T>(this EcsGroup self, ref T[] array)
        {
            if (array.Length < self.CapacityDense) Array.Resize(ref array, self.CapacityDense);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize<T>(this EcsReadonlyGroup self, ref T[] array)
        {
            if (array.Length < self.CapacityDense) Array.Resize(ref array, self.CapacityDense);
        }
    }
}

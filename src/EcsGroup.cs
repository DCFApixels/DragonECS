using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsReadonlyGroup 
    {
        public IEcsWorld World { get; }
        public int Count { get; }
        public EcsGroup.Enumerator GetEnumerator();
    }
    public interface IEcsGroup : IEcsReadonlyGroup
    {
        public void Add(int entityID);
        public void Remove(int entityID);
    }

    public class EcsGroup : IEcsGroup 
    {
        private IEcsWorld _source;
        private SparseSet _entities;

        private DelayedOp[] _delayedOps;
        private int _delayedOpsCount;

        private int _lockCount;

        #region Properties
        public IEcsWorld World => _source;
        public int Count => _entities.Count;
        #endregion

        #region Constrcutors
        public EcsGroup(IEcsWorld world,  int entitiesCapacity, int delayedOpsCapacity = 128)
        {
            _source = world;
            _entities = new SparseSet(entitiesCapacity);
            _delayedOps = new DelayedOp[delayedOpsCapacity];
            _lockCount = 0;
        }
        #endregion

        #region add/remove
        public void Add(int entityID)
        {
            if (_lockCount > 0)
                AddDelayedOp(entityID, true);
            _entities.Add(entityID);
        }

        public void Remove(int entityID)
        {
            if (_lockCount > 0)
                AddDelayedOp(entityID, false);
            _entities.Remove(entityID);
        }

        private void AddDelayedOp(int entityID, bool isAdd)
        {
            if (_delayedOpsCount >= _delayedOps.Length)
            {
                Array.Resize(ref _delayedOps, _delayedOps.Length << 1);
            }
            ref DelayedOp delayedOd = ref _delayedOps[_delayedOpsCount];
            delayedOd.Entity = entityID;
            delayedOd.Added = isAdd;
        }
        #endregion

        #region AddGroup/RemoveGroup
        public void AddGroup(IEcsReadonlyGroup group)
        {
            foreach (var item in group)
            {
                _entities.TryAdd(item.id);
            }
        }

        public void RemoveGroup(IEcsReadonlyGroup group)
        {
            foreach (var item in group)
            {
                _entities.TryRemove(item.id);
            }
        }
        #endregion

        #region GetEnumerator
        private void Unlock()
        {
#if DEBUG
            if (_lockCount <= 0)
            {
                throw new Exception($"Invalid lock-unlock balance for {nameof(EcsFilter)}.");
            }
#endif
            if (--_lockCount <= 0)
            {
                for (int i = 0; i < _delayedOpsCount; i++)
                {
                    ref DelayedOp op = ref _delayedOps[i];
                    if (op.Added)
                    {
                        Add(op.Entity);
                    }
                    else
                    {
                        Remove(op.Entity);
                    }
                }
            }
        }
        public Enumerator GetEnumerator()
        {
            _lockCount++;
            return new Enumerator(this);
        }
        #endregion

        #region Utils
        public ref struct Enumerator
        {
            private readonly EcsGroup _source;
            private readonly SparseSet _entities;
            private int _index;
            private Entity _currentEntity;

            public Enumerator(EcsGroup group)
            {
                _source = group;
                _entities = group._entities;
                _index = -1;
                _currentEntity = new Entity(group.World, -1);
            }

            public Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    _currentEntity.id = _entities[_index];
                    return _currentEntity;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_index < _entities.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _source.Unlock();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _index = -1;
                _currentEntity.id = -1;
            }
        }

        private struct DelayedOp
        {
            public bool Added;
            public int Entity;
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private readonly Dictionary<(Type, object), EcsQueryExecutor> _executorCoures = new Dictionary<(Type, object), EcsQueryExecutor>(256);
        public TExecutor GetExecutor<TExecutor>(EcsMask mask)
            where TExecutor : EcsQueryExecutor, new()
        {
            var coreType = typeof(TExecutor);
            if (_executorCoures.TryGetValue((coreType, mask), out EcsQueryExecutor core) == false)
            {
                core = new TExecutor();
                core.Initialize(this, mask);
                _executorCoures.Add((coreType, mask), core);
            }
            return (TExecutor)core;
        }
        public TExecutorCore GetExecutor<TExecutorCore>(EcsStaticMask staticMask)
            where TExecutorCore : EcsQueryExecutor, new()
        {
            var coreType = typeof(TExecutorCore);
            if (_executorCoures.TryGetValue((coreType, staticMask), out EcsQueryExecutor core) == false)
            {
                core = new TExecutorCore();
                core.Initialize(this, staticMask.ToMask(this));
                _executorCoures.Add((coreType, staticMask), core);
            }
            return (TExecutorCore)core;
        }
    }
    public abstract class EcsQueryExecutor
    {
        private EcsWorld _source;
        private EcsMask _mask;
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.id; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }
        protected EcsMask Mask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _mask; }
        }
        public abstract long Version { get; }
        internal void Initialize(EcsWorld world, EcsMask mask)
        {
            _source = world;
            _mask = mask;
            OnInitialize();
        }
        internal void Destroy()
        {
            OnDestroy();
            _source = null;
        }
        protected abstract void OnInitialize();
        protected abstract void OnDestroy();
    }

    public readonly struct PoolVersionsChecker
    {
        private readonly EcsMask _mask;
        private readonly long[] _versions;

        public PoolVersionsChecker(EcsMask mask)
        {
            _mask = mask;
            _versions = new long[mask._inc.Length + mask._exc.Length];
        }

        public bool NextEquals()
        {
            var slots = _mask.World._poolSlots;
            bool result = true;
            int index = 0;
            foreach (var i in _mask._inc)
            {
                if (slots[i].version != _versions[index++])
                {
                    result = false;
                }
            }
            foreach (var i in _mask._exc)
            {
                if (slots[i].version != _versions[index++])
                {
                    result = false;
                }
            }
            return result;
        }
    }
}
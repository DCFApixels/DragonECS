#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private readonly Dictionary<(Type, object), IQueryExecutorImplementation> _executorCoures;

        public TExecutor GetExecutorForMask<TExecutor>(IComponentMask gmask)
            where TExecutor : MaskQueryExecutor, new()
        {
            var executorType = typeof(TExecutor);
            //проверяет ключ по абстрактной маске
            if (_executorCoures.TryGetValue((executorType, gmask), out IQueryExecutorImplementation executor) == false)
            {
                var mask = gmask.ToMask(this);
                //проверяет ключ по конкретной маске, или что конкретная и абстрактая одна и таже
                if (mask == gmask ||
                    _executorCoures.TryGetValue((executorType, mask), out executor) == false)
                {
                    TExecutor executorCore = new TExecutor();
                    executorCore.Initialize(this, mask);
                    executor = executorCore;
                }
                _executorCoures.Add((executorType, gmask), executor);
            }
            return (TExecutor)executor;
        }

        public void GetMaskQueryExecutors(List<MaskQueryExecutor> result, ref int version)
        {
            if (_executorCoures == null || version == _executorCoures.Count)
            {
                return;
            }

            result.Clear();

            foreach (var item in _executorCoures)
            {
                if (item.Value is MaskQueryExecutor x)
                {
                    result.Add(x);
                }
            }

            version = _executorCoures.Count;
        }
    }
}

namespace DCFApixels.DragonECS.Core
{
    public interface IQueryExecutorImplementation
    {
        EcsWorld World { get; }
        long Version { get; }
        bool IsCached { get; }
        int LastCachedCount { get; }
        void Destroy();
    }
    public abstract class MaskQueryExecutor : IQueryExecutorImplementation
    {
        private EcsWorld _source;
        private EcsMask _mask;
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ID; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }
        public EcsMask Mask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _mask; }
        }
        public abstract long Version { get; }
        public abstract bool IsCached { get; }
        public abstract int LastCachedCount { get; }
        internal void Initialize(EcsWorld world, EcsMask mask)
        {
            _source = world;
            _mask = mask;
            OnInitialize();
        }
        void IQueryExecutorImplementation.Destroy()
        {
            OnDestroy();
            _source = null;
        }
        protected abstract void OnInitialize();
        protected abstract void OnDestroy();
    }

    public readonly unsafe struct WorldStateVersionsChecker : IDisposable
    {
        private readonly EcsWorld _world;
        private readonly int[] _maskInc;
        private readonly int[] _maskExc;
        // [0]                      world version
        // [-> _maskInc.Length]     inc versions
        // [-> _maskExc.Length]     exc versions
        private readonly long* _versions;
        private readonly int _count;
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _versions[0]; }
        }

        public WorldStateVersionsChecker(EcsMask mask)
        {
            _world = mask.World;
            _maskInc = mask._incs;
            _maskExc = mask._excs;
            _count = 1 + mask._incs.Length + mask._excs.Length;
     
            _versions = UnmanagedArrayUtility.NewAndInit<long>(_count);
        }
        public bool Check()
        {
            if (*_versions == _world.Version)
            {
                return true;
            }

            long* versionsPtr = _versions;
            var slots = _world._poolSlots;
            foreach (var slotIndex in _maskInc)
            {
                versionsPtr++;
                if (*versionsPtr != slots[slotIndex].version)
                {
                    return false;
                }
            }
            foreach (var slotIndex in _maskExc)
            {
                versionsPtr++;
                if (*versionsPtr != slots[slotIndex].version)
                {
                    return false;
                }
            }
            return true;
        }
        public void Next()
        {
            *_versions = _world.Version;

            long* versionsPtr = _versions;
            var slots = _world._poolSlots;
            foreach (var slotIndex in _maskInc)
            {
                versionsPtr++;
                *versionsPtr = slots[slotIndex].version;
            }
            foreach (var slotIndex in _maskExc)
            {
                versionsPtr++;
                *versionsPtr = slots[slotIndex].version;
            }
        }
        public bool CheckAndNext()
        {
            if (*_versions == _world.Version)
            {
                return true;
            }
            *_versions = _world.Version;

            long* versionsPtr = _versions;
            var slots = _world._poolSlots;
            // Так как проверки EXC работают не правильно при отсутсвии INC,
            // то проверки без INC должны всегда возвращать false.
            bool result = _maskInc.Length > 0;
            foreach (var slotIndex in _maskInc)
            {
                versionsPtr++;
                if (*versionsPtr != slots[slotIndex].version)
                {
                    result = false;
                    *versionsPtr = slots[slotIndex].version;
                }
            }
            foreach (var slotIndex in _maskExc)
            {
                versionsPtr++;
                if (*versionsPtr != slots[slotIndex].version)
                {
                    result = false;
                    *versionsPtr = slots[slotIndex].version;
                }
            }
            return result;
        }

        public void Dispose()
        {
            UnmanagedArrayUtility.Free(_versions);
        }
    }
}
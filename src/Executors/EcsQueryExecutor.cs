using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private readonly Dictionary<(Type, object), EcsQueryExecutor> _executorCoures = new Dictionary<(Type, object), EcsQueryExecutor>(256);
        public TExecutor GetExecutor<TExecutor>(IEcsComponentMask mask)
            where TExecutor : EcsQueryExecutor, new()
        {
            var coreType = typeof(TExecutor);
            if (_executorCoures.TryGetValue((coreType, mask), out EcsQueryExecutor core) == false)
            {
                core = new TExecutor();
                core.Initialize(this, mask.ToMask(this));
                _executorCoures.Add((coreType, mask), core);
            }
            return (TExecutor)core;
        }
    }
    public abstract class EcsQueryExecutor
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

    public readonly unsafe struct WorldStateVersionsChecker : IDisposable
    {
        private readonly EcsWorld _world;
        private readonly int[] _maskInc;
        private readonly int[] _maskExc;
        // [0]                      world version
        // [-> _maskInc.Length]     inc versions
        // [-> _maskExc.Length]     exc versions
        private readonly long* _versions;
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _versions[0]; }
        }

        public WorldStateVersionsChecker(EcsMask mask)
        {
            _world = mask.World;
            _maskInc = mask._inc;
            _maskExc = mask._exc;
            _versions = UnmanagedArrayUtility.New<long>(1 + mask._inc.Length + mask._exc.Length);
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

            long* ptr = _versions;
            var slots = _world._poolSlots;
            foreach (var slotIndex in _maskInc)
            {
                ptr++;
                *ptr = slots[slotIndex].version;
            }
            foreach (var slotIndex in _maskExc)
            {
                ptr++;
                *ptr = slots[slotIndex].version;
            }
        }
        public bool CheckAndNext()
        {
            if (*_versions == _world.Version)
            {
                return true;
            }
            *_versions = _world.Version;

            long* ptr = _versions;
            var slots = _world._poolSlots;
            bool result = true;
            foreach (var slotIndex in _maskInc)
            {
                ptr++;
                if (*ptr != slots[slotIndex].version)
                {
                    result = false;
                    *ptr = slots[slotIndex].version;
                }
            }
            foreach (var slotIndex in _maskExc)
            {
                ptr++;
                if (*ptr != slots[slotIndex].version)
                {
                    result = false;
                    *ptr = slots[slotIndex].version;
                }
            }
            return result;
        }

        public void Dispose()
        {

        }
    }
}
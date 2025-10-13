#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

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

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public readonly unsafe struct WorldStateVersionsChecker : IDisposable
    {
        private readonly EcsWorld _world;
        private readonly MemoryAllocator.Handler _handler;
        private readonly int* _componentIDs;
        // _versions[0]                          world version
        // _versions[-> EcsMask.Inc.Length]      inc versions
        // _versions[-> EcsMask.Exc.Length]      exc versions
        // _versions[-> EcsMask.Any.Length]      any versions
        private readonly long* _versions;
        private readonly int _count;
        private readonly bool _isNotOnlyExc;
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _versions[0]; }
        }

        public WorldStateVersionsChecker(EcsMask mask)
        {
            _world = mask.World;
            _count = 1 + mask._incs.Length + mask._excs.Length + mask._anys.Length;

            _handler = MemoryAllocator.AllocAndInit<int>(_count * 2 + _count);
            _versions = _handler.As<long>();
            _componentIDs = (int*)(_handler.As<long>() + _count);

            var ptr = _componentIDs + 1;
            Marshal.Copy(mask._incs, 0, (IntPtr)ptr, mask._incs.Length);
            ptr += mask._incs.Length;
            Marshal.Copy(mask._excs, 0, (IntPtr)ptr, mask._excs.Length);
            ptr += mask._excs.Length;
            Marshal.Copy(mask._anys, 0, (IntPtr)ptr, mask._anys.Length);

            _isNotOnlyExc = mask._incs.Length > 0 || mask._anys.Length > 0;
        }
        public bool Check()
        {
            if (*_versions == _world.Version)
            {
                return true;
            }

            var slots = _world._poolSlots;
            for (int i = 1; i < _count; i++)
            {
                if (_versions[i] == slots[_componentIDs[i]].version)
                {
                    return false;
                }
            }
            return true;
        }
        public void Next()
        {
            *_versions = _world.Version;

            var slots = _world._poolSlots;
            for (int i = 1; i < _count; i++)
            {
                _versions[i] = slots[_componentIDs[i]].version;
            }
        }
        public bool CheckAndNext()
        {
            if (*_versions == _world.Version)
            {
                return true;
            }
            *_versions = _world.Version;

            var slots = _world._poolSlots;
            bool result = _isNotOnlyExc;
            for (int i = 1; i < _count; i++)
            {
                if (_versions[i] != slots[_componentIDs[i]].version)
                {
                    result = false;
                    _versions[i] = slots[_componentIDs[i]].version;
                }
            }
            return result;
        }

        public void Dispose()
        {
            MemoryAllocator.Free(_handler);
        }
    }
}
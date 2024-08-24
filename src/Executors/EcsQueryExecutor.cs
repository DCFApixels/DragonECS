﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private readonly Dictionary<(Type, Type), EcsQueryExecutorCore> _executorCoures = new Dictionary<(Type, Type), EcsQueryExecutorCore>();
        private readonly ExecutorMediator _executorsMediator;
        public readonly struct ExecutorMediator
        {
            public readonly EcsWorld World;
            internal ExecutorMediator(EcsWorld world)
            {
                if (world == null || world._executorsMediator.World != null)
                {
                    throw new InvalidOperationException();
                }
                World = world;
            }
            public TExecutorCore GetCore<TExecutorCore>(Type aspectType)
                where TExecutorCore : EcsQueryExecutorCore, new()
            {
                var coreType = typeof(EcsQueryExecutorCore);
                if(World._executorCoures.TryGetValue((aspectType, coreType), out EcsQueryExecutorCore core))
                {
                    core = new TExecutorCore();
                    core.Initialize(World);
                    World._executorCoures.Add((aspectType, coreType), core);
                }
                return (TExecutorCore)core;
            }
        }
    }
    public abstract class EcsQueryExecutorCore
    {
        private EcsWorld _source;
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
        internal void Initialize(EcsWorld world)
        {
            _source = world;
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
    public abstract class EcsQueryExecutor
    {
        private EcsWorld _source;
        private EcsWorld.ExecutorMediator _mediator;
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
        protected EcsWorld.ExecutorMediator Mediator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _mediator; }
        }
        public abstract long Version { get; }
        internal void Initialize(EcsWorld world, EcsWorld.ExecutorMediator mediator)
        {
            _source = world;
            _mediator = mediator;
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
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsAspect : ITemplateNode
    {
        #region Initialization Halpers
        [ThreadStatic]
        private static Stack<Builder> _constructorBuildersStack = null;
        private static Stack<Builder> GetBuildersStack()
        {
            if (_constructorBuildersStack == null)
            {
                _constructorBuildersStack = new Stack<Builder>();
            }
            return _constructorBuildersStack;
        }
        protected static Builder CurrentBuilder
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(CurrentBuilder));
                }
                return buildersStack.Peek();
            }
        }
        protected static IncludeMarker Inc
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(Inc));
                }
                return buildersStack.Peek().Inc;
            }
        }
        protected static ExcludeMarker Exc
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(Exc));
                }
                return buildersStack.Peek().Exc;
            }
        }
        protected static OptionalMarker Opt
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(Opt));
                }
                return buildersStack.Peek().Opt;
            }
        }
        #endregion

        internal EcsWorld _source;
        internal EcsMask _mask;
        private bool _isBuilt = false;

        #region Properties
        public EcsMask Mask
        {
            get { return _mask; }
        }
        public EcsWorld World
        {
            get { return _source; }
        }
        public bool IsInit
        {
            get { return _isBuilt; }
        }
        #endregion

        #region Methods
        public bool IsMatches(int entityID)
        {
            return _source.IsMatchesMask(_mask, entityID);
        }
        #endregion

        #region Builder
        protected virtual void Init(Builder b) { }
        public sealed class Builder
        {
            private EcsWorld _world;
            private EcsMask.Builder _maskBuilder;

            public IncludeMarker Inc
            {
                get { return new IncludeMarker(this); }
            }
            public ExcludeMarker Exc
            {
                get { return new ExcludeMarker(this); }
            }
            public OptionalMarker Opt
            {
                get { return new OptionalMarker(this); }
            }

            public EcsWorld World => _world;

            private Builder(EcsWorld world)
            {
                _world = world;
                _maskBuilder = EcsMask.New(world);
            }
            internal static unsafe TAspect New<TAspect>(EcsWorld world) where TAspect : EcsAspect, new()
            {
                Builder builder = new Builder(world);
                Type aspectType = typeof(TAspect);
                EcsAspect newAspect;

                var buildersStack = GetBuildersStack();
                buildersStack.Push(builder);

                newAspect = new TAspect();
                newAspect.Init(builder);
                buildersStack.Pop();
                newAspect._source = world;
                builder.Build(out newAspect._mask);
                newAspect._isBuilt = true;

                newAspect._sortIncBuffer = new UnsafeArray<int>(newAspect._mask._inc.Length, true);
                newAspect._sortExcBuffer = new UnsafeArray<int>(newAspect._mask._exc.Length, true);
                newAspect._sortIncChunckBuffer = new UnsafeArray<EcsMaskChunck>(newAspect._mask._incChunckMasks.Length, true);
                newAspect._sortExcChunckBuffer = new UnsafeArray<EcsMaskChunck>(newAspect._mask._excChunckMasks.Length, true);

                for (int i = 0; i < newAspect._sortIncBuffer.Length; i++)
                {
                    newAspect._sortIncBuffer.ptr[i] = newAspect._mask._inc[i];
                }
                for (int i = 0; i < newAspect._sortExcBuffer.Length; i++)
                {
                    newAspect._sortExcBuffer.ptr[i] = newAspect._mask._exc[i];
                }

                for (int i = 0; i < newAspect._sortIncChunckBuffer.Length; i++)
                {
                    newAspect._sortIncChunckBuffer.ptr[i] = newAspect._mask._incChunckMasks[i];
                }
                for (int i = 0; i < newAspect._sortExcChunckBuffer.Length; i++)
                {
                    newAspect._sortExcChunckBuffer.ptr[i] = newAspect._mask._excChunckMasks[i];
                }

                return (TAspect)newAspect;
            }

            #region Include/Exclude/Optional/Combine/Except
            public TPool IncludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPoolInstance<TPool>();
                IncludeImplicit(pool.ComponentType);
                return pool;
            }
            public TPool ExcludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPoolInstance<TPool>();
                ExcludeImplicit(pool.ComponentType);
                return pool;
            }
            public TPool OptionalPool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return _world.GetPoolInstance<TPool>();
            }
            private void IncludeImplicit(Type type)
            {
                _maskBuilder.Include(type);
            }
            private void ExcludeImplicit(Type type)
            {
                _maskBuilder.Exclude(type);
            }
            public TOtherAspect Combine<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect, new()
            {
                var result = _world.GetAspect<TOtherAspect>();
                _maskBuilder.Combine(result.Mask);
                return result;
            }
            public TOtherAspect Except<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect, new()
            {
                var result = _world.GetAspect<TOtherAspect>();
                _maskBuilder.Except(result.Mask);
                return result;
            }
            #endregion

            private void Build(out EcsMask mask)
            {
                mask = _maskBuilder.Build();
            }

            #region SupportReflectionHack
#if UNITY_2020_3_OR_NEWER
            [UnityEngine.Scripting.Preserve]
#endif
            private void SupportReflectionHack<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                IncludePool<TPool>();
                ExcludePool<TPool>();
                OptionalPool<TPool>();
                IncludeImplicit(null);
                ExcludeImplicit(null);
            }
            #endregion
        }
        #endregion

        #region Finalizator
        unsafe ~EcsAspect()
        {
            _sortIncBuffer.Dispose();
            _sortExcBuffer.Dispose();
            _sortIncChunckBuffer.Dispose();
            _sortExcChunckBuffer.Dispose();
        }
        #endregion

        #region Iterator
        public Iterator GetIterator()
        {
            return new Iterator(this, _source.Entities);
        }
        public Iterator GetIteratorFor(EcsSpan span)
        {
            return new Iterator(this, span);
        }
        #endregion

        #region Combined
        private readonly struct Combined
        {
            public readonly EcsAspect aspect;
            public readonly int order;
            public Combined(EcsAspect aspect, int order)
            {
                this.aspect = aspect;
                this.order = order;
            }
        }
        #endregion

        #region Iterator
        public ref struct Iterator
        {
            public readonly short worldID;
            public readonly EcsAspect aspect;
            private EcsSpan _span;

            public Iterator(EcsAspect aspect, EcsSpan span)
            {
                worldID = aspect.World.id;
                _span = span;
                this.aspect = aspect;
            }
        }
        #endregion

        #region Template
        public virtual void Apply(short worldID, int entityID)
        {
            EcsWorld world = EcsWorld.GetWorld(worldID);
            foreach (var incTypeID in _mask._inc)
            {
                var pool = world.FindPoolInstance(incTypeID);
                if (pool != null)
                {
                    if (pool.Has(entityID) == false)
                    {
                        pool.AddRaw(entityID, null);
                    }
                }
#if DEBUG
                else
                {
                    EcsDebug.PrintWarning("Component has not been added because the pool has not been initialized yet.");
                }
#endif
            }
            foreach (var excTypeID in _mask._exc)
            {
                var pool = world.FindPoolInstance(excTypeID);
                if (pool != null && pool.Has(entityID))
                {
                    pool.Del(entityID);
                }
            }
        }
        #endregion
    }

    #region Constraint Markers
    public readonly ref struct IncludeMarker
    {
        private readonly EcsAspect.Builder _builder;
        public IncludeMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.IncludePool<T>();
        }
    }
    public readonly ref struct ExcludeMarker
    {
        private readonly EcsAspect.Builder _builder;
        public ExcludeMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.ExcludePool<T>();
        }
    }
    public readonly ref struct OptionalMarker
    {
        private readonly EcsAspect.Builder _builder;
        public OptionalMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.OptionalPool<T>();
        }
    }
    #endregion
}
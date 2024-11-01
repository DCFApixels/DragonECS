﻿using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public abstract class EcsAspect : ITemplateNode, IComponentMask
    {
        #region Initialization Halpers
        [ThreadStatic]
        private static Builder[] _constructorBuildersStack;
        [ThreadStatic]
        private static int _constructorBuildersStackIndex;
        protected static Builder CurrentBuilder
        {
            get
            {
                if (_constructorBuildersStack == null || _constructorBuildersStackIndex < 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(CurrentBuilder));
                }
                return _constructorBuildersStack[_constructorBuildersStackIndex];
            }
        }
        protected static IncludeMarker Inc
        {
            get { return CurrentBuilder.Inc; }
        }
        protected static ExcludeMarker Exc
        {
            get { return CurrentBuilder.Exc; }
        }
        protected static OptionalMarker Opt
        {
            get { return CurrentBuilder.Opt; }
        }
        #endregion

        internal EcsWorld _source;
        internal EcsMask _mask;
        private bool _isBuilt = false;

        //Инициализация аспектов проходит в синхронизированном состоянии, поэтому использование _staticMaskCache потоко безопасно.
        private static Dictionary<Type, EcsStaticMask> _staticMaskCache = new Dictionary<Type, EcsStaticMask>();

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
        /// <summary>
        /// Статическая инициализация означет что каждый новый эекземпляр идентичен другому, инициализация стандартным путем создает идентичные экземпляры, поэтому значение по умолчанию true.
        /// </summary>
        protected virtual bool IsStaticInitialization
        {
            get { return true; }
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
            private EcsStaticMask.Builder _maskBuilder;

            #region Properties
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
            public EcsWorld World
            {
                get { return _world; }
            }
            #endregion

            #region Constructors/New
            private Builder() { }
            internal static unsafe TAspect New<TAspect>(EcsWorld world) where TAspect : EcsAspect, new()
            {
                //Get Builder
                if (_constructorBuildersStack == null)
                {
                    _constructorBuildersStack = new Builder[4];
                    _constructorBuildersStackIndex = -1;
                }
                _constructorBuildersStackIndex++;
                if (_constructorBuildersStackIndex >= _constructorBuildersStack.Length)
                {
                    Array.Resize(ref _constructorBuildersStack, _constructorBuildersStack.Length << 1);
                }
                Builder builder = _constructorBuildersStack[_constructorBuildersStackIndex];
                if (builder == null)
                {
                    builder = new Builder();
                    _constructorBuildersStack[_constructorBuildersStackIndex] = builder;
                }

                //Setup Builder
                EcsStaticMask staticMask = null;
                if (_staticMaskCache.TryGetValue(typeof(TAspect), out staticMask) == false)
                {
                    builder._maskBuilder = EcsStaticMask.New();
                }
                builder._world = world;

                //Building
                TAspect newAspect = new TAspect();
                newAspect._source = world;
                newAspect.Init(builder);

                //Build Mask
                if (staticMask == null)
                {
                    staticMask = builder._maskBuilder.Build();
                    builder._maskBuilder = default;
                    if (newAspect.IsStaticInitialization)
                    {
                        _staticMaskCache.Add(typeof(TAspect), staticMask);
                    }
                }
                newAspect._mask = staticMask.ToMask(world);
                newAspect._isBuilt = true;

                _constructorBuildersStackIndex--;
                return newAspect;
            }
            #endregion

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
                if (_maskBuilder.IsNull == false)
                {
                    _maskBuilder.Inc(type);
                }
            }
            private void ExcludeImplicit(Type type)
            {
                if (_maskBuilder.IsNull == false)
                {
                    _maskBuilder.Exc(type);
                }
            }
            public TOtherAspect Combine<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect, new()
            {
                var result = _world.GetAspect<TOtherAspect>();
                if (_maskBuilder.IsNull == false)
                {
                    _maskBuilder.Combine(result.Mask._staticMask);
                }
                return result;
            }
            public TOtherAspect Except<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect, new()
            {
                var result = _world.GetAspect<TOtherAspect>();
                if (_maskBuilder.IsNull == false)
                {
                    _maskBuilder.Except(result.Mask._staticMask);
                }
                return result;
            }
            #endregion

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

        #region Template
        public virtual void Apply(short worldID, int entityID)
        {
            EcsWorld world = EcsWorld.GetWorld(worldID);
            foreach (var incTypeID in _mask._incs)
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
            foreach (var excTypeID in _mask._excs)
            {
                var pool = world.FindPoolInstance(excTypeID);
                if (pool != null && pool.Has(entityID))
                {
                    pool.Del(entityID);
                }
            }
        }
        #endregion

        #region Other
        EcsMask IComponentMask.ToMask(EcsWorld world) { return _mask; }
        #endregion

        #region Obsolete
        [Obsolete("Use EcsMask.GetIterator()")]
        public Iterator GetIterator()
        {
            return new Iterator(Mask.GetIterator(), _source.Entities);
        }
        [Obsolete("Use EcsMask.GetIterator().Iterate(span)")]
        public Iterator GetIteratorFor(EcsSpan span)
        {
            return new Iterator(Mask.GetIterator(), span);
        }
        [Obsolete("Use EcsMaskIterator")]
        public ref struct Iterator
        {
            public readonly short worldID;
            public readonly EcsMaskIterator.Enumerable iterator;
            private EcsSpan _span;

            public Iterator(EcsMaskIterator iterator, EcsSpan span)
            {
                worldID = iterator.World.ID;
                _span = span;
                this.iterator = iterator.Iterate(span);
            }

            #region CopyTo
            public void CopyTo(EcsGroup group)
            {
                iterator.CopyTo(group);
            }
            public int CopyTo(ref int[] array)
            {
                return iterator.CopyTo(ref array);
            }
            public EcsSpan CopyToSpan(ref int[] array)
            {
                int count = CopyTo(ref array);
                return new EcsSpan(worldID, array, count);
            }
            #endregion

            public EcsMaskIterator.Enumerable.Enumerator GetEnumerator()
            {
                return iterator.GetEnumerator();
            }
        }
        #endregion
    }

    #region EcsAspectExtensions
    //public static class EcsAspectExtensions
    //{
    //    public static EcsAspect.Builder Inc<TPool>(this EcsAspect.Builder self, ref TPool pool) where TPool : IEcsPoolImplementation, new()
    //    {
    //        pool = self.IncludePool<TPool>();
    //        return self;
    //    }
    //    public static EcsAspect.Builder Exc<TPool>(this EcsAspect.Builder self, ref TPool pool) where TPool : IEcsPoolImplementation, new()
    //    {
    //        pool = self.ExcludePool<TPool>();
    //        return self;
    //    }
    //    public static EcsAspect.Builder Opt<TPool>(this EcsAspect.Builder self, ref TPool pool) where TPool : IEcsPoolImplementation, new()
    //    {
    //        pool = self.OptionalPool<TPool>();
    //        return self;
    //    }
    //}
    #endregion

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
#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public readonly struct Singleton<T> where T : struct
    {
        public readonly short WorldID;
        public Singleton(short worldID)
        {
            WorldID = worldID;
            EcsWorld.GetData<T>(worldID);
        }
        public EcsWorld World
        {
            get { return EcsWorld.GetWorld(WorldID); }
        }
        public ref T Value
        {
            get { return ref EcsWorld.GetDataUnchecked<T>(WorldID); }
        }

        public static implicit operator Singleton<T>(SingletonMarker a) { return new Singleton<T>(a.Builder.World.ID); }
    }
    public interface IEcsAspect
    {
        EcsMask Mask { get; set; }
    }

    #region IEcsAspectExtensions tmp
    //    public static class IEcsAspectExtensions
    //    {
    //        public static void Apply(this IEcsAspect aspect, short worldID, int entityID)
    //        {
    //            EcsWorld world = EcsWorld.GetWorld(worldID);
    //            EcsMask mask = aspect.Mask;
    //            foreach (var incTypeID in mask._incs)
    //            {
    //                var pool = world.FindPoolInstance(incTypeID);
    //                if (pool != null)
    //                {
    //                    if (pool.Has(entityID) == false)
    //                    {
    //                        pool.AddEmpty(entityID);
    //                    }
    //                }
    //#if DEBUG
    //                else
    //                {
    //                    EcsDebug.PrintWarning("Component has not been added because the pool has not been initialized yet.");
    //                }
    //#endif
    //            }
    //            foreach (var excTypeID in mask._excs)
    //            {
    //                var pool = world.FindPoolInstance(excTypeID);
    //                if (pool != null && pool.Has(entityID))
    //                {
    //                    pool.Del(entityID);
    //                }
    //            }
    //        }
    //    }
    #endregion

    public static partial class API
    {
        public static IncludeMarker Inc
        {
            get { return EcsAspect.CurrentBuilder.Inc; }
        }
        public static ExcludeMarker Exc
        {
            get { return EcsAspect.CurrentBuilder.Exc; }
        }
        public static OptionalMarker Opt
        {
            get { return EcsAspect.CurrentBuilder.Opt; }
        }
    }
    public abstract class EcsAspect : IEcsAspect, ITemplateNode, IComponentMask
    {
        #region Initialization Halpers
        [ThreadStatic]
        private static Builder[] _constructorBuildersStack;
        [ThreadStatic]
        private static int _constructorBuildersStackIndex;
        protected static Builder B
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
        public static Builder CurrentBuilder
        {
            get { return B; }
        }
        protected static IncludeMarker Inc
        {
            get { return B.Inc; }
        }
        protected static ExcludeMarker Exc
        {
            get { return B.Exc; }
        }
        protected static OptionalMarker Opt
        {
            get { return B.Opt; }
        }
        protected static SingletonMarker Singleton
        {
            get { return B.Singleton; }
        }
        #endregion

        //Инициализация аспектов проходит в синхронизированном состоянии, поэтому использование _staticMaskCache потоко безопасно.
        private readonly static Dictionary<Type, EcsStaticMask> _staticMaskCache = new Dictionary<Type, EcsStaticMask>();

        internal EcsWorld _source;
        internal EcsMask _mask;
        private bool _isBuilt = false;

        #region Properties
        public EcsMask Mask
        {
            get { return _mask; }
            set { }
        }
        public EcsWorld World
        {
            get { return _source; }
        }
        public bool IsInit
        {
            get { return _isBuilt; }
        }
        //public ReadOnlySpan<IEcsPool> Pools
        //{
        //    get { return _pools; }
        //}
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
            public SingletonMarker Singleton
            {
                get { return new SingletonMarker(this); }
            }
            public EcsWorld World
            {
                get { return _world; }
            }
            #endregion

            #region Constructors/New
            private Builder() { }

            internal static unsafe (TAspect aspect, EcsMask mask) New<TAspect>(EcsWorld world) where TAspect : new()
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
                object newAspectObj = newAspect;
                EcsAspect builtinAspect = newAspect as EcsAspect;
                if (builtinAspect != null)
                {
                    builtinAspect._source = world;
                    builtinAspect.Init(builder);
                }
                OnInit(newAspectObj, builder);

                //Build Mask
                if (staticMask == null)
                {
                    staticMask = builder._maskBuilder.Build();
                    builder._maskBuilder = default;
                    if (builtinAspect == null || builtinAspect.IsStaticInitialization)
                    {
                        _staticMaskCache.Add(typeof(TAspect), staticMask);
                    }
                }
                EcsMask mask = staticMask.ToMask(world);
                if (builtinAspect != null)
                {
                    builtinAspect._mask = mask;
                    //var pools = new IEcsPool[builder._poolsBufferCount];
                    //Array.Copy(builder._poolsBuffer, pools, pools.Length);
                    builtinAspect._isBuilt = true;
                }

                _constructorBuildersStackIndex--;

                OnAfterInit(newAspectObj, mask);

                return ((TAspect)newAspectObj, mask);
            }
            #endregion

            #region Include/Exclude/Optional/Combine/Except
            public Singleton<T> Get<T>() where T : struct
            {
                return new Singleton<T>(_world.ID);
            }
            public TPool IncludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = CachePool<TPool>();
                SetMaskInclude(pool.ComponentType);
                return pool;
            }
            public TPool ExcludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = CachePool<TPool>();
                SetMaskExclude(pool.ComponentType);
                return pool;
            }
            public TPool OptionalPool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return CachePool<TPool>();
            }

            private TPool CachePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPoolInstance<TPool>();
                return pool;
            }
            public void SetMaskInclude(Type type)
            {
                if (_maskBuilder.IsNull == false)
                {
                    _maskBuilder.Inc(type);
                }
            }
            public void SetMaskExclude(Type type)
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
                SetMaskInclude(null);
                SetMaskExclude(null);
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
                        pool.AddEmpty(entityID);
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

        #region Events
        public delegate void OnInitApectHandler(object aspect, Builder builder);
        public static event OnInitApectHandler OnInit = delegate { };

        public delegate void OnBuildApectHandler(object aspect, EcsMask mask);
        public static event OnBuildApectHandler OnAfterInit = delegate { };
        #endregion
    }

    #region EcsAspect.Builder.Extensions
    public static class EcsAspectBuilderExtensions
    {
        public static EcsAspect.Builder Inc<TPool>(this EcsAspect.Builder self, ref TPool pool) where TPool : IEcsPoolImplementation, new()
        {
            pool = self.IncludePool<TPool>();
            return self;
        }
        public static EcsAspect.Builder Exc<TPool>(this EcsAspect.Builder self, ref TPool pool) where TPool : IEcsPoolImplementation, new()
        {
            pool = self.ExcludePool<TPool>();
            return self;
        }
        public static EcsAspect.Builder Opt<TPool>(this EcsAspect.Builder self, ref TPool pool) where TPool : IEcsPoolImplementation, new()
        {
            pool = self.OptionalPool<TPool>();
            return self;
        }
    }
    #endregion
}
namespace DCFApixels.DragonECS.Core
{
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
    public readonly ref struct SingletonMarker
    {
        public readonly EcsAspect.Builder Builder;
        public SingletonMarker(EcsAspect.Builder builder)
        {
            Builder = builder;
        }
        public T Get<T>() where T : struct
        {
            return Builder.World.Get<T>();
        }
    }
    #endregion
}
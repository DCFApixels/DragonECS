#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    /// <summary>
    /// Provides access to a world‑scoped singleton component of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The component type, must be a struct.</typeparam>
    public readonly struct Singleton<T> where T : struct
    {
        /// <summary>The ID of the world that owns this singleton.</summary>
        public readonly short WorldID;

        /// <summary>Creates a singleton handle for the specified world ID.</summary>
        /// <param name="worldID">The identifier of the world.</param>
        public Singleton(short worldID)
        {
            WorldID = worldID;
            EcsWorld.GetData<T>(worldID);
        }

        /// <summary>Gets the world instance associated with this singleton.</summary>
        public EcsWorld World
        {
            get { return EcsWorld.GetWorld(WorldID); }
        }

        /// <summary>Gets a reference to the singleton component value.</summary>
        public ref T Value
        {
            get { return ref EcsWorld.GetDataUnchecked<T>(WorldID); }
        }

        public static implicit operator Singleton<T>(SingletonMarker a) { return new Singleton<T>(a.Builder.World.ID); }
    }

    /// <summary>
    /// Defines an aspect — a named set of component conditions and cached pools.
    /// Aspects are used by queries to filter entities and provide fast pool access.
    /// </summary>
    public interface IEcsAspect
    {
        /// <summary>Gets the mask that describes the component conditions of this aspect.</summary>
        EcsMask Mask { get; }
    }

    /// <summary>
    /// Provides global entry points for aspect building markers.
    /// Use <see cref="Inc"/>, <see cref="Exc"/>, <see cref="Any"/>, and <see cref="Opt"/>
    /// inside an <see cref="EcsAspect"/> derived class to declare component conditions.
    /// </summary>
    public static partial class API
    {
        /// <summary>Shortcut that adds the component to the Include condition and caches its pool.</summary>
        public static IncludeMarker Inc
        {
            get { return EcsAspect.CurrentBuilder.Inc; }
        }

        /// <summary>Shortcut that adds the component to the Exclude condition and caches its pool.</summary>
        public static ExcludeMarker Exc
        {
            get { return EcsAspect.CurrentBuilder.Exc; }
        }

        /// <summary>Marker for declaring Any conditions (at least one component must be present).</summary>
        public static AnyMarker Any
        {
            get { return EcsAspect.CurrentBuilder.Any; }
        }

        /// <summary>Marker for declaring Optional components (does not affect filtering).</summary>
        public static OptionalMarker Opt
        {
            get { return EcsAspect.CurrentBuilder.Opt; }
        }
    }

    /// <summary>
    /// Base class for user‑defined aspects. An aspect describes a set of component conditions
    /// used for entity filtering and caches component pools for fast access.
    /// </summary>
    /// <example>
    /// Creating and using an aspect:
    /// <code>
    /// <![CDATA[
    /// public class MyAspect : EcsAspect
    /// {
    ///     // Declare components in the mask and cache pools
    ///     public EcsPool<Position> Positions = Inc; // required
    ///     public EcsPool<Velocity> Velocities = Opt; // optional (does not affect filtering)
    /// }
    /// 
    /// // Query entities with the aspect
    /// foreach (var e in world.Where(out MyAspect a))
    /// {
    ///     ref var pos = ref a.Positions.Get(e);
    ///     // ...
    /// }
    /// ]]>
    /// </code>
    /// </example>
    /// <remarks>
    /// Aspects serve two purposes:
    /// <list type="bullet">
    ///   <item><description>Hold an <see cref="EcsMask"/> that defines which entities are selected by queries.</description></item>
    ///   <item><description>Provide cached access to component pools via public fields of type <see cref="EcsPool{T}"/> or similar.</description></item>
    /// </list>
    /// Use the <see cref="Inc"/>, <see cref="Exc"/>, <see cref="Any"/>, and <see cref="Opt"/> markers
    /// inside the <see cref="Init(Builder)"/> method or as field initializers to declare conditions.
    /// </remarks>

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
        /// <summary>Shortcut that adds the component to the Include condition and caches its pool.</summary>
        protected static IncludeMarker Inc
        {
            get { return B.Inc; }
        }
        /// <summary>Shortcut that adds the component to the Exclude condition and caches its pool.</summary>
        protected static ExcludeMarker Exc
        {
            get { return B.Exc; }
        }
        /// <summary>Shortcut that adds the component to the Any condition and caches its pool.</summary>
        protected static AnyMarker Any
        {
            get { return B.Any; }
        }
        /// <summary>Shortcut for caching an optional component pool without affecting the mask.</summary>
        protected static OptionalMarker Opt
        {
            get { return B.Opt; }
        }
        /// <summary>Shortcut for declaring a world‑scoped singleton dependency.</summary>
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
        /// <summary>Gets the mask that describes the component conditions of this aspect.</summary>
        public EcsMask Mask
        {
            get { return _mask; }
        }

        /// <summary>Gets the world instance that owns this aspect.</summary>
        public EcsWorld World
        {
            get { return _source; }
        }

        /// <summary>Indicates whether the aspect has been fully initialized and built.</summary>
        public bool IsInit
        {
            get { return _isBuilt; }
        }

        /// <summary>
        /// Indicates whether the aspect uses static initialization.
        /// When <c>true</c> (default), all instances of this aspect type share the same static mask cache.
        /// Override to <c>false</c> if the aspect must be rebuilt per instance.
        /// </summary>
        protected virtual bool IsStaticInitialization
        {
            get { return true; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Checks whether the specified entity matches this aspect's mask.
        /// </summary>
        /// <param name="entityID">The entity identifier.</param>
        /// <returns><c>true</c> if the entity satisfies all conditions; otherwise, <c>false</c>.</returns>
        public bool IsMatches(int entityID)
        {
            return _source.IsMatchesMask(_mask, entityID);
        }
        #endregion

        #region Builder
        protected virtual void Init(Builder b) { }

        /// <summary>
        /// Builder used inside <see cref="EcsAspect.Init(Builder)"/> to construct the aspect's mask
        /// and cache component pools.
        /// </summary>
        public sealed class Builder
        {
            private EcsWorld _world;
            private EcsStaticMask.Builder _maskBuilder;

            #region Properties
            /// <summary>Marker for declaring Include conditions (component must be present).</summary>
            public IncludeMarker Inc
            {
                get { return new IncludeMarker(this); }
            }

            /// <summary>Marker for declaring Exclude conditions (component must be absent).</summary>
            public ExcludeMarker Exc
            {
                get { return new ExcludeMarker(this); }
            }

            /// <summary>Marker for declaring Any conditions (at least one component must be present).</summary>
            public AnyMarker Any
            {
                get { return new AnyMarker(this); }
            }

            /// <summary>Marker for declaring Optional components (does not affect filtering).</summary>
            public OptionalMarker Opt
            {
                get { return new OptionalMarker(this); }
            }

            /// <summary>Marker for declaring a singleton dependency.</summary>
            public SingletonMarker Singleton
            {
                get { return new SingletonMarker(this); }
            }

            /// <summary>Gets the world associated with this builder.</summary>
            public EcsWorld World
            {
                get { return _world; }
            }
            #endregion

            #region Constructors/New
            private Builder() { }

            internal static (TAspect aspect, EcsMask mask) New<TAspect>(EcsWorld world) where TAspect : new()
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
            /// <summary>
            /// Retrieves a <see cref="Singleton{T}"/> accessor for the specified component type.
            /// </summary>
            /// <typeparam name="T">The singleton component type.</typeparam>
            /// <returns>A singleton accessor.</returns>
            public Singleton<T> Get<T>() where T : struct
            {
                return new Singleton<T>(_world.ID);
            }

            /// <summary>Adds the pool type <typeparamref name="TPool"/> to the Include condition and caches it.</summary>
            public TPool IncludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = CachePool<TPool>();
                SetMaskInclude(pool.ComponentType);
                return pool;
            }

            /// <summary>Adds the pool type <typeparamref name="TPool"/> to the Exclude condition and caches it.</summary>
            public TPool ExcludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = CachePool<TPool>();
                SetMaskExclude(pool.ComponentType);
                return pool;
            }

            /// <summary>Adds the pool type <typeparamref name="TPool"/> to the Any condition and caches it.</summary>
            public TPool AnyPool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = CachePool<TPool>();
                SetMaskAny(pool.ComponentType);
                return pool;
            }

            /// <summary>Caches the pool type <typeparamref name="TPool"/> without affecting the mask (Optional).</summary>
            public TPool OptionalPool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return CachePool<TPool>();
            }

            private TPool CachePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPoolInstance<TPool>();
                return pool;
            }

            /// <summary>Adds the specified component type to the Include condition.</summary>
            public void SetMaskInclude(Type type)
            {
                if (_maskBuilder.IsNull) { return; }
                _maskBuilder.Inc(type);
            }

            /// <summary>Adds the specified component type to the Exclude condition.</summary>
            public void SetMaskExclude(Type type)
            {
                if (_maskBuilder.IsNull) { return; }
                _maskBuilder.Exc(type);
            }

            /// <summary>Adds the specified component type to the Any condition.</summary>
            public void SetMaskAny(Type type)
            {
                if (_maskBuilder.IsNull) { return; }
                _maskBuilder.Any(type);
            }

            /// <summary>
            /// Combines another aspect into this one. The conditions of the other aspect are merged
            /// according to the specified order (higher order overrides lower).
            /// </summary>
            /// <typeparam name="TOtherAspect">The aspect type to combine.</typeparam>
            /// <param name="order">Override priority (higher value wins in case of conflicts).</param>
            /// <returns>The combined aspect instance.</returns>
            public TOtherAspect Combine<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect, new()
            {
                var result = _world.GetAspect<TOtherAspect>();
                if (_maskBuilder.IsNull == false)
                {
                    _maskBuilder.Combine(result.Mask._staticMask);
                }
                return result;
            }

            /// <summary>
            /// Excludes (subtracts) the conditions of another aspect from this one.
            /// </summary>
            /// <typeparam name="TOtherAspect">The aspect type to exclude.</typeparam>
            /// <param name="order">Override priority.</param>
            /// <returns>The resulting aspect instance.</returns>
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
        /// <summary>
        /// Applies the aspect's Include and Exclude conditions to the given entity.
        /// Adds components from Include conditions and removes components from Exclude conditions.
        /// </summary>
        /// <param name="worldID">The ID of the world.</param>
        /// <param name="entityID">The entity identifier.</param>
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
        /// <summary>Clears the global static mask cache for all aspect types.</summary>
        public static void ClearCache()
        {
            _staticMaskCache.Clear();
        }

        /// <summary>Explicitly converts this aspect to an <see cref="EcsMask"/>.</summary>
        EcsMask IComponentMask.ToMask(EcsWorld world) { return _mask; }
        #endregion

        #region Events
        public delegate void OnInitApectHandler(object aspect, Builder builder);

        /// <summary>Event invoked during aspect initialization.</summary>
        public static event OnInitApectHandler OnInit = delegate { };

        public delegate void OnBuildApectHandler(object aspect, EcsMask mask);

        /// <summary>Event invoked after the aspect mask has been built.</summary>
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
    /// <summary>
    /// Marker for declaring an Include condition — the component must be present on the entity.
    /// Used as a shortcut for initializing pool fields.
    /// </summary>
    public readonly ref struct IncludeMarker
    {
        private readonly EcsAspect.Builder _builder;
        public IncludeMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }

        /// <summary>Caches the pool and adds it to the Include condition.</summary>
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.IncludePool<T>();
        }
    }

    /// <summary>
    /// Marker for declaring an Exclude condition — the component must be absent from the entity.
    /// Used as a shortcut for initializing pool fields.
    /// </summary>
    public readonly ref struct ExcludeMarker
    {
        private readonly EcsAspect.Builder _builder;
        public ExcludeMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }

        /// <summary>Caches the pool and adds it to the Exclude condition.</summary>
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.ExcludePool<T>();
        }
    }

    /// <summary>
    /// Marker for declaring an Any condition — at least one of the listed components must be present on the entity.
    /// Used as a shortcut for initializing pool fields.
    /// </summary>
    public readonly ref struct AnyMarker
    {
        private readonly EcsAspect.Builder _builder;
        public AnyMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }

        /// <summary>Caches the pool and adds it to the Any condition.</summary>
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.AnyPool<T>();
        }
    }

    /// <summary>
    /// Marker for declaring an Optional component — does not affect filtering, only caches the pool.
    /// Used as a shortcut for initializing pool fields.
    /// </summary>
    public readonly ref struct OptionalMarker
    {
        private readonly EcsAspect.Builder _builder;
        public OptionalMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }

        /// <summary>Caches the pool without adding any condition to the mask.</summary>
        public T GetInstance<T>() where T : IEcsPoolImplementation, new()
        {
            return _builder.OptionalPool<T>();
        }
    }

    /// <summary>
    /// Marker for declaring a world‑scoped singleton dependency.
    /// Used as a shortcut for initializing singleton fields.
    /// </summary>
    public readonly ref struct SingletonMarker
    {
        public readonly EcsAspect.Builder Builder;
        public SingletonMarker(EcsAspect.Builder builder)
        {
            Builder = builder;
        }

        /// <summary>Gets the singleton component instance from the world.</summary>
        public T Get<T>() where T : struct
        {
            return Builder.World.Get<T>();
        }
    }
    #endregion
}
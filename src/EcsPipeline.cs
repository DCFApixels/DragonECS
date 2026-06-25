#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.EcsConsts;

namespace DCFApixels.DragonECS
{
    /// <summary>
    /// Base marker interface for all ECS members (systems, modules, etc.).
    /// </summary>
    public interface IEcsMember { }

    /// <summary>
    /// Marker interface for members that are associated with a specific component type.
    /// </summary>
    public interface IEcsComponentMember : IEcsMember { }

    /// <summary>
    /// Interface for members that have a human‑readable name.
    /// </summary>
    public interface INamedMember
    {
        /// <summary>Gets the name of the member.</summary>
        string Name { get; }
    }

    /// <summary>
    /// Interface for systems that need a reference to their owning <see cref="EcsPipeline"/>.
    /// </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "...")]
    [MetaID("DragonECS_F064557C92010419AB677453893D00AE")]
    public interface IEcsPipelineMember : IEcsProcess
    {
        /// <summary>
        /// Gets the pipeline instance that owns this member. The setter is used internally by the pipeline
        /// to assign itself during initialization; systems should only read this property.
        /// </summary>
        EcsPipeline Pipeline { get; set; }
    }

    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "Container and engine for systems. Responsible for configuring the execution order of systems, providing a mechanism for messaging between systems, and a dependency injection mechanism.")]
    [MetaID("DragonECS_9F5A557C9201C5C3D9BCAC2FF1CC07D4")]
    public sealed partial class EcsPipeline
    {
        private readonly IConfigContainer _configs;
        private Injector _injector;

        private IEcsProcess[] _allSystems;
        private Dictionary<Type, Array> _processes = new Dictionary<Type, Array>();
        private Dictionary<Type, IEcsRunner> _runners = new Dictionary<Type, IEcsRunner>();
        private EcsRunRunner _runRunnerCache;

        private bool _isInit = false;
        private bool _isDestoryed = false;

#if DEBUG
        private static EcsProfilerMarker _initMarker = new EcsProfilerMarker("EcsPipeline.Init");
#endif

        #region Properties
        /// <summary>Gets the configuration container used by this pipeline.</summary>
        public IConfigContainer Configs
        {
            get { return _configs; }
        }

        /// <summary>Gets the dependency injector used to resolve system dependencies.</summary>
        public Injector Injector
        {
            get { return _injector; }
        }

        /// <summary>Gets a process wrapper containing all systems in the pipeline.</summary>
        public EcsProcess<IEcsProcess> AllSystems
        {
            get { return new EcsProcess<IEcsProcess>(_allSystems); }
        }

        /// <summary>Gets a read‑only dictionary of all registered runners by their interface type.</summary>
        public IReadOnlyDictionary<Type, IEcsRunner> AllRunners
        {
            get { return _runners; }
        }

        /// <summary>Indicates whether the pipeline has been initialized.</summary>
        public bool IsInit
        {
            get { return _isInit; }
        }

        /// <summary>Indicates whether the pipeline has been destroyed.</summary>
        public bool IsDestroyed
        {
            get { return _isDestoryed; }
        }
        #endregion

        #region Constructors
        /// <summary>Initializes a new pipeline with the specified systems and optional configuration.</summary>
        /// <param name="systems">The systems to include in the pipeline.</param>
        /// <param name="configs">Optional configuration container. If null, a default container is created.</param>
        /// <param name="injectionList">Optional list of injections to apply during initialization.</param>
        public EcsPipeline(ReadOnlySpan<IEcsProcess> systems, IConfigContainer configs = null, Injector.InjectionList injectionList = null) :
            this(systems.ToArray(), configs, injectionList)
        { }

        /// <summary>Initializes a new pipeline with the specified systems and optional configuration.</summary>
        /// <param name="systems">The systems to include in the pipeline.</param>
        /// <param name="configs">Optional configuration container. If null, a default container is created.</param>
        /// <param name="injectionList">Optional list of injections to apply during initialization.</param>
        public EcsPipeline(IEnumerable<IEcsProcess> systems, IConfigContainer configs = null, Injector.InjectionList injectionList = null) :
            this(systems.ToArray(), configs, injectionList)
        { }

        private EcsPipeline(IEcsProcess[] systems, IConfigContainer configs, Injector.InjectionList injectionList)
        {
            if (configs == null)
            {
                configs = new ConfigContainer();
            }
            if (injectionList == null)
            {
                injectionList = Injector.InjectionList._Empty_Internal;
            }

            _configs = configs;
            _allSystems = systems;

            var members = GetProcess<IEcsPipelineMember>();
            for (int i = 0; i < members.Length; i++)
            {
                members[i].Pipeline = this;
            }

            _injector = new Injector(this);
            injectionList.InitInjectTo(_injector, this);
        }
        ~EcsPipeline()
        {
            if (_isDestoryed) { return; }
            if (_isInit == false) { Init(); }
            Destroy();
        }
        #endregion

        #region GetProcess
        /// <summary>Returns a process wrapper containing all systems of type <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The process type to filter by.</typeparam>
        /// <returns>A process wrapper containing the matching systems.</returns>
        public EcsProcess<T> GetProcess<T>() where T : IEcsProcess
        {
            Type type = typeof(T);
            T[] result;
            if (_processes.TryGetValue(type, out Array array))
            {
                result = (T[])array;
            }
            else
            {
                result = CreateProcess<T>();
                _processes.Add(type, result);
            }
            return new EcsProcess<T>(result);
        }
        [ThreadStatic]
        private static IEcsProcess[] _buffer;
        private T[] CreateProcess<T>() where T : IEcsProcess
        {
            ArrayUtility.UpsizeWithoutCopy(ref _buffer, _allSystems.Length);
            int bufferLength = 0;
            for (int i = 0, iMax = _allSystems.Length; i < iMax; i++)
            {
                if (_allSystems[i] is T)
                {
                    _buffer[bufferLength++] = _allSystems[i];
                }
            }
            T[] result = new T[bufferLength];
            Array.Copy(_buffer, result, bufferLength);
            Array.Clear(_buffer, 0, bufferLength);
            return result;
        }
        #endregion

        #region GetRunner
        /// <summary>Gets or creates a runner instance of type <typeparamref name="TRunner"/>.</summary>
        /// <typeparam name="TRunner">The runner type to get or create.</typeparam>
        /// <returns>An instance of the requested runner.</returns>
        public TRunner GetRunnerInstance<TRunner>() where TRunner : EcsRunner, IEcsRunner, new()
        {
            Type runnerType = typeof(TRunner);
            if (_runners.TryGetValue(runnerType, out IEcsRunner result))
            {
                return (TRunner)result;
            }
            TRunner runnerInstance = new TRunner();
#if DEBUG
            EcsRunner.CheckRunnerTypeIsValide(runnerType, runnerInstance.Interface);
#endif
            runnerInstance.Init_Internal(this);
            _runners.Add(runnerType, runnerInstance);
            _runners.Add(runnerInstance.Interface, runnerInstance);
            Injector.ExtractAllTo(runnerInstance);

            // init after.
            Injector.Inject(runnerInstance);
            return runnerInstance;
        }

        /// <summary>Gets an existing runner by its interface type.</summary>
        /// <typeparam name="T">The interface type of the runner.</typeparam>
        /// <returns>The runner instance.</returns>
        /// <exception cref="Exception">Thrown if no matching runner is found.</exception>
        public T GetRunner<T>() where T : IEcsProcess
        {
            if (_runners.TryGetValue(typeof(T), out IEcsRunner result))
            {
                return (T)result;
            }
            Throw.Exception("No matching runner found.");
            return default;
        }

        /// <summary>Tries to get an existing runner by its interface type.</summary>
        /// <typeparam name="T">The interface type of the runner.</typeparam>
        /// <param name="runner">Outputs the runner instance if found.</param>
        /// <returns>True if a matching runner was found; otherwise false.</returns>
        public bool TryGetRunner<T>(out T runner) where T : IEcsProcess
        {
            if (_runners.TryGetValue(typeof(T), out IEcsRunner result))
            {
                runner = (T)result;
                return true;
            }
            runner = default;
            return false;
        }
        #endregion

        #region Internal
        internal void OnRunnerDestroy_Internal(IEcsRunner runner)
        {
            _runners.Remove(runner.Interface);
        }
        #endregion

        #region LifeCycle
        /// <summary>Initializes the pipeline by calling PreInit and Init runners.</summary>
        public void Init()
        {
            if (_isInit == true)
            {
                EcsDebug.PrintWarning($"This {nameof(EcsPipeline)} has already been initialized");
                return;
            }
#if DEBUG
            _initMarker.Begin();
#endif

            GetRunnerInstance<EcsPreInitRunner>().PreInit();
            GetRunnerInstance<EcsInitRunner>().Init();
            _runRunnerCache = GetRunnerInstance<EcsRunRunner>();

            _isInit = true;

            GC.Collect();
#if DEBUG
            _initMarker.End();
#endif
        }

        /// <summary>Executes the Run phase of the pipeline.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (!_isInit) { Throw.Pipeline_MethodCalledBeforeInitialisation(nameof(Run)); }
            if (_isDestoryed) { Throw.Pipeline_MethodCalledAfterDestruction(nameof(Run)); }
#endif
            _runRunnerCache.Run();
        }

        /// <summary>Destroys the pipeline and releases all resources.</summary>
        public void Destroy()
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (!_isInit) { Throw.Pipeline_MethodCalledBeforeInitialisation(nameof(Destroy)); }
#endif
            if (_isDestoryed)
            {
                EcsDebug.PrintWarning($"This {nameof(EcsPipeline)} has already been destroyed");
                return;
            }
            _isDestoryed = true;
            GetRunnerInstance<EcsDestroyRunner>().Destroy();
        }
        #endregion

        #region Builder
        /// <summary>Creates a new pipeline builder.</summary>
        /// <param name="config">Optional configuration container to use for the builder.</param>
        /// <returns>A new <see cref="Builder"/> instance.</returns>
        public static Builder New(IConfigContainerWriter config = null)
        {
            return new Builder(config);
        }
        #endregion
    }

    #region EcsModule
    /// <summary>
    /// Represents a module that can be imported into an <see cref="EcsPipeline"/>.
    /// </summary>
    public interface IEcsModule
    {
        /// <summary>Imports the module's systems into the pipeline builder.</summary>
        /// <param name="b">The pipeline builder to add systems to.</param>
        void Import(EcsPipeline.Builder b);
    }

    /// <summary>
    /// Base class for modules that also support dependency injection.
    /// </summary>
    /// <typeparam name="T">The concrete module type (used for injection).</typeparam>
    public abstract class EcsModule<T> : IEcsModule, IInjectionUnit, IEcsDefaultAddParams
    {
        AddParams IEcsDefaultAddParams.AddParams { get { return AddParams; } }

        /// <summary>Gets the default add parameters for systems added by this module.</summary>
        protected virtual AddParams AddParams { get { return default; } }

        /// <summary>Imports the module's systems into the pipeline builder.</summary>
        /// <param name="b">The pipeline builder to add systems to.</param>
        public abstract void Import(EcsPipeline.Builder b);
        void IInjectionUnit.InitInjectionNode(InjectionGraph nodes) { nodes.AddNode<T>(); }
        /// <summary>Initializes a new instance of the module.</summary>
        public EcsModule() { if (GetType() != typeof(T)) { Throw.UndefinedException(); } }
    }
    #endregion

    #region Extensions
    /// <summary>Provides extension methods for pipeline building and initialization.</summary>
    public static partial class EcsPipelineExtensions
    {
        /// <summary>Checks whether the pipeline is null or has been destroyed.</summary>
        /// <param name="self">The pipeline instance.</param>
        /// <returns>True if the pipeline is null or destroyed; otherwise false.</returns>
        public static bool IsNullOrDestroyed(this EcsPipeline self)
        {
            return self == null || self.IsDestroyed;
        }

        /// <summary>Adds a range of systems to the pipeline builder.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="range">The systems to add.</param>
        /// <param name="layerName">Optional layer name for the systems.</param>
        /// <returns>The builder instance for chaining.</returns>
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEnumerable<IEcsProcess> range, string layerName = null)
        {
            foreach (var item in range)
            {
                self.Add(item, layerName);
            }
            return self;
        }

        /// <summary>Adds a range of systems to the builder, skipping any that are already present.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="range">The systems to add.</param>
        /// <param name="layerName">Optional layer name for the systems.</param>
        /// <returns>The builder instance for chaining.</returns>
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEnumerable<IEcsProcess> range, string layerName = null)
        {
            foreach (var item in range)
            {
                self.AddUnique(item, layerName);
            }
            return self;
        }

        /// <summary>Builds the pipeline and immediately initializes it.</summary>
        /// <param name="self">The builder instance.</param>
        /// <returns>The initialized pipeline instance.</returns>
        public static EcsPipeline BuildAndInit(this EcsPipeline.Builder self)
        {
            EcsPipeline result = self.Build();
            result.Init();
            return result;
        }
    }
    #endregion

    #region SystemsLayerMarkerSystem
    /// <summary>
    /// Auxiliary system used to mark and organize pipeline layers.
    /// This system is automatically added to the pipeline during layer management.
    /// </summary>
    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.Black)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "An auxiliary type of system for dividing a pipeline into layers. This system is automatically added to the EcsPipeline.")]
    [MetaID("DragonECS_42596C7C9201D0B85D1335E6E4704B57")]
    public class SystemsLayerMarkerSystem : IEcsProcess
    {
        /// <summary>The fully qualified layer name, including namespace.</summary>
        public readonly string name;
        /// <summary>The namespace part of the layer name.</summary>
        public readonly string layerNameSpace;
        /// <summary>The layer name without namespace.</summary>
        public readonly string layerName;

        /// <summary>Initializes a new layer marker with the specified layer name.</summary>
        /// <param name="name">The fully qualified layer name (may include namespace).</param>
        public SystemsLayerMarkerSystem(string name)
        {
            this.name = name;
            int indexof = name.LastIndexOf('.');
            if (indexof > 0)
            {
                layerNameSpace = name.Substring(0, indexof + 1);
                layerName = name.Substring(indexof + 1);
            }
            else
            {
                layerNameSpace = string.Empty;
                layerName = name;
            }
        }
        /// <summary>Returns a string representation of the layer marker.</summary>
        public override string ToString() { return name; }
    }
    #endregion

    #region EcsProcess
    /// <summary>
    /// A read‑only collection of <see cref="IEcsProcess"/> systems.
    /// Provides a non‑generic view of systems in a pipeline.
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EcsProcessRaw : IReadOnlyCollection<IEcsProcess>
    {
        /// <summary>An empty process collection.</summary>
        public static readonly EcsProcessRaw Empty = new EcsProcessRaw(Array.Empty<IEcsProcess>());
        private readonly Array _systems;

        #region Properties
        /// <summary>Indicates whether the collection is null or empty.</summary>
        public bool IsNullOrEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems == null || _systems.Length <= 0; }
        }

        /// <summary>Gets the number of systems in the collection.</summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems.Length; }
        }
        int IReadOnlyCollection<IEcsProcess>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems.Length; }
        }

        /// <summary>Gets the system at the specified index.</summary>
        /// <param name="index">The zero‑based index.</param>
        /// <returns>The system at the given index.</returns>
        public IEcsProcess this[int index]
        {
            get { return (IEcsProcess)_systems.GetValue(index); }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsProcessRaw(Array systems)
        {
            _systems = systems;
        }
        #endregion

        #region Enumerator

        IEnumerator<IEcsProcess> IEnumerable<IEcsProcess>.GetEnumerator()
        {
            return ((IEnumerable<IEcsProcess>)(EcsProcess<IEcsProcess>)this).GetEnumerator();
        }
        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        public IEnumerator GetEnumerator()
        {
            return _systems.GetEnumerator();
        }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T[] GetSystems_Internal<T>()
        {
            return (T[])_systems;
        }
        #endregion

        #region DebuggerProxy
        internal class DebuggerProxy
        {
            private EcsProcessRaw _process;
            public IEnumerable<IEcsProcess> Systems
            {
                get
                {
                    return _process._systems.Cast<IEcsProcess>().ToArray();
                }
            }
            public int Count
            {
                get { return _process.Length; }
            }
            public DebuggerProxy(EcsProcessRaw process)
            {
                _process = process;
            }
        }
        #endregion
    }

    /// <summary>
    /// A read‑only collection of <typeparamref name="TProcess"/> systems.
    /// Provides a type-safe view of systems in a pipeline.
    /// </summary>
    [DebuggerTypeProxy(typeof(EcsProcess<>.DebuggerProxy))]
    public readonly struct EcsProcess<TProcess> : IReadOnlyCollection<TProcess>
        where TProcess : IEcsProcess
    {
        /// <summary>An empty process collection.</summary>
        public static readonly EcsProcess<TProcess> Empty = new EcsProcess<TProcess>(Array.Empty<TProcess>());
        private readonly TProcess[] _systems;

        #region Properties
        /// <summary>Indicates whether the collection is null or empty.</summary>
        public bool IsNullOrEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems == null || _systems.Length <= 0; }
        }
        /// <summary>Gets the number of systems in the collection.</summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems.Length; }
        }
        int IReadOnlyCollection<TProcess>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems.Length; }
        }
        /// <summary>Gets the system at the specified index.</summary>
        public TProcess this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems[index]; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsProcess(TProcess[] systems)
        {
            _systems = systems;
        }
        #endregion

        #region Converts
        /// <summary>Converts an <see cref="EcsProcessRaw"/> to <see cref="EcsProcess{TProcess}"/>.</summary>
        public static explicit operator EcsProcess<TProcess>(EcsProcessRaw raw)
        {
            return new EcsProcess<TProcess>(raw.GetSystems_Internal<TProcess>());
        }
        /// <summary>Converts an <see cref="EcsProcess{TProcess}"/> to <see cref="EcsProcessRaw"/>.</summary>
        public static implicit operator EcsProcessRaw(EcsProcess<TProcess> process)
        {
            return new EcsProcessRaw(process._systems);
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        public Enumerator GetEnumerator() { return new Enumerator(_systems); }
        IEnumerator<TProcess> IEnumerable<TProcess>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        /// <summary>Enumerates the systems in an <see cref="EcsProcess{TProcess}"/> collection.</summary>
        public struct Enumerator : IEnumerator<TProcess>
        {
            private readonly TProcess[] _systems;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            /// <summary>Initializes a new enumerator for the specified systems array.</summary>
            /// <param name="systems">The array of systems to enumerate.</param>
            public Enumerator(TProcess[] systems)
            {
                _systems = systems;
                _index = -1;
            }
            /// <summary>Gets the current system in the collection.</summary>
            public TProcess Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _systems[_index]; }
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            /// <summary>Advances the enumerator to the next system.</summary>
            /// <returns>True if there is a next system; otherwise false.</returns>
            public bool MoveNext() { return ++_index < _systems.Length; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            /// <summary>Sets the enumerator to its initial position.</summary>
            public void Reset() { _index = -1; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
        }
        #endregion

        #region DebuggerProxy
        internal class DebuggerProxy
        {
            private EcsProcess<TProcess> _process;
            public IEnumerable<IEcsProcess> Systems
            {
                get
                {
                    return _process._systems.Cast<IEcsProcess>().ToArray();
                }
            }
            public int Count
            {
                get { return _process.Length; }
            }
            public DebuggerProxy(EcsProcess<TProcess> process)
            {
                _process = process;
            }

        }
        #endregion
    }
    #endregion
}
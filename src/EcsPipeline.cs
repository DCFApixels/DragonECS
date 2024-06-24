using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsPipelineMember : IEcsProcess
    {
        EcsPipeline Pipeline { get; set; }
    }
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsSystemDefaultLayer : IEcsProcess
    {
        string Layer { get; }
    }
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsSystemDefaultOrder : IEcsProcess
    {
        int Order { get; }
    }
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Container and engine for systems. Responsible for configuring the execution order of systems, providing a mechanism for messaging between systems, and a dependency injection mechanism.")]
    public sealed partial class EcsPipeline
    {
        private readonly IConfigContainer _configs;
        private Injector.Builder _injectorBuilder;
        private Injector _injector;

        private IEcsProcess[] _allSystems;
        private Dictionary<Type, Array> _processes = new Dictionary<Type, Array>();
        private Dictionary<Type, IEcsRunner> _runners = new Dictionary<Type, IEcsRunner>();
        private EcsRunRunner _runRunnerCache;

        private bool _isInit = false;
        private bool _isDestoryed = false;

#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        private EcsProfilerMarker _initMarker = new EcsProfilerMarker("EcsPipeline.Init");
#endif

        #region Properties
        public IConfigContainer Configs
        {
            get { return _configs; }
        }
        public Injector Injector
        {
            get { return _injector; }
        }
        public EcsProcess<IEcsProcess> AllSystems
        {
            get { return new EcsProcess<IEcsProcess>(_allSystems); }
        }
        public IReadOnlyDictionary<Type, IEcsRunner> AllRunners
        {
            get { return _runners; }
        }
        public bool IsInit
        {
            get { return _isInit; }
        }
        public bool IsDestoryed
        {
            get { return _isDestoryed; }
        }
        #endregion

        #region Constructors
        private EcsPipeline(IConfigContainer configs, Injector.Builder injectorBuilder, IEcsProcess[] systems)
        {
            _configs = configs;
            _allSystems = systems;
            _injectorBuilder = injectorBuilder;
            _injectorBuilder.Inject(this);
        }
        #endregion

        #region GetProcess
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
                result = _allSystems.OfType<T>().ToArray();
                _processes.Add(type, result);
            }
            return new EcsProcess<T>(result);
        }
        #endregion

        #region GetRunner
        public TRunner GetRunnerInstance<TRunner>() where TRunner : EcsRunner, IEcsRunner, new()
        {
            Type runnerType = typeof(TRunner);
            if (_runners.TryGetValue(runnerType, out IEcsRunner result))
            {
                return (TRunner)result;
            }
            TRunner instance = new TRunner();
#if DEBUG
            EcsRunner.CheckRunnerTypeIsValide(runnerType, instance.Interface);
#endif
            instance.Init_Internal(this);
            _runners.Add(runnerType, instance);
            _runners.Add(instance.Interface, instance);
            return instance;
        }
        public T GetRunner<T>() where T : IEcsProcess
        {
            if (_runners.TryGetValue(typeof(T), out IEcsRunner result))
            {
                return (T)result;
            }
            Throw.Exception("No matching runner found.");
            return default;
        }
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
        public void Init()
        {
            if (_isInit == true)
            {
                EcsDebug.PrintWarning($"This {nameof(EcsPipeline)} has already been initialized");
                return;
            }
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            _initMarker.Begin();
#endif
            var members = GetProcess<IEcsPipelineMember>();
            for (int i = 0; i < members.Length; i++)
            {
                members[i].Pipeline = this;
            }
            _injector = _injectorBuilder.Build(this);
            _injectorBuilder = null;

            GetRunnerInstance<EcsPreInitRunner>().PreInit();
            GetRunnerInstance<EcsInitRunner>().Init();
            _runRunnerCache = GetRunnerInstance<EcsRunRunner>();

            _isInit = true;

            GC.Collect();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            _initMarker.End();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!_isInit) { Throw.Pipeline_MethodCalledBeforeInitialisation(nameof(Run)); }
            if (_isDestoryed) { Throw.Pipeline_MethodCalledAfterDestruction(nameof(Run)); }
#endif
            _runRunnerCache.Run();
        }
        public void Destroy()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
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
        public static Builder New(IConfigContainerWriter config = null)
        {
            return new Builder(config);
        }
        #endregion
    }

    public interface IEcsModule
    {
        void Import(EcsPipeline.Builder b);
    }

    #region Extensions
    public static partial class EcsPipelineExtensions
    {
        public static bool IsNullOrDestroyed(this EcsPipeline self)
        {
            return self == null || self.IsDestoryed;
        }
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEnumerable<IEcsProcess> range, string layerName = null)
        {
            foreach (var item in range)
            {
                self.Add(item, layerName);
            }
            return self;
        }
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEnumerable<IEcsProcess> range, string layerName = null)
        {
            foreach (var item in range)
            {
                self.AddUnique(item, layerName);
            }
            return self;
        }
        public static EcsPipeline BuildAndInit(this EcsPipeline.Builder self)
        {
            EcsPipeline result = self.Build();
            result.Init();
            return result;
        }
    }
    #endregion

    #region SystemsLayerMarkerSystem
    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.Black)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "An auxiliary type of system for dividing a pipeline into layers. This system is automatically added to the EcsPipeline.")]
    public class SystemsLayerMarkerSystem : IEcsProcess
    {
        public readonly string name;
        public SystemsLayerMarkerSystem(string name) { this.name = name; }
        public override string ToString() { return name; }
    }
    #endregion

    #region EcsProcess
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EcsProcessRaw : IEnumerable
    {
        private readonly Array _systems;

        #region Properties
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems.Length; }
        }
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

    [DebuggerTypeProxy(typeof(EcsProcess<>.DebuggerProxy))]
    public readonly struct EcsProcess<TProcess> : IReadOnlyCollection<TProcess>
        where TProcess : IEcsProcess
    {
        public readonly static EcsProcess<TProcess> Empty = new EcsProcess<TProcess>(Array.Empty<TProcess>());
        private readonly TProcess[] _systems;

        #region Properties
        public bool IsNullOrEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _systems == null || _systems.Length <= 0; }
        }
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
        public static explicit operator EcsProcess<TProcess>(EcsProcessRaw raw)
        {
            return new EcsProcess<TProcess>(raw.GetSystems_Internal<TProcess>());
        }
        public static implicit operator EcsProcessRaw(EcsProcess<TProcess> process)
        {
            return new EcsProcessRaw(process._systems);
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(_systems); }
        IEnumerator<TProcess> IEnumerable<TProcess>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<TProcess>
        {
            private readonly TProcess[] _systems;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(TProcess[] systems)
            {
                _systems = systems;
                _index = -1;
            }
            public TProcess Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _systems[_index]; }
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return ++_index < _systems.Length; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { _index = -1; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }
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
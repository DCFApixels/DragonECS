#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.EcsConsts;

namespace DCFApixels.DragonECS
{
    public interface IEcsMember { }
    public interface IEcsComponentMember : IEcsMember { }
    public interface INamedMember
    {
        string Name { get; }
    }

    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "...")]
    [MetaID("DragonECS_F064557C92010419AB677453893D00AE")]
    public interface IEcsPipelineMember : IEcsProcess
    {
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
        public bool IsDestroyed
        {
            get { return _isDestoryed; }
        }
        #endregion

        #region Constructors
        private EcsPipeline(IConfigContainer configs, Injector.Builder injectorBuilder, IEcsProcess[] systems)
        {
            _configs = configs;
            _allSystems = systems;
            injectorBuilder.Inject(this);

            var members = GetProcess<IEcsPipelineMember>();
            for (int i = 0; i < members.Length; i++)
            {
                members[i].Pipeline = this;
            }

            _injector = injectorBuilder.Build(this);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (!_isInit) { Throw.Pipeline_MethodCalledBeforeInitialisation(nameof(Run)); }
            if (_isDestoryed) { Throw.Pipeline_MethodCalledAfterDestruction(nameof(Run)); }
#endif
            _runRunnerCache.Run();
        }
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
        public static Builder New(IConfigContainerWriter config = null)
        {
            return new Builder(config);
        }
        #endregion
    }

    #region EcsModule
    public interface IEcsModule
    {
        void Import(EcsPipeline.Builder b);
    }
    public abstract class EcsModule<T> : IEcsModule, IInjectionUnit, IEcsDefaultAddParams
    {
        AddParams IEcsDefaultAddParams.AddParams { get { return AddParams; } }
        protected virtual AddParams AddParams { get { return default; } }
        public abstract void Import(EcsPipeline.Builder b);
        void IInjectionUnit.InitInjectionNode(InjectionGraph nodes) { nodes.AddNode<T>(); }
        public EcsModule() { if (GetType() != typeof(T)) { Throw.UndefinedException(); } }
    }
    #endregion

    #region Extensions
    public static partial class EcsPipelineExtensions
    {
        public static bool IsNullOrDestroyed(this EcsPipeline self)
        {
            return self == null || self.IsDestroyed;
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
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "An auxiliary type of system for dividing a pipeline into layers. This system is automatically added to the EcsPipeline.")]
    [MetaID("DragonECS_42596C7C9201D0B85D1335E6E4704B57")]
    public class SystemsLayerMarkerSystem : IEcsProcess
    {
        public readonly string name;
        public readonly string layerNameSpace;
        public readonly string layerName;
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
        public override string ToString() { return name; }
    }
    #endregion

    #region EcsProcess
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EcsProcessRaw : IReadOnlyCollection<IEcsProcess>
    {
        public static readonly EcsProcessRaw Empty = new EcsProcessRaw(Array.Empty<IEcsProcess>());
        private readonly Array _systems;

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
        int IReadOnlyCollection<IEcsProcess>.Count
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

        IEnumerator<IEcsProcess> IEnumerable<IEcsProcess>.GetEnumerator()
        {
            return ((IEnumerable<IEcsProcess>)(EcsProcess<IEcsProcess>)this).GetEnumerator();
        }
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
        public static readonly EcsProcess<TProcess> Empty = new EcsProcess<TProcess>(Array.Empty<TProcess>());
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
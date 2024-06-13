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
    [MetaDescription(EcsConsts.AUTHOR, "Container and engine for systems. Responsible for configuring the execution order of systems, providing a mechanism for messaging between systems, and a dependency injection mechanism.")]
    public sealed class EcsPipeline
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
        public class Builder
        {
            private const int KEYS_CAPACITY = 4;
            private HashSet<Type> _uniqueTypes;
            private readonly Dictionary<string, List<IEcsProcess>> _systems;
            private readonly string _basicLayer;
            public readonly LayerList Layers;
            private readonly Configurator _configurator;
            private readonly Injector.Builder _injector;
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            private EcsProfilerMarker _buildBarker = new EcsProfilerMarker("EcsPipeline.Build");
#endif
            private List<InitDeclaredRunner> _initDeclaredRunners = new List<InitDeclaredRunner>(4);

            public Configurator Configs
            {
                get { return _configurator; }
            }
            public Injector.Builder Injector
            {
                get { return _injector; }
            }
            public Builder(IConfigContainerWriter config = null)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                _buildBarker.Begin();
#endif
                if (config == null) { config = new ConfigContainer(); }
                _configurator = new Configurator(config, this);

                _injector = new Injector.Builder(this);
                _injector.AddNode<object>();
                _injector.AddNode<EcsWorld>();
                _injector.AddNode<EcsAspect>();
                _injector.AddNode<EcsPipeline>();

                _basicLayer = EcsConsts.BASIC_LAYER;
                Layers = new LayerList(this, _basicLayer);
                Layers.Insert(EcsConsts.BASIC_LAYER, EcsConsts.PRE_BEGIN_LAYER, EcsConsts.BEGIN_LAYER);
                Layers.InsertAfter(EcsConsts.BASIC_LAYER, EcsConsts.END_LAYER, EcsConsts.POST_END_LAYER);

                _uniqueTypes = new HashSet<Type>();
                _systems = new Dictionary<string, List<IEcsProcess>>(KEYS_CAPACITY);
            }
            public Builder AddRunner<TRunner>() where TRunner : EcsRunner, IEcsRunner, new()
            {
                _initDeclaredRunners.Add(new InitDeclaredRunner<TRunner>());
                return this;
            }
            public Builder Add(IEcsProcess system, string layerName = null)
            {
                AddInternal(system, layerName, false);
                return this;
            }
            public Builder AddUnique(IEcsProcess system, string layerName = null)
            {
                AddInternal(system, layerName, true);
                return this;
            }
            public Builder Remove<TSystem>()
            {
                _uniqueTypes.Remove(typeof(TSystem));
                foreach (var list in _systems.Values)
                {
                    list.RemoveAll(o => o is TSystem);
                }
                return this;
            }
            private void AddInternal(IEcsProcess system, string layerName, bool isUnique)
            {
                if (string.IsNullOrEmpty(layerName))
                {
                    layerName = system is IEcsSystemDefaultLayer defaultLayer ? defaultLayer.Layer : _basicLayer;
                }
                List<IEcsProcess> list;
                if (!_systems.TryGetValue(layerName, out list))
                {
                    list = new List<IEcsProcess> { new SystemsLayerMarkerSystem(layerName) };
                    _systems.Add(layerName, list);
                }
                if (_uniqueTypes.Add(system.GetType()) == false && isUnique)
                {
                    return;
                }
                list.Add(system);

                if (system is IEcsModule module)//если система одновременно явялется и системой и модулем то за один Add будет вызван Add и AddModule
                {
                    AddModule(module);
                }
            }
            public Builder AddModule(IEcsModule module)
            {
                module.Import(this);
                return this;
            }
            public EcsPipeline Build()
            {
                List<IEcsProcess> result = new List<IEcsProcess>(32);
                List<IEcsProcess> basicBlockList;
                if (_systems.TryGetValue(_basicLayer, out basicBlockList) == false)
                {
                    basicBlockList = new List<IEcsProcess>();
                }
                foreach (var item in _systems)
                {
                    if (!Layers.Contains(item.Key))
                        basicBlockList.AddRange(item.Value);
                }
                foreach (var item in Layers)
                {
                    if (_systems.TryGetValue(item, out var list))
                        result.AddRange(list);
                }
                EcsPipeline pipeline = new EcsPipeline(_configurator.Instance.GetContainer(), _injector, result.ToArray());
                foreach (var item in _initDeclaredRunners)
                {
                    item.Declare(pipeline);
                }
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                _buildBarker.End();
#endif
                return pipeline;
            }

            private abstract class InitDeclaredRunner
            {
                public abstract void Declare(EcsPipeline pipeline);
            }
            private class InitDeclaredRunner<T> : InitDeclaredRunner where T : EcsRunner, IEcsRunner, new()
            {
                public override void Declare(EcsPipeline pipeline)
                {
                    pipeline.GetRunnerInstance<T>();
                }
            }
            public class Configurator
            {
                private readonly IConfigContainerWriter _configs;
                private readonly Builder _builder;
                public Configurator(IConfigContainerWriter configs, Builder builder)
                {
                    _configs = configs;
                    _builder = builder;
                }
                public IConfigContainerWriter Instance
                {
                    get { return _configs; }
                }
                public Builder Set<T>(T value)
                {
                    _configs.Set(value);
                    return _builder;
                }
            }
            public class LayerList : IEnumerable<string>
            {
                private const string ADD_LAYER = nameof(ADD_LAYER); // автоматический слой нужный только для метода Add

                private Builder _source;
                private List<string> _layers;
                private string _basicLayerName;

                public LayerList(Builder source, string basicLayerName)
                {
                    _source = source;
                    _layers = new List<string>(16) { basicLayerName, ADD_LAYER };
                    _basicLayerName = basicLayerName;
                }

                public Builder Add(string newLayer) => Insert(ADD_LAYER, newLayer);
                public Builder Insert(string targetLayer, string newLayer)
                {
                    if (Contains(newLayer)) return _source;

                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    _layers.Insert(index, newLayer);
                    return _source;
                }
                public Builder InsertAfter(string targetLayer, string newLayer)
                {
                    if (Contains(newLayer)) return _source;

                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                        targetLayer = ADD_LAYER;

                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");

                    if (++index >= _layers.Count)
                        _layers.Add(newLayer);
                    else
                        _layers.Insert(index, newLayer);
                    return _source;
                }
                public Builder Move(string targetLayer, string movingLayer)
                {
                    _layers.Remove(movingLayer);
                    return Insert(targetLayer, movingLayer);
                }
                public Builder MoveAfter(string targetLayer, string movingLayer)
                {
                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                        targetLayer = ADD_LAYER;

                    _layers.Remove(movingLayer);
                    return InsertAfter(targetLayer, movingLayer);
                }

                public Builder Add(params string[] newLayers) => Insert(ADD_LAYER, newLayers);
                public Builder Insert(string targetLayer, params string[] newLayers)
                {
                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    _layers.InsertRange(index, newLayers.Where(o => !Contains(o)));
                    return _source;
                }
                public Builder InsertAfter(string targetLayer, params string[] newLayers)
                {
                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");

                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                        targetLayer = ADD_LAYER;

                    if (++index >= _layers.Count)
                        _layers.AddRange(newLayers.Where(o => !Contains(o)));
                    else
                        _layers.InsertRange(index, newLayers.Where(o => !Contains(o)));
                    return _source;
                }
                public Builder Move(string targetLayer, params string[] movingLayers)
                {
                    foreach (var movingLayer in movingLayers)
                        _layers.Remove(movingLayer);
                    return Insert(targetLayer, movingLayers);
                }
                public Builder MoveAfter(string targetLayer, params string[] movingLayers)
                {
                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                        targetLayer = ADD_LAYER;

                    foreach (var movingLayer in movingLayers)
                        _layers.Remove(movingLayer);
                    return InsertAfter(targetLayer, movingLayers);
                }

                public bool Contains(string layer) => _layers.Contains(layer);

                public List<string>.Enumerator GetEnumerator() => _layers.GetEnumerator();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => _layers.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _layers.GetEnumerator();
            }
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
        public SystemsLayerMarkerSystem(string name) => this.name = name;
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
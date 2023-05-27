using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public sealed class EcsPipeline
    {
        private IEcsSystem[] _allSystems;
        private Dictionary<Type, IEcsRunner> _runners;
        private IEcsRunProcess _runRunnerCache;

        private ReadOnlyCollection<IEcsSystem> _allSystemsSealed;
        private ReadOnlyDictionary<Type, IEcsRunner> _allRunnersSealed;

        private bool _isInit;
        private bool _isDestoryed;

        #region Properties
        public ReadOnlyCollection<IEcsSystem> AllSystems => _allSystemsSealed;
        public ReadOnlyDictionary<Type, IEcsRunner> AllRunners => _allRunnersSealed;
        public bool IsInit => _isInit;
        public bool IsDestoryed => _isDestoryed;
        #endregion

        #region Constructors
        private EcsPipeline(IEcsSystem[] systems)
        {
            _allSystems = systems;
            _runners = new Dictionary<Type, IEcsRunner>();

            _allSystemsSealed = new ReadOnlyCollection<IEcsSystem>(_allSystems);
            _allRunnersSealed = new ReadOnlyDictionary<Type, IEcsRunner>(_runners);

            _isInit = false;
            _isDestoryed = false;
        }
        #endregion

        #region Runners
        public T GetRunner<T>() where T : IEcsSystem
        {
            Type type = typeof(T);
            if (_runners.TryGetValue(type, out IEcsRunner result))
                return (T)result;
            result = (IEcsRunner)EcsRunner<T>.Instantiate(this);
            _runners.Add(type, result);
            return (T)result;
        }

        internal void OnRunnerDestroy(IEcsRunner runner)
        {
            _runners.Remove(runner.Interface);
        }
        #endregion

        #region LifeCycle
        public void Init()
        {
            if (_isInit == true)
            {
                EcsDebug.Print("[Warning]", $"This {nameof(EcsPipeline)} has already been initialized");
                return;
            }
            _isInit = true;

            var ecsPipelineInjectRunner = GetRunner<IEcsInject<EcsPipeline>>();
            ecsPipelineInjectRunner.Inject(this);
            EcsRunner.Destroy(ecsPipelineInjectRunner);
            var preInitRunner = GetRunner<IEcsPreInitProcess>();
            preInitRunner.PreInit(this);
            EcsRunner.Destroy(preInitRunner);
            var initRunner = GetRunner<IEcsInitProcess>();
            initRunner.Init(this);
            EcsRunner.Destroy(initRunner);

            _runRunnerCache = GetRunner<IEcsRunProcess>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            CheckBeforeInitForMethod(nameof(Run));
            CheckAfterDestroyForMethod(nameof(Run));
#endif
            _runRunnerCache.Run(this);
        }
        public void Destroy()
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            CheckBeforeInitForMethod(nameof(Run));
#endif
            if (_isDestoryed == true)
            {
                EcsDebug.Print("[Warning]", $"This {nameof(EcsPipeline)} has already been destroyed");
                return;
            }
            _isDestoryed = true;
            GetRunner<IEcsDestroyProcess>().Destroy(this);
        }
        #endregion

        #region StateChecks
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
        private void CheckBeforeInitForMethod(string methodName)
        {
            if (!_isInit)
                throw new MethodAccessException($"It is forbidden to call {methodName}, before initialization {nameof(EcsPipeline)}");
        }
        private void CheckAfterInitForMethod(string methodName)
        {
            if (_isInit)
                throw new MethodAccessException($"It is forbidden to call {methodName}, after initialization {nameof(EcsPipeline)}");
        }
        private void CheckAfterDestroyForMethod(string methodName)
        {
            if (_isDestoryed)
                throw new MethodAccessException($"It is forbidden to call {methodName}, after destroying {nameof(EcsPipeline)}");
        }
#endif
        #endregion

        #region Builder
        public static Builder New() => new Builder();
        public class Builder
        {
            private const int KEYS_CAPACITY = 4;
            private HashSet<Type> _uniqueTypes;
            private readonly Dictionary<string, List<IEcsSystem>> _systems;
            private readonly string _basicLayer;
            public readonly LayerList Layers;
            public Builder()
            {
                _basicLayer = EcsConsts.BASIC_LAYER;
                Layers = new LayerList(this, _basicLayer);
                Layers.Insert(EcsConsts.BASIC_LAYER, EcsConsts.PRE_BEGIN_LAYER, EcsConsts.BEGIN_LAYER);
                Layers.InsertAfter(EcsConsts.BASIC_LAYER, EcsConsts.END_LAYER, EcsConsts.POST_END_LAYER);
                _uniqueTypes = new HashSet<Type>();
                _systems = new Dictionary<string, List<IEcsSystem>>(KEYS_CAPACITY);
            }
            public Builder Add(IEcsSystem system, string layerName = null)
            {
                AddInternal(system, layerName, false);
                return this;
            }
            public Builder AddUnique(IEcsSystem system, string layerName = null)
            {
                AddInternal(system, layerName, true);
                return this;
            }
            public Builder Remove<TSystem>()
            {
                _uniqueTypes.Remove(typeof(TSystem));
                foreach (var list in _systems.Values)
                    list.RemoveAll(o => o is TSystem);
                return this;
            }
            private void AddInternal(IEcsSystem system, string layerName, bool isUnique)
            {
                if (layerName == null) layerName = _basicLayer;
                List<IEcsSystem> list;
                if (!_systems.TryGetValue(layerName, out list))
                {
                    list = new List<IEcsSystem> { new SystemsLayerMarkerSystem(layerName.ToString()) };
                    _systems.Add(layerName, list);
                }
                if ((_uniqueTypes.Add(system.GetType()) == false && isUnique))
                    return;
                list.Add(system);

                if (system is IEcsModule module)//если система одновременно явялется и системой и модулем то за один Add будет вызван Add и AddModule
                    AddModule(module);
            }
            public Builder AddModule(IEcsModule module)
            {
                module.ImportSystems(this);
                return this;
            }
            public EcsPipeline Build()
            {
                Add(new DeleteEmptyEntitesSystem(), EcsConsts.POST_END_LAYER);
                List<IEcsSystem> result = new List<IEcsSystem>(32);
                List<IEcsSystem> basicBlockList = _systems[_basicLayer];
                foreach (var item in _systems)
                {
                    if (!Layers.Has(item.Key))
                        basicBlockList.AddRange(item.Value);
                }
                foreach (var item in Layers)
                {
                    if(_systems.TryGetValue(item, out var list))
                        result.AddRange(list);
                }
                return new EcsPipeline(result.ToArray());
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
                    if (Has(newLayer)) return _source;

                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    _layers.Insert(index, newLayer);
                    return _source;
                }
                public Builder InsertAfter(string targetLayer, string newLayer)
                {
                    if (Has(newLayer)) return _source;

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
                    _layers.InsertRange(index, newLayers.Where(o => !Has(o)));
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
                        _layers.AddRange(newLayers.Where(o => !Has(o)));
                    else
                        _layers.InsertRange(index, newLayers.Where(o => !Has(o)));
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

                public bool Has(string layer) => _layers.Contains(layer);

                public List<string>.Enumerator GetEnumerator() => _layers.GetEnumerator();
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => _layers.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _layers.GetEnumerator();
            }
        }
        #endregion
    }

    public interface IEcsModule
    {
        void ImportSystems(EcsPipeline.Builder b);
    }

    #region Extensions
    public static class EcsPipelineExtensions
    {
        public static bool IsNullOrDestroyed(this EcsPipeline self) => self == null || self.IsDestoryed;
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEnumerable<IEcsSystem> range, string layerName = null)
        {
            foreach (var item in range) self.Add(item, layerName);
            return self;
        }
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEnumerable<IEcsSystem> range, string layerName = null)
        {
            foreach (var item in range) self.AddUnique(item, layerName);
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
}

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
    public interface IEcsPipelineMember
    {
        public EcsPipeline Pipeline { get; set; }
    }
    public sealed class EcsPipeline
    {
        private IEcsProcess[] _allSystems;
        private Dictionary<Type, IEcsRunner> _runners = new Dictionary<Type, IEcsRunner>();
        private IEcsRunProcess _runRunnerCache;

        private ReadOnlyCollection<IEcsProcess> _allSystemsSealed;
        private ReadOnlyDictionary<Type, IEcsRunner> _allRunnersSealed;

        private bool _isInit = false;
        private bool _isDestoryed = false;

        #region Properties
        public ReadOnlyCollection<IEcsProcess> AllSystems => _allSystemsSealed;
        public ReadOnlyDictionary<Type, IEcsRunner> AllRunners => _allRunnersSealed;
        public bool IsInit => _isInit;
        public bool IsDestoryed => _isDestoryed;
        #endregion

        #region Constructors
        private EcsPipeline(IEcsProcess[] systems)
        {
            _allSystems = systems;
            _allSystemsSealed = new ReadOnlyCollection<IEcsProcess>(_allSystems);
            _allRunnersSealed = new ReadOnlyDictionary<Type, IEcsRunner>(_runners);
        }
        #endregion

        #region GetSystems
        public T[] GetSystems<T>()
        {
            return _allSystems.OfType<T>().ToArray();
        }
        public int GetSystemsNoAllock<T>(ref T[] array)
        {
            int count = 0;
            for (int i = 0; i < _allSystems.Length; i++)
            {
                if (_allSystems is T targetSystem)
                {
                    if (array.Length <= count)
                    {
                        Array.Resize(ref array, array.Length << 1);
                    }
                    array[count++] = targetSystem;
                }
            }
            return count;
        }
        #endregion

        #region Runners
        public T GetRunner<T>() where T : IEcsProcess
        {
            Type type = typeof(T);
            if (_runners.TryGetValue(type, out IEcsRunner result))
            {
                return (T)result;
            }
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
                EcsDebug.PrintWarning($"This {nameof(EcsPipeline)} has already been initialized");
                return;
            }

            IEcsPipelineMember[] members = GetSystems<IEcsPipelineMember>();
            foreach (var member in members)
            {
                member.Pipeline = this;
            }

            var preInitRunner = GetRunner<IEcsPreInitProcess>();
            preInitRunner.PreInit();
            EcsRunner.Destroy(preInitRunner);
            var initRunner = GetRunner<IEcsInitProcess>();
            initRunner.Init();
            EcsRunner.Destroy(initRunner);

            _runRunnerCache = GetRunner<IEcsRunProcess>();
            _isInit = true;
            GC.Collect();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!_isInit) Throw.Pipeline_MethodCalledBeforeInitialisation(nameof(Run));
            if (_isDestoryed) Throw.Pipeline_MethodCalledAfterDestruction(nameof(Run));
#endif
            _runRunnerCache.Run();
        }
        public void Destroy()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!_isInit) Throw.Pipeline_MethodCalledBeforeInitialisation(nameof(Destroy));
#endif
            if (_isDestoryed)
            {
                EcsDebug.PrintWarning($"This {nameof(EcsPipeline)} has already been destroyed");
                return;
            }
            _isDestoryed = true;
            GetRunner<IEcsDestroyProcess>().Destroy();
        }
        #endregion

        #region Builder
        public static Builder New() => new Builder();
        public class Builder
        {
            private const int KEYS_CAPACITY = 4;
            private HashSet<Type> _uniqueTypes;
            private readonly Dictionary<string, List<IEcsProcess>> _systems;
            private readonly string _basicLayer;
            public readonly LayerList Layers;
            public Builder()
            {
                _basicLayer = EcsConsts.BASIC_LAYER;
                Layers = new LayerList(this, _basicLayer);
                Layers.Insert(EcsConsts.BASIC_LAYER, EcsConsts.PRE_BEGIN_LAYER, EcsConsts.BEGIN_LAYER);
                Layers.InsertAfter(EcsConsts.BASIC_LAYER, EcsConsts.END_LAYER, EcsConsts.POST_END_LAYER);
                _uniqueTypes = new HashSet<Type>();
                _systems = new Dictionary<string, List<IEcsProcess>>(KEYS_CAPACITY);
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
                    list.RemoveAll(o => o is TSystem);
                return this;
            }
            private void AddInternal(IEcsProcess system, string layerName, bool isUnique)
            {
                if (layerName == null) layerName = _basicLayer;
                List<IEcsProcess> list;
                if (!_systems.TryGetValue(layerName, out list))
                {
                    list = new List<IEcsProcess> { new SystemsLayerMarkerSystem(layerName.ToString()) };
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
                module.Import(this);
                return this;
            }
            public EcsPipeline Build()
            {
                Add(new EndFrameSystem(), EcsConsts.POST_END_LAYER);
                List<IEcsProcess> result = new List<IEcsProcess>(32);
                List<IEcsProcess> basicBlockList = _systems[_basicLayer];
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
        public static bool IsNullOrDestroyed(this EcsPipeline self) => self == null || self.IsDestoryed;
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEnumerable<IEcsProcess> range, string layerName = null)
        {
            foreach (var item in range) self.Add(item, layerName);
            return self;
        }
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEnumerable<IEcsProcess> range, string layerName = null)
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

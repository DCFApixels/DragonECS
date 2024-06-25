using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DCFApixels.DragonECS
{
    public sealed partial class EcsPipeline
    {
        public class Builder : IEcsModule
        {
            private const int KEYS_CAPACITY = 4;
            private const string BASIC_LAYER = EcsConsts.BASIC_LAYER;

            private readonly HashSet<Type> _uniqueTypes = new HashSet<Type>(32);
            private readonly Dictionary<string, SystemsList> _systems = new Dictionary<string, SystemsList>(KEYS_CAPACITY);
            private readonly List<InitDeclaredRunner> _initDeclaredRunners = new List<InitDeclaredRunner>(4);
            public readonly LayerList Layers;
            public readonly Injector.Builder Injector;
            public readonly Configurator Configs;

#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            private EcsProfilerMarker _buildBarker = new EcsProfilerMarker("EcsPipeline.Build");
#endif

            #region Constructors
            public Builder(IConfigContainerWriter config = null)
            {
                if (config == null) { config = new ConfigContainer(); }
                Configs = new Configurator(config, this);

                Injector = new Injector.Builder(this);
                Injector.AddNode<object>();
                Injector.AddNode<EcsWorld>();
                Injector.AddNode<EcsAspect>();
                Injector.AddNode<EcsPipeline>();

                Layers = new LayerList(this, BASIC_LAYER);
                Layers.Insert(EcsConsts.BASIC_LAYER, EcsConsts.PRE_BEGIN_LAYER, EcsConsts.BEGIN_LAYER);
                Layers.InsertAfter(EcsConsts.BASIC_LAYER, EcsConsts.END_LAYER, EcsConsts.POST_END_LAYER);
            }
            #endregion

            #region Add
            public Builder Add(IEcsProcess system, int? order = null)
            {
                return AddInternal(system, string.Empty, order, false);
            }
            public Builder Add(IEcsProcess system, string layerName, int? order = null)
            {
                return AddInternal(system, layerName, order, false);
            }
            public Builder AddUnique(IEcsProcess system, int? order = null)
            {
                return AddInternal(system, string.Empty, order, true);
            }
            public Builder AddUnique(IEcsProcess system, string layerName, int? order = null)
            {
                return AddInternal(system, layerName, order, true);
            }
            private Builder AddInternal(IEcsProcess system, string layerName, int? settedOrder, bool isUnique)
            {
                int order;
                if (settedOrder.HasValue)
                {
                    order = settedOrder.Value;
                }
                else
                {
                    order = system is IEcsSystemDefaultOrder defaultOrder ? defaultOrder.Order : 0;
                }

                if (string.IsNullOrEmpty(layerName))
                {
                    layerName = system is IEcsSystemDefaultLayer defaultLayer ? defaultLayer.Layer : BASIC_LAYER;
                }
                if (!_systems.TryGetValue(layerName, out SystemsList list))
                {
                    list = new SystemsList(layerName);
                    _systems.Add(layerName, list);
                }
                if (_uniqueTypes.Add(system.GetType()) == false && isUnique)
                {
                    return this;
                }
                list.Add(system, order, isUnique);

                if (system is IEcsModule module)//если система одновременно явялется и системой и модулем то за один Add будет вызван Add и AddModule
                {
                    AddModule(module);
                }

                return this;
            }
            #endregion

            #region Add other
            public Builder AddModule(IEcsModule module)
            {
                module.Import(this);
                return this;
            }
            public Builder AddRunner<TRunner>() where TRunner : EcsRunner, IEcsRunner, new()
            {
                _initDeclaredRunners.Add(new InitDeclaredRunner<TRunner>());
                return this;
            }
            void IEcsModule.Import(Builder into)
            {
                into.MergeWith(this);
            }
            private void MergeWith(Builder other)
            {
                Injector.Add(other.Injector);
                foreach (var declaredRunners in other._initDeclaredRunners)
                {
                    _initDeclaredRunners.Add(declaredRunners);
                }
                foreach (var config in other.Configs.Instance.GetAllConfigs())
                {
                    Configs.Instance.Set(config.Key, config.Value);
                }
                Layers.MergeWith(other.Layers);

                foreach (var otherPair in other._systems)
                {
                    if (_systems.TryGetValue(otherPair.Key, out SystemsList selfList) == false)
                    {
                        selfList = new SystemsList(otherPair.Key);
                        _systems.Add(otherPair.Key, selfList);
                    }
                    //selfList.AddList(otherPair.Value);
                    foreach (var otherSystem in otherPair.Value.Records)
                    {
                        AddInternal(otherSystem.system, otherPair.Key, otherSystem.order, otherSystem.isUnique);
                    }
                }

                //TODO добавить проверку уникальных систем
                //сливать множество уникальных нужно после слияния систем
                //_uniqueTypes.UnionWith(other._uniqueTypes);

            }
            #endregion

            #region Remove
            public Builder Remove<TSystem>()
            {
                _uniqueTypes.Remove(typeof(TSystem));
                foreach (var list in _systems.Values)
                {
                    list.RemoveAll<TSystem>();
                }
                return this;
            }
            #endregion

            #region Build
            public EcsPipeline Build()
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                _buildBarker.Begin();
#endif
                SystemsList basicBlockList;
                if (_systems.TryGetValue(BASIC_LAYER, out basicBlockList) == false)
                {
                    basicBlockList = new SystemsList(BASIC_LAYER);
                    _systems.Add(BASIC_LAYER, basicBlockList);
                }
                int allSystemsLength = 0;
                foreach (var item in _systems)
                {
                    if (item.Key == BASIC_LAYER)
                    {
                        continue;
                    }
                    if (!Layers.Contains(item.Key))
                    {
                        basicBlockList.AddList(item.Value);
                    }
                    else
                    {
                        allSystemsLength += item.Value.recordsCount;
                    }
                    item.Value.Sort();
                }
                allSystemsLength += basicBlockList.recordsCount;
                basicBlockList.Sort();

                IEcsProcess[] allSystems = new IEcsProcess[allSystemsLength];
                {
                    int i = 0;
                    foreach (var item in Layers)
                    {
                        if (_systems.TryGetValue(item, out var list))
                        {
                            for (int j = 0; j < list.recordsCount; j++)
                            {
                                allSystems[i++] = list.records[j].system;
                            }
                        }
                    }
                }

                EcsPipeline pipeline = new EcsPipeline(Configs.Instance.GetContainer(), Injector, allSystems);
                foreach (var item in _initDeclaredRunners)
                {
                    item.Declare(pipeline);
                }
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                _buildBarker.End();
#endif
                return pipeline;
            }
            #endregion

            #region InitDeclaredRunner
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
            #endregion

            #region Configurator
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
            #endregion

            #region LayerList
            public class LayerList : IEnumerable<string>
            {
                private const string ADD_LAYER = nameof(ADD_LAYER); // автоматический слой нужный только для метода Add

                private Builder _source;
                private List<string> _layers;
                private string _basicLayerName;

                public int Count
                {
                    get { return _layers.Count; }
                }
                public LayerList(Builder source, string basicLayerName)
                {
                    _source = source;
                    _layers = new List<string>(16) { basicLayerName, ADD_LAYER };
                    _basicLayerName = basicLayerName;
                }

                public Builder Add(string newLayer) { return Insert(ADD_LAYER, newLayer); }
                public Builder Insert(string targetLayer, string newLayer)
                {
                    if (Contains(newLayer)) { return _source; }

                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                    {
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    }
                    _layers.Insert(index, newLayer);
                    return _source;
                }
                public Builder InsertAfter(string targetLayer, string newLayer)
                {
                    if (Contains(newLayer)) { return _source; }

                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                    {
                        targetLayer = ADD_LAYER;
                    }

                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                    {
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    }

                    if (++index >= _layers.Count)
                    {
                        _layers.Add(newLayer);
                    }
                    else
                    {
                        _layers.Insert(index, newLayer);
                    }
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
                    {
                        targetLayer = ADD_LAYER;
                    }

                    _layers.Remove(movingLayer);
                    return InsertAfter(targetLayer, movingLayer);
                }

                public Builder Add(params string[] newLayers) { return Insert(ADD_LAYER, newLayers); }
                public Builder Insert(string targetLayer, params string[] newLayers)
                {
                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                    {
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    }
                    _layers.InsertRange(index, newLayers.Where(o => !Contains(o)));
                    return _source;
                }
                public Builder InsertAfter(string targetLayer, params string[] newLayers)
                {
                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                    {
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    }

                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                    {
                        targetLayer = ADD_LAYER;
                    }

                    if (++index >= _layers.Count)
                    {
                        _layers.AddRange(newLayers.Where(o => !Contains(o)));
                    }
                    else
                    {
                        _layers.InsertRange(index, newLayers.Where(o => !Contains(o)));
                    }
                    return _source;
                }
                public Builder Move(string targetLayer, params string[] movingLayers)
                {
                    foreach (var movingLayer in movingLayers)
                    {
                        _layers.Remove(movingLayer);
                    }
                    return Insert(targetLayer, movingLayers);
                }
                public Builder MoveAfter(string targetLayer, params string[] movingLayers)
                {
                    if (targetLayer == _basicLayerName) // нужно чтобы метод Add работал правильно. _basicLayerName и ADD_LAYER считается одним слоем, поэтому Before = _basicLayerName After = ADD_LAYER
                    {
                        targetLayer = ADD_LAYER;
                    }

                    foreach (var movingLayer in movingLayers)
                    {
                        _layers.Remove(movingLayer);
                    }
                    return InsertAfter(targetLayer, movingLayers);
                }

                public void MergeWith(LayerList other)
                {
                    HashSet<string> seen = new HashSet<string>();
                    List<string> result = new List<string>();

                    List<string> listA = _layers;
                    List<string> listB = other._layers;

                    foreach (string item in listA)
                    {
                        seen.Add(item);
                    }
                    foreach (string item in listB)
                    {
                        if (seen.Add(item) == false)
                        {
                            seen.Remove(item);
                        }
                    }

                    int i = 0, j = 0;
                    while (i < listA.Count || j < listB.Count)
                    {
                        while (i < listA.Count && seen.Contains(listA[i]))
                        {
                            result.Add(listA[i]);
                            i++;
                        }
                        while (j < listB.Count && seen.Contains(listB[j]))
                        {
                            result.Add(listB[j]);
                            j++;
                        }

                        if (i < listA.Count) { i++; }
                        if (j < listB.Count)
                        {
                            result.Add(listB[j]);
                            j++;
                        }
                    }

                    _layers = result;
                }

                public bool Contains(string layer) { return _layers.Contains(layer); }

                public List<string>.Enumerator GetEnumerator() { return _layers.GetEnumerator(); }
                IEnumerator<string> IEnumerable<string>.GetEnumerator() { return _layers.GetEnumerator(); }
                IEnumerator IEnumerable.GetEnumerator() { return _layers.GetEnumerator(); }
            }
            #endregion

            #region SystemsList
            private class SystemsList
            {
                public SystemRecord[] records = new SystemRecord[32];
                public int recordsCount = 0;
                public ReadOnlySpan<SystemRecord> Records { get { return new ReadOnlySpan<SystemRecord>(records, 1, recordsCount - 1); } }
                public SystemsList(string layerName)
                {
                    Add(new SystemsLayerMarkerSystem(layerName), int.MinValue, false);
                }
                public void AddList(SystemsList other)
                {
                    for (int i = 1; i < other.recordsCount; i++)
                    {
                        var otherRecord = other.records[i];
                        Add(otherRecord.system, otherRecord.order, otherRecord.isUnique);
                    }
                }
                public void Add(IEcsProcess system, int order, bool isUnique)
                {
                    if (records.Length <= recordsCount)
                    {
                        Array.Resize(ref records, recordsCount << 1);
                    }
                    records[recordsCount++] = new SystemRecord(system, order, isUnique);
                }
                public void RemoveAll<T>()
                {
                    //for (int i = 0; i < recordsCount; i++)
                    //{
                    //    if (records[i].system is T)
                    //    {
                    //        records[i] = records[--recordsCount];
                    //    }
                    //}

                    int freeIndex = 0;

                    while (freeIndex < recordsCount && (records[freeIndex].system is T) == false) { freeIndex++; }
                    if (freeIndex >= recordsCount) { return; }

                    int current = freeIndex + 1;
                    while (current < recordsCount)
                    {
                        while (current < recordsCount && (records[current].system is T)) { current++; }
                        if (current < recordsCount)
                        {
                            records[freeIndex++] = records[current++];
                        }
                    }
                    recordsCount = freeIndex;
                }
                public void Sort()
                {
                    //Игнорирую первую систему, так как это чисто система с названием слоя
                    Array.Sort(records, 1, recordsCount - 1);
                }
            }
            private readonly struct SystemRecord : IComparable<SystemRecord>
            {
                public readonly IEcsProcess system;
                public readonly int order;
                public readonly bool isUnique;
                public SystemRecord(IEcsProcess system, int order, bool isUnique)
                {
                    this.system = system;
                    this.order = order;
                    this.isUnique = isUnique;
                }
                public int CompareTo(SystemRecord other) { return order - other.order; }
            }
            #endregion
        }
    }
}
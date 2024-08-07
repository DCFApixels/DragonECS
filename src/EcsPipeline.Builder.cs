using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static DCFApixels.DragonECS.EcsConsts;

namespace DCFApixels.DragonECS
{
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "...")]
    public interface IEcsAddLayer
    {
        string Layer { get; }
    }
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "...")]
    public interface IEcsAddSortOrder
    {
        int SortOrder { get; }
    }
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "...")]
    public interface IEcsAddUnique { }
    public sealed partial class EcsPipeline
    {
        public class Builder : IEcsModule
        {
            private SystemRecord[] _systemRecords = new SystemRecord[256];
            private int _systemRecordsCount = 0;
            private int _systemRecordsInrement = 0;

            private readonly Dictionary<string, LayerSystemsList> _layerLists = new Dictionary<string, LayerSystemsList>(8);
            private readonly List<InitDeclaredRunner> _initDeclaredRunners = new List<InitDeclaredRunner>(4);

            public readonly LayerList Layers;
            public readonly Injector.Builder Injector;
            public readonly Configurator Configs;

            private string _defaultLayer = BASIC_LAYER;
            private int _defaultOrder = 0;

            #region Properties
            private ReadOnlySpan<SystemRecord> SystemRecords
            {
                get { return new ReadOnlySpan<SystemRecord>(_systemRecords, 0, _systemRecordsCount); }
            }
            #endregion

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

                Layers = new LayerList(this, PRE_BEGIN_LAYER, BEGIN_LAYER, BASIC_LAYER, END_LAYER, POST_END_LAYER);
            }
            #endregion

            #region Add
            public Builder Add(IEcsProcess system, int? sortOrder = null)
            {
                return AddSystem_Internal(system, string.Empty, sortOrder, false);
            }
            public Builder Add(IEcsProcess system, string layerName, int? sortOrder = null)
            {
                return AddSystem_Internal(system, layerName, sortOrder, false);
            }
            public Builder AddUnique(IEcsProcess system, int? sortOrder = null)
            {
                return AddSystem_Internal(system, string.Empty, sortOrder, true);
            }
            public Builder AddUnique(IEcsProcess system, string layerName, int? sortOrder = null)
            {
                return AddSystem_Internal(system, layerName, sortOrder, true);
            }
            private Builder AddSystem_Internal(IEcsProcess system, string layerName, int? settedSortOrder, bool isUnique)
            {
                int sortOrder;
                if (settedSortOrder.HasValue)
                {
                    sortOrder = settedSortOrder.Value;
                }
                else
                {
                    sortOrder = system is IEcsAddSortOrder defaultSortOrder ? defaultSortOrder.SortOrder : _defaultOrder;
                }
                if (string.IsNullOrEmpty(layerName))
                {
                    layerName = system is IEcsAddLayer defaultLayer ? defaultLayer.Layer : _defaultLayer;
                }
                AddRecord_Internal(system, layerName, sortOrder, isUnique, _systemRecordsInrement++);

                if (system is IEcsModule module)//если система одновременно явялется и системой и модулем то за один Add будет вызван Add и AddModule
                {
                    AddModule(module, layerName, settedSortOrder);
                }

                return this;
            }
            private void AddRecord_Internal(IEcsProcess system, string layer, int sortOrder, bool isUnique, int addOrder)
            {
                SystemRecord record = new SystemRecord(system, layer, addOrder, sortOrder, isUnique);
                if (_layerLists.TryGetValue(layer, out LayerSystemsList list) == false)
                {
                    list = new LayerSystemsList(layer);
                    _layerLists.Add(layer, list);
                }
                list.lasyInitSystemsCount++;
                if (_systemRecords.Length <= _systemRecordsCount)
                {
                    Array.Resize(ref _systemRecords, _systemRecordsCount << 1);
                }
                _systemRecords[_systemRecordsCount++] = record;
            }

            #endregion

            #region AddModule
            public Builder AddModule(IEcsModule module, int? sortOrder = null)
            {
                return AddModule_Internal(module, string.Empty, sortOrder, false);
            }
            public Builder AddModule(IEcsModule module, string layerName, int? sortOrder = null)
            {
                return AddModule_Internal(module, layerName, sortOrder, false);
            }
            public Builder AddModuleUnique(IEcsModule module, int? sortOrder = null)
            {
                return AddModule_Internal(module, string.Empty, sortOrder, true);
            }
            public Builder AddModuleUnique(IEcsModule module, string layerName, int? sortOrder = null)
            {
                return AddModule_Internal(module, layerName, sortOrder, true);
            }
            public Builder AddModule_Internal(IEcsModule module, string layerName, int? settedSortOrder, bool isUnique)
            {
                string prevLayer = _defaultLayer;
                int prevSortOrder = _defaultOrder;

                if (settedSortOrder.HasValue)
                {
                    _defaultOrder = settedSortOrder.Value;
                }
                else
                {
                    _defaultOrder = module is IEcsAddSortOrder defaultSortOrder ? defaultSortOrder.SortOrder : 0;
                }
                if (string.IsNullOrEmpty(layerName))
                {
                    _defaultLayer = module is IEcsAddLayer defaultLayer ? defaultLayer.Layer : BASIC_LAYER;
                }
                else
                {
                    _defaultLayer = layerName;
                }

                module.Import(this);
                _defaultLayer = prevLayer;
                _defaultOrder = prevSortOrder;
                return this;
            }
            #endregion

            #region Add other
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

                //_systemRecordsInrement + otherRecord.addOrder смещает порядок так что новые системы встают в конец очереди, но сохраняют порядок addOrder
                foreach (var otherRecord in other.SystemRecords)
                {
                    AddRecord_Internal(otherRecord.system, otherRecord.layer, otherRecord.sortOrder, otherRecord.isUnique, _systemRecordsInrement + otherRecord.addOrder);
                }
                _systemRecordsInrement += other._systemRecordsInrement;
            }
            #endregion

            #region Remove
            private void RemoveAt(int index)
            {
                ref var slot = ref _systemRecords[index];
                _layerLists[slot.layer].lasyInitSystemsCount--;
                slot = _systemRecords[--_systemRecordsCount];
            }
            public Builder Remove<TSystem>()
            {
                for (int i = 0; i < _systemRecordsCount; i++)
                {
                    if (_systemRecords[i].system is TSystem)
                    {
                        RemoveAt(i--);
                    }
                }
                return this;
            }
            #endregion

            #region Build
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            private EcsProfilerMarker _buildBarker = new EcsProfilerMarker("EcsPipeline.Build");
#endif
            public EcsPipeline Build()
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                _buildBarker.Begin();
#endif
                LayerSystemsList basicLayerList;
                if (_layerLists.TryGetValue(BASIC_LAYER, out basicLayerList) == false)
                {
                    basicLayerList = new LayerSystemsList(BASIC_LAYER);
                    _layerLists.Add(BASIC_LAYER, basicLayerList);
                }

                HashSet<Type> uniqueSystemsSet = new HashSet<Type>();

                int allSystemsLength = 0;
                foreach (var item in _layerLists)
                {
                    if (item.Key == BASIC_LAYER) { continue; }
                    if (!Layers.Contains(item.Key))
                    {
                        basicLayerList.lasyInitSystemsCount += item.Value.lasyInitSystemsCount;
                    }
                    else
                    {
                        item.Value.Init();
                        allSystemsLength += item.Value.lasyInitSystemsCount + 1;
                    }
                }
                allSystemsLength += basicLayerList.lasyInitSystemsCount + 1;
                basicLayerList.Init();

                for (int i = 0, iMax = _systemRecordsCount; i < iMax; i++)
                {
                    ref var record = ref _systemRecords[i];
                    var list = _layerLists[record.layer];
                    if (list.IsInit == false)
                    {
                        list = basicLayerList;
                    }
                    if (record.isUnique == false || uniqueSystemsSet.Add(record.system.GetType()))
                    {
                        list.Add(record.system, record.addOrder, record.sortOrder, record.isUnique);
                    }
                }


                IEcsProcess[] allSystems = new IEcsProcess[allSystemsLength];
                {
                    int i = 0;
                    foreach (var item in Layers)
                    {
                        if (_layerLists.TryGetValue(item, out var list) && list.IsInit)
                        {
                            list.Sort();
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

                public int Count { get { return _layers.Count; } }
                public object this[int index] { get { return _layers[index]; } }

                public LayerList(Builder source, string basicLayerName)
                {
                    _source = source;
                    _layers = new List<string>(16) { basicLayerName, ADD_LAYER };
                    _basicLayerName = basicLayerName;
                }
                public LayerList(Builder source, string preBeginlayer, string beginlayer, string basicLayer, string endLayer, string postEndLayer)
                {
                    _source = source;
                    _layers = new List<string>(16) { preBeginlayer, beginlayer, basicLayer, ADD_LAYER, endLayer, postEndLayer };
                    _basicLayerName = basicLayer;
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
                public void MergeWith(IReadOnlyList<string> other)
                {
                    HashSet<string> seen = new HashSet<string>();
                    List<string> result = new List<string>();

                    List<string> listA = _layers;
                    IReadOnlyList<string> listB = other;

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
                public void MergeWith(LayerList other)
                {
                    MergeWith(other._layers);
                }

                public bool Contains(string layer) { return _layers.Contains(layer); }

                public List<string>.Enumerator GetEnumerator() { return _layers.GetEnumerator(); }
                IEnumerator<string> IEnumerable<string>.GetEnumerator() { return _layers.GetEnumerator(); }
                IEnumerator IEnumerable.GetEnumerator() { return _layers.GetEnumerator(); }
            }
            #endregion

            #region SystemsList
            private class LayerSystemsList
            {
                public int lasyInitSystemsCount = 0;

                public Item[] records = null;
                public int recordsCount = 0;

                //отслеживание осортированности систем
                private int _lastSortOrder;
                private int _lastAddOrder;
                private bool _isSorted = true;

                private string _layerName;

                public bool IsInit { get { return records != null; } }
                public bool IsSorted { get { return _isSorted; } }
                public ReadOnlySpan<Item> Records { get { return new ReadOnlySpan<Item>(records, 1, recordsCount - 1); } }
                public LayerSystemsList(string layerName) { _layerName = layerName; }
                public void Init()
                {
                    if (IsInit) { Throw.UndefinedException(); }

                    records = new Item[lasyInitSystemsCount + 1];
                    Add(new SystemsLayerMarkerSystem(_layerName), int.MinValue, int.MinValue, false);
                }
                public void AddList(LayerSystemsList other)
                {
                    for (int i = 1; i < other.recordsCount; i++)
                    {
                        var otherRecord = other.records[i];
                        AddItem_Internal(otherRecord);
                    }
                }
                public void Add(IEcsProcess system, int addOrder, int sortOrder, bool isUnique)
                {
                    AddItem_Internal(new Item(system, addOrder, sortOrder, isUnique));
                }
                private void AddItem_Internal(Item item)
                {
                    if (_isSorted)
                    {
                        if (recordsCount <= 1)
                        {
                            _lastSortOrder = item.sortOrder;
                            _lastAddOrder = item.addOrder;
                        } 
                        else if (_lastSortOrder > item.sortOrder || _lastAddOrder > item.addOrder)
                        {
                            _isSorted = false;
                        }
                        else
                        {
                            _lastSortOrder = item.sortOrder;
                            _lastAddOrder = item.addOrder;
                        }
                    }

                    if (records.Length <= recordsCount)
                    {
                        Array.Resize(ref records, recordsCount << 1);
                    }
                    records[recordsCount++] = item;
                }
                public void RemoveAll<T>()
                {
                    for (int i = 0; i < recordsCount; i++)
                    {
                        if (records[i].system is T)
                        {
                            records[i] = records[--recordsCount];
                        }
                    }
                    _isSorted = false;
                }
                public void Sort()
                {
                    if (_isSorted) { return; }
                    //Игнорирую первую систему, так как это чисто система с названием слоя
                    Array.Sort(records, 1, recordsCount - 1);
                    _isSorted = true;
                    _lastSortOrder = records[recordsCount - 1].sortOrder;
                    _lastAddOrder = records[recordsCount - 1].addOrder;
                }
            }
            private readonly struct Item : IComparable<Item>
            {
                public readonly IEcsProcess system;
                public readonly int addOrder;
                public readonly int sortOrder;
                public readonly bool isUnique;
                public Item(IEcsProcess system, int addOrder, int sortOrder, bool isUnique)
                {
                    this.system = system;
                    this.addOrder = addOrder;
                    this.sortOrder = sortOrder;
                    this.isUnique = isUnique;
                }
                public int CompareTo(Item other)
                {
                    int c = sortOrder - other.sortOrder;
                    return c == 0 ? addOrder - other.addOrder : c;
                }
            }
            #endregion

            #region SerializableTemplate
            public EcsPipelineTemplate GenerateSerializableTemplate()
            {
                Array.Sort(_systemRecords, 0, _systemRecordsCount);
                var records = SystemRecords;
                EcsPipelineTemplate result = new EcsPipelineTemplate();
                result.layers = new string[Layers.Count];
                result.systems = new EcsPipelineTemplate.SystemRecord[records.Length];
                for (int i = 0; i < records.Length; i++)
                {
                    var r = records[i];
                    result.systems[i] = new EcsPipelineTemplate.SystemRecord(r.system, r.layer, r.sortOrder, r.isUnique);
                }

                return result;
            }
            #endregion

            private readonly struct SystemRecord : IComparable<SystemRecord>
            {
                public readonly IEcsProcess system;
                public readonly string layer;
                public readonly int addOrder;
                public readonly int sortOrder;
                public readonly bool isUnique;
                public SystemRecord(IEcsProcess system, string layer, int addOrder, int sortOrder, bool isUnique)
                {
                    this.system = system;
                    this.layer = layer;
                    this.addOrder = addOrder;
                    this.sortOrder = sortOrder;
                    this.isUnique = isUnique;
                }
                public int CompareTo(SystemRecord other)
                {
                    int c = sortOrder - other.sortOrder;
                    return c == 0 ? addOrder - other.addOrder : c;
                }
            }
        }
    }
}
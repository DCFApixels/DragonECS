#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using static DCFApixels.DragonECS.EcsConsts;

namespace DCFApixels.DragonECS
{
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaDescription(AUTHOR, "...")]
    [MetaID("DragonECS_FC38597C9201C15D1A14D133237BD67F")]
    public interface IEcsDefaultAddParams
    {
        AddParams AddParams { get; }
    }

    public sealed partial class EcsPipeline
    {
        public class Builder : IEcsModule
        {
            private SystemNode[] _systemNodes = new SystemNode[256];
            private int _startIndex = -1;
            private int _endIndex = -1;
            private int _systemNodesCount = 0;
            private int _freeIndex = -1;
            private int _freeNodesCount = 0;

            private readonly Dictionary<string, LayerSystemsList> _layerLists = new Dictionary<string, LayerSystemsList>(8);
            private readonly List<InitDeclaredRunner> _initDeclaredRunners = new List<InitDeclaredRunner>(4);

            public readonly LayerList Layers;
            public readonly Injector.Builder Injector;
            public readonly Configurator Configs;

            private AddParams _defaultAddParams = new AddParams(BASIC_LAYER, 0, false);

            private HashSet<Type> _uniqueSystemsSet = new HashSet<Type>();

            #region Properties
            //private ReadOnlySpan<SystemNode> SystemRecords
            //{
            //    get { return new ReadOnlySpan<SystemNode>(_systemNodes, 0, _systemNodesCount); }
            //}
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

            #region Add IEcsProcess
            public Builder Add(IEcsProcess system, AddParams parameters)
            {
                return AddSystem_Internal(system, parameters);
            }
            private IEcsProcess _systemModule;
            private bool _systemModuleAdded;
            private Builder AddSystem_Internal(IEcsProcess system, AddParams settedAddParams)
            {
                AddParams prms = _defaultAddParams;
                if (system is IEcsDefaultAddParams overrideInterface)
                {
                    prms = prms.Overwrite(overrideInterface.AddParams);
                }
                prms = prms.Overwrite(settedAddParams);

                // Если система одновременно явялется и системой и модулем то сначала будет вызван IEcsModule
                // При этом дается возможность ручной установки порядка импорта системы вызовом Add(this)
                if (_systemModule == system)
                {
                    _systemModuleAdded = true;
                }
                else
                {
                    if (system is IEcsModule module)
                    {
                        IEcsProcess systemModulePrev = _systemModule;
                        bool systemModuleAddedPrev = _systemModuleAdded;

                        _systemModule = system;
                        _systemModuleAdded = false;
                        int importHeadIndex = _endIndex;
                        AddModule_Internal(module, prms);
                        if (_systemModuleAdded == false)
                        { //Если система не была добавлена вручную, то она будет добавлена перед тем что было импортировано через IEcsModule
                            InsertAfterNode_Internal(importHeadIndex, system, prms.layerName, prms.sortOrder, prms.isUnique);
                        }

                        _systemModule = systemModulePrev;
                        _systemModuleAdded = systemModuleAddedPrev;
                        return this;
                    }
                }

                AddNode_Internal(system, prms.layerName, prms.sortOrder, prms.isUnique);
                return this;
            }
            private void AddNode_Internal(IEcsProcess system, string layer, int sortOrder, bool isUnique)
            {
                InsertAfterNode_Internal(_endIndex, system, layer, sortOrder, isUnique);
            }
            private void InsertAfterNode_Internal(int insertAfterIndex, IEcsProcess system, string layer, int sortOrder, bool isUnique)
            {
                //TODO нужно потестить
                if (isUnique && _uniqueSystemsSet.Add(system.GetType()) == false)
                {
                    //EcsDebug.PrintWarning($"The pipeline already contains a unique instance of {system.GetType().Name}");
                    return;
                }

                if (string.IsNullOrEmpty(layer))
                {
                    layer = BASIC_LAYER;
                }

                SystemNode record = new SystemNode(system, layer, sortOrder, isUnique);
                int newIndex;
                if (_freeNodesCount <= 0)
                {
                    if (_systemNodes.Length <= _systemNodesCount)
                    {
                        Array.Resize(ref _systemNodes, _systemNodes.Length << 1);
                    }
                    newIndex = _systemNodesCount;
                    _systemNodes[newIndex] = record;
                }
                else
                {
                    _freeNodesCount--;
                    newIndex = _freeIndex;
                    _freeIndex = _systemNodes[_freeIndex].next;
                }
                _systemNodesCount++;

                _systemNodes[newIndex] = record;
                if (_systemNodesCount == 1)
                {
                    _startIndex = newIndex;
                }
                else
                {
                    _systemNodes[newIndex].next = _systemNodes[insertAfterIndex].next;
                    _systemNodes[insertAfterIndex].next = newIndex;
                }
                if (insertAfterIndex == _endIndex)
                {
                    _endIndex = newIndex;
                }

                if (_layerLists.TryGetValue(layer, out LayerSystemsList list) == false)
                {
                    list = new LayerSystemsList(layer);
                    _layerLists.Add(layer, list);
                }
                list.lasyInitSystemsCount++;
            }
            #endregion

            #region AddModule IEcsModule
            public Builder AddModule(IEcsModule module, AddParams parameters)
            {
                if (module is IEcsProcess system)
                {
                    return AddSystem_Internal(system, parameters);
                }
                return AddModule_Internal(module, parameters);
            }
            private Builder AddModule_Internal(IEcsModule module, AddParams settedAddParams)
            {
                if (settedAddParams.flags.IsNoImport() == false)
                {
                    AddParams prms = _defaultAddParams;
                    if (module is IEcsDefaultAddParams overrideInterface)
                    {
                        prms = prms.Overwrite(overrideInterface.AddParams);
                    }
                    var oldDefaultAddParams = _defaultAddParams;
                    _defaultAddParams = prms.Overwrite(settedAddParams);

                    module.Import(this);
                    _defaultAddParams = oldDefaultAddParams;
                }

                Injector.Inject(module);
                return this;
            }
            #endregion

            #region Add Raw
            public Builder Add(object raw, AddParams parameters)
            {
                return AddRaw_Internal(raw, parameters);
            }
            private Builder AddRaw_Internal(object raw, AddParams settedAddParams)
            {
                switch (raw)
                {
                    case IEcsProcess system: return AddSystem_Internal(system, settedAddParams);
                    case IEcsModule module: return AddModule_Internal(module, settedAddParams);
                    default: Throw.ArgumentException($"{raw.GetMeta().TypeName} Unsupported type"); return this;
                }
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

                foreach (ref readonly SystemNode otherRecord in new LinkedListIterator<SystemNode>(_systemNodes, _systemNodesCount, _startIndex))
                {
                    AddNode_Internal(otherRecord.system, otherRecord.layerName, otherRecord.sortOrder, otherRecord.isUnique);
                }
            }
            #endregion

            #region Remove
            private void RemoveAt(int prevIndex, int removedNodeIndex)
            {
                ref var removedeNode = ref _systemNodes[removedNodeIndex];
                _layerLists[removedeNode.layerName].lasyInitSystemsCount--;
                _systemNodes[prevIndex].next = removedeNode.next;
                removedeNode = default;

                if (_endIndex == removedNodeIndex)
                {
                    _endIndex = prevIndex;
                }
                if (_freeNodesCount > 0)
                {
                    removedeNode.next = _freeIndex;
                }
                _freeIndex = removedNodeIndex;
                _freeNodesCount++;
                _systemNodesCount--;
            }
            public Builder Remove<TSystem>()
            {
                _uniqueSystemsSet.Remove(typeof(TSystem));

                if (_systemNodesCount <= 1)
                {
                    if (_systemNodesCount == 1 && _systemNodes[0].system is TSystem)
                    {
                        _systemNodesCount = 0;
                    }
                    return this;
                }

                int enumIndex = _startIndex;
                for (int i = 1; i < _systemNodesCount; i++)
                {
                    int nextIndex = _systemNodes[enumIndex].next;
                    if (_systemNodes[nextIndex].system is TSystem)
                    {
                        RemoveAt(enumIndex, nextIndex);
                        nextIndex = _systemNodes[enumIndex].next;
                    }
                    enumIndex = nextIndex;
                }
                return this;
            }
            #endregion

            #region Build
#if DEBUG
            private static EcsProfilerMarker _buildMarker = new EcsProfilerMarker("EcsPipeline.Build");
#endif
            public EcsPipeline Build()
            {
#if DEBUG
                _buildMarker.Begin();
#endif
                var it = new LinkedListIterator<SystemNode>(_systemNodes, _systemNodesCount, _startIndex);

                LayerSystemsList basicLayerList;
                if (_layerLists.TryGetValue(BASIC_LAYER, out basicLayerList) == false)
                {
                    basicLayerList = new LayerSystemsList(BASIC_LAYER);
                    _layerLists.Add(BASIC_LAYER, basicLayerList);
                }

                int allSystemsLength = 0;
                foreach (var item in _layerLists)
                {
                    if (item.Key == BASIC_LAYER) { continue; }
                    if (Layers.Contains(item.Key))
                    {
                        item.Value.Init();
                        allSystemsLength += item.Value.lasyInitSystemsCount + 1;
                    }
                    else
                    {
                        basicLayerList.lasyInitSystemsCount += item.Value.lasyInitSystemsCount;
                    }
                }
                allSystemsLength += basicLayerList.lasyInitSystemsCount + 1;
                basicLayerList.Init();

                foreach (ref readonly SystemNode node in it)
                {
                    var list = _layerLists[node.layerName];
                    if (list.IsInit == false)
                    {
                        list = basicLayerList;
                    }
                    list.Add(node.system, node.sortOrder, node.isUnique);

                    //if (node.isUnique == false || uniqueSystemsSet.Add(node.system.GetType()))
                    //{
                    //    list.Add(node.system, node.sortOrder, node.isUnique);
                    //}
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
#if DEBUG
                _buildMarker.End();
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
                private Builder _source;
                private List<string> _layers;
                private string _basicLayerName;
                private string _addLayerName;

                #region Properties
                public int Count { get { return _layers.Count; } }
                public object this[int index] { get { return _layers[index]; } }
                #endregion

                #region Constructors
                public LayerList(Builder source, string basicLayerName)
                {
                    _source = source;
                    _layers = new List<string>(16) { basicLayerName };
                    _basicLayerName = basicLayerName;
                    _addLayerName = _basicLayerName;
                }
                public LayerList(Builder source, string preBeginlayer, string beginlayer, string basicLayer, string endLayer, string postEndLayer)
                {
                    _source = source;
                    _layers = new List<string>(16) { preBeginlayer, beginlayer, basicLayer, endLayer, postEndLayer };
                    _basicLayerName = basicLayer;
                    _addLayerName = _basicLayerName;
                }
                #endregion

                #region Edit

                #region Single
                public Builder Add(string newLayer)
                {
                    InsertAfter(_addLayerName, newLayer);
                    _addLayerName = newLayer;
                    return _source;
                }
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

                    int index = _layers.IndexOf(targetLayer);
                    if (index < 0)
                    {
                        throw new KeyNotFoundException($"Layer {targetLayer} not found");
                    }

                    _layers.Insert(++index, newLayer);
                    return _source;
                }
                public Builder Move(string targetLayer, string movingLayer)
                {
                    _layers.Remove(movingLayer);
                    return Insert(targetLayer, movingLayer);
                }
                public Builder MoveAfter(string targetLayer, string movingLayer)
                {
                    _layers.Remove(movingLayer);
                    return InsertAfter(targetLayer, movingLayer);
                }
                #endregion

                #region Range
                public Builder Add(params string[] newLayers)
                {
                    InsertAfter(_addLayerName, newLayers);
                    _addLayerName = newLayers[newLayers.Length - 1];
                    return _source;
                }
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

                    _layers.InsertRange(++index, newLayers.Where(o => !Contains(o)));
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
                    foreach (var movingLayer in movingLayers)
                    {
                        _layers.Remove(movingLayer);
                    }
                    return InsertAfter(targetLayer, movingLayers);
                }
                #endregion

                #endregion

                #region MergeWith
                private static bool CheckOverlapsOrder(List<string> listA, IReadOnlyList<string> listB)
                {
                    int lastIndexof = 0;
                    for (int i = 0; i < listB.Count; i++)
                    {
                        var a = listB[i];
                        int indexof = listA.IndexOf(a);

                        if (indexof < 0) { continue; }
                        if (indexof < lastIndexof)
                        {
                            return false;
                        }
                        lastIndexof = indexof;
                    }
                    return true;
                }
                public void MergeWith(IReadOnlyList<string> other)
                {
                    List<string> listA = _layers;
                    IReadOnlyList<string> listB = other;

                    if (CheckOverlapsOrder(listA, listB) == false)
                    {
                        //Для слияния списков слоев, нужно чтобы в пересечении порядок записей совпадал
                        Throw.Exception("To merge layer lists, the names of the layers present in both lists must appear in the same order in both lists.");
                    }

                    HashSet<string> seen = new HashSet<string>();
                    List<string> result = new List<string>();

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
                #endregion

                #region Other
                public bool Contains(string layer) { return _layers.Contains(layer); }

                public List<string>.Enumerator GetEnumerator() { return _layers.GetEnumerator(); }
                IEnumerator<string> IEnumerable<string>.GetEnumerator() { return _layers.GetEnumerator(); }
                IEnumerator IEnumerable.GetEnumerator() { return _layers.GetEnumerator(); }
                #endregion
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
                //private int _lastAddOrder;
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
                    Add(new SystemsLayerMarkerSystem(_layerName), int.MinValue, false);
                }
                public void AddList(LayerSystemsList other)
                {
                    for (int i = 1; i < other.recordsCount; i++)
                    {
                        var otherRecord = other.records[i];
                        AddItem_Internal(otherRecord);
                    }
                }
                public void Add(IEcsProcess system, int sortOrder, bool isUnique)
                {
                    AddItem_Internal(new Item(system, sortOrder, isUnique));
                }
                private void AddItem_Internal(Item item)
                {
                    if (_isSorted)
                    {
                        if (recordsCount <= 1)
                        {
                            _lastSortOrder = item.sortOrder;
                        }
                        else if (_lastSortOrder > item.sortOrder)
                        {
                            _isSorted = false;
                        }
                        else
                        {
                            _lastSortOrder = item.sortOrder;
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
                }
            }
            private readonly struct Item : IComparable<Item>
            {
                public readonly IEcsProcess system;
                public readonly int sortOrder;
                public readonly bool isUnique;
                public Item(IEcsProcess system, int sortOrder, bool isUnique)
                {
                    this.system = system;
                    this.sortOrder = sortOrder;
                    this.isUnique = isUnique;
                }
                public int CompareTo(Item other)
                {
                    return sortOrder - other.sortOrder;
                }
            }
            #endregion

            #region SerializableTemplate
            public EcsPipelineTemplate GenerateSerializableTemplate()
            {
                var it = new LinkedListCountIterator<SystemNode>(_systemNodes, _systemNodesCount, _startIndex);
                EcsPipelineTemplate result = new EcsPipelineTemplate();
                result.layers = new string[Layers.Count];
                result.records = new EcsPipelineTemplate.Record[it.Count];
                int i = 0;
                foreach (ref readonly SystemNode node in it)
                {
                    var prms = new AddParams(node.layerName, node.sortOrder, node.isUnique, AddParamsFlags.None.SetOverwriteAll(true).SetNoImport(true));
                    result.records[i++] = new EcsPipelineTemplate.Record(node.system, prms);
                }
                return result;
            }
            #endregion

            #region SystemRecord
            [StructLayout(LayoutKind.Auto)]
            private struct SystemNode : ILinkedNext
            {
                public readonly IEcsProcess system;
                public readonly string layerName;
                public readonly int sortOrder;
                public readonly bool isUnique;
                public int next;
                public int Next
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get { return next; }
                }
                public SystemNode(IEcsProcess system, string layerName, int sortOrder, bool isUnique, int next = -1)
                {
                    this.system = system;
                    this.layerName = layerName;
                    this.sortOrder = sortOrder;
                    this.isUnique = isUnique;
                    this.next = next;
                }
                public override string ToString()
                {
                    return this.AutoToString();
                }
            }
            #endregion
        }
    }

    public static partial class EcsPipelineBuilderExtensions
    {
        #region Simple Builders
        public static EcsPipeline ToPipeline(this IEcsModule module)
        {
            return EcsPipeline.New().Add(module).Build();
        }
        public static EcsPipeline ToPipelineAndInit(this IEcsModule module)
        {
            return EcsPipeline.New().Add(module).BuildAndInit();
        }
        public static EcsPipeline ToPipeline(this IEnumerable<IEcsModule> modules)
        {
            var result = EcsPipeline.New();
            foreach (var module in modules)
            {
                result.Add(module);
            }
            return result.Build();
        }
        public static EcsPipeline ToPipelineAndInit(this IEnumerable<IEcsModule> modules)
        {
            var result = modules.ToPipeline();
            result.Init();
            return result;
        }
        #endregion

        #region Add IEcsProcess
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system)
        {
            return self.Add(system, AddParams.Default);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName)
        {
            return self.Add(system, new AddParams(layerName: layerName));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, int sortOrder)
        {
            return self.Add(system, new AddParams(sortOrder: sortOrder));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, bool isUnique)
        {
            return self.Add(system, new AddParams(isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName, int sortOrder)
        {
            return self.Add(system, new AddParams(layerName: layerName, sortOrder: sortOrder));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName, bool isUnique)
        {
            return self.Add(system, new AddParams(layerName: layerName, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, int sortOrder, bool isUnique)
        {
            return self.Add(system, new AddParams(sortOrder: sortOrder, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName, int sortOrder, bool isUnique)
        {
            return self.Add(system, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system)
        {
            return self.Add(system, new AddParams(isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system, string layerName)
        {
            return self.Add(system, new AddParams(layerName: layerName, isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system, int sortOrder)
        {
            return self.Add(system, new AddParams(sortOrder: sortOrder, isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system, string layerName, int sortOrder)
        {
            return self.Add(system, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: true));
        }
        #endregion

        #region AddModule IEcsModule
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module)
        {
            return self.AddModule(module, AddParams.Default);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName)
        {
            return self.AddModule(module, new AddParams(layerName: layerName));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, int sortOrder)
        {
            return self.AddModule(module, new AddParams(sortOrder: sortOrder));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, bool isUnique)
        {
            return self.AddModule(module, new AddParams(isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName, int sortOrder)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, sortOrder: sortOrder));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName, bool isUnique)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, int sortOrder, bool isUnique)
        {
            return self.AddModule(module, new AddParams(sortOrder: sortOrder, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName, int sortOrder, bool isUnique)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module)
        {
            return self.AddModule(module, new AddParams(isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module, string layerName)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module, int sortOrder)
        {
            return self.AddModule(module, new AddParams(sortOrder: sortOrder, isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module, string layerName, int sortOrder)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: true));
        }
        #endregion

        #region Add Raw
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw)
        {
            return self.Add(raw, AddParams.Default);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName)
        {
            return self.Add(raw, new AddParams(layerName: layerName));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, int sortOrder)
        {
            return self.Add(raw, new AddParams(sortOrder: sortOrder));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, bool isUnique)
        {
            return self.Add(raw, new AddParams(isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName, int sortOrder)
        {
            return self.Add(raw, new AddParams(layerName: layerName, sortOrder: sortOrder));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName, bool isUnique)
        {
            return self.Add(raw, new AddParams(layerName: layerName, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, int sortOrder, bool isUnique)
        {
            return self.Add(raw, new AddParams(sortOrder: sortOrder, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName, int sortOrder, bool isUnique)
        {
            return self.Add(raw, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: isUnique));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw)
        {
            return self.Add(raw, new AddParams(isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw, string layerName)
        {
            return self.Add(raw, new AddParams(layerName: layerName, isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw, int sortOrder)
        {
            return self.Add(raw, new AddParams(sortOrder: sortOrder, isUnique: true));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw, string layerName, int sortOrder)
        {
            return self.Add(raw, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: true));
        }
        #endregion
    }

    #region AddParams
    [Serializable]
    [DataContract]
    public struct AddParams : IEquatable<AddParams>
    {
        public static readonly AddParams Default = new AddParams();
        [DataMember] public string layerName;
        [DataMember] public int sortOrder;
        [DataMember] public bool isUnique;
        [DataMember] public AddParamsFlags flags;

        #region Properties
        public bool IsOverwriteLayerName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return flags.IsOverwriteLayerName(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { flags = value ? flags | AddParamsFlags.OverwriteLayerName : flags & ~AddParamsFlags.OverwriteLayerName; }
        }
        public bool IsOverwriteSortOrder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return flags.IsOverwriteSortOrder(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { flags = value ? flags | AddParamsFlags.OverwriteSortOrder : flags & ~AddParamsFlags.OverwriteSortOrder; }
        }
        public bool IsOverwriteIsUnique
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return flags.IsOverwriteIsUnique(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { flags = value ? flags | AddParamsFlags.OverwriteIsUnique : flags & ~AddParamsFlags.OverwriteIsUnique; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName)
        {
            this.layerName = layerName;
            this.sortOrder = default;
            this.isUnique = default;
            flags = AddParamsFlags.OverwriteLayerName;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(int sortOrder)
        {
            this.layerName = default;
            this.sortOrder = sortOrder;
            this.isUnique = default;
            flags = AddParamsFlags.OverwriteSortOrder;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(bool isUnique)
        {
            this.layerName = default;
            this.sortOrder = default;
            this.isUnique = isUnique;
            flags = AddParamsFlags.OverwriteIsUnique;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, int sortOrder)
        {
            this.layerName = layerName;
            this.sortOrder = sortOrder;
            this.isUnique = default;
            flags = AddParamsFlags.OverwriteLayerName | AddParamsFlags.OverwriteSortOrder;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, bool isUnique)
        {
            this.layerName = layerName;
            this.sortOrder = default;
            this.isUnique = isUnique;
            flags = AddParamsFlags.OverwriteLayerName | AddParamsFlags.OverwriteIsUnique;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(int sortOrder, bool isUnique)
        {
            this.layerName = default;
            this.sortOrder = sortOrder;
            this.isUnique = isUnique;
            flags = AddParamsFlags.OverwriteSortOrder | AddParamsFlags.OverwriteIsUnique;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, int sortOrder, bool isUnique)
        {
            this.layerName = layerName;
            this.sortOrder = sortOrder;
            this.isUnique = isUnique;
            flags = AddParamsFlags.None.SetOverwriteAll(true);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, int sortOrder, bool isUnique, AddParamsFlags overrideFlags)
        {
            this.layerName = layerName;
            this.sortOrder = sortOrder;
            this.isUnique = isUnique;
            this.flags = overrideFlags;
        }
        #endregion

        #region Overwrite
        public AddParams Overwrite(AddParams other)
        {
            AddParams result = this;
            if (other.flags.IsOverwriteLayerName())
            {
                result.layerName = other.layerName;
            }
            if (other.flags.IsOverwriteSortOrder())
            {
                result.sortOrder = other.sortOrder;
            }
            if (other.flags.IsOverwriteIsUnique())
            {
                result.isUnique = other.isUnique;
            }
            result.flags |= other.flags;
            return result;
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AddParams other)
        {
            return sortOrder == other.sortOrder &&
                layerName == other.layerName &&
                isUnique == other.isUnique;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is AddParams && Equals((AddParams)obj);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(sortOrder, layerName, isUnique);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return (flags.IsOverwriteLayerName() ? $"{layerName}, " : "") +
                (flags.IsOverwriteSortOrder() ? $"{sortOrder}, " : "") +
                (flags.IsOverwriteIsUnique() ? $"{isUnique}, " : "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator AddParams(string a) { return new AddParams(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator AddParams(int a) { return new AddParams(a); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator AddParams(bool a) { return new AddParams(a); }
        #endregion
    }

    [Flags]
    public enum AddParamsFlags : byte
    {
        None = 0,
        OverwriteLayerName = 1 << 0,
        OverwriteSortOrder = 1 << 1,
        OverwriteIsUnique = 1 << 2,

        /// <summary>
        /// Ignore call IEcsModule.Import(Builder b)
        /// </summary>
        NoImport = 1 << 7,
    }
    public static class AddParamsFlagsUtility
    {
        private const AddParamsFlags OverwriteAll = AddParamsFlags.OverwriteLayerName | AddParamsFlags.OverwriteSortOrder | AddParamsFlags.OverwriteIsUnique;
        private const AddParamsFlags All = OverwriteAll | AddParamsFlags.NoImport;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddParamsFlags Normalize(this AddParamsFlags flags) { return flags & All; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOverwriteLayerName(this AddParamsFlags flags) { return (flags & AddParamsFlags.OverwriteLayerName) == AddParamsFlags.OverwriteLayerName; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOverwriteSortOrder(this AddParamsFlags flags) { return (flags & AddParamsFlags.OverwriteSortOrder) == AddParamsFlags.OverwriteSortOrder; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOverwriteIsUnique(this AddParamsFlags flags) { return (flags & AddParamsFlags.OverwriteIsUnique) == AddParamsFlags.OverwriteIsUnique; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOverwriteAll(this AddParamsFlags flags) { return (flags & OverwriteAll) == OverwriteAll; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoImport(this AddParamsFlags flags) { return (flags & AddParamsFlags.NoImport) == AddParamsFlags.NoImport; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddParamsFlags SetOverwriteLayerName(this AddParamsFlags flags, bool flag) { return flag ? flags | AddParamsFlags.OverwriteLayerName : flags & ~AddParamsFlags.OverwriteLayerName; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddParamsFlags SetOverwriteSortOrder(this AddParamsFlags flags, bool flag) { return flag ? flags | AddParamsFlags.OverwriteSortOrder : flags & ~AddParamsFlags.OverwriteSortOrder; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddParamsFlags SetOverwriteIsUnique(this AddParamsFlags flags, bool flag) { return flag ? flags | AddParamsFlags.OverwriteIsUnique : flags & ~AddParamsFlags.OverwriteIsUnique; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddParamsFlags SetOverwriteAll(this AddParamsFlags flags, bool flag) { return flag ? flags | OverwriteAll : flags & ~OverwriteAll; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddParamsFlags SetNoImport(this AddParamsFlags flags, bool flag) { return flag ? flags | AddParamsFlags.NoImport : flags & ~AddParamsFlags.NoImport; }
    }
    #endregion
}
#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using static DCFApixels.DragonECS.EcsConsts;

namespace DCFApixels.DragonECS
{
    /// <summary>
    /// Interface for systems or modules that specify default addition parameters
    /// when they are added to a pipeline builder.
    /// </summary>
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
        /// <summary>
        /// Builder for constructing an <see cref="EcsPipeline"/>.
        /// Provides a fluent API for adding systems, modules, and configuring layers.
        /// </summary>
        public partial class Builder : IEcsModule
        {
            private SystemNode[] _systemNodes = new SystemNode[256];
            private int _startIndex = -1;
            private int _endIndex = -1;
            private int _systemNodesCount = 0;
            private int _freeIndex = -1;
            private int _freeNodesCount = 0;

            private readonly Dictionary<string, LayerSystemsList> _layerLists = new Dictionary<string, LayerSystemsList>(8);
            private readonly StructList<InitDeclaredRunner> _initDeclaredRunners = new StructList<InitDeclaredRunner>(4);

            /// <summary>Provides access to layer management for the pipeline being built.</summary>
            public readonly LayersMap Layers;

            /// <summary>Provides access to dependency injection configuration.</summary>
            public readonly InitInjectionList Injections;

            /// <summary>Provides access to configuration container for the pipeline.</summary>
            public readonly Configurator Configs;

            private AddParams _defaultAddParams = new AddParams(BASIC_LAYER, 0, false);

            private HashSet<Type> _uniqueSystemsSet = new HashSet<Type>();

            #region Constructors
            /// <summary>Initializes a new pipeline builder with an optional configuration container.</summary>
            /// <param name="config">Optional configuration container. If null, a default container is created.</param>
            public Builder(IConfigContainerWriter config = null)
            {
                if (config == null) { config = new ConfigContainer(); }
                Configs = new Configurator(config, this);

                var injectorBuilder = new Injector.InjectionList();
                Injections = new InitInjectionList(injectorBuilder, this);
                Injections.AddNode<object>();
                Injections.AddNode<EcsWorld>();
                Injections.AddNode<EcsAspect>();
                Injections.AddNode<EcsPipeline>();

                var graph = new DependencyGraph<string>(BASIC_LAYER);
                Layers = new LayersMap(graph, this, PRE_BEGIN_LAYER, BEGIN_LAYER, BASIC_LAYER, END_LAYER, POST_END_LAYER);
            }
            #endregion

            #region Add IEcsProcess
            /// <summary>Adds a system to the pipeline with the specified parameters.</summary>
            /// <param name="system">The system to add.</param>
            /// <param name="parameters">Addition parameters (layer, sort order, uniqueness).</param>
            /// <returns>The builder instance for chaining.</returns>
            /// <remarks>
            /// its <see cref="IEcsModule.Import"/> method is invoked to import other systems, and then the system itself
            /// is added to the pipeline (unless it was already added manually inside the <see cref="IEcsModule.Import"/> implementation).
            /// The system is added with the same layer, sort order, and uniqueness parameters as the module import.
            /// </remarks>
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
            /// <summary>Adds a module to the pipeline with the specified parameters.</summary>
            /// <param name="module">The module to import.</param>
            /// <param name="parameters">Addition parameters (layer, sort order, uniqueness).</param>
            /// <returns>The builder instance for chaining.</returns>
            /// <remarks>
            /// If the module also implements <see cref="IEcsProcess"/>, it is treated both as a module and as a system:
            /// its <see cref="IEcsModule.Import"/> method is invoked first, and then the module itself is added as a system
            /// to the pipeline using the same addition parameters.
            /// </remarks>
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

                Injections.Inject(module);
                return this;
            }
            #endregion

            #region Add Raw
            /// <summary>
            /// Adds a raw object (system or module) to the pipeline with the specified parameters.
            /// The object must implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.
            /// </summary>
            /// <param name="raw">The object to add (must be an <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>).</param>
            /// <param name="parameters">Addition parameters (layer, sort order, uniqueness).</param>
            /// <returns>The builder instance for chaining.</returns>
            /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
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
            /// <summary>Declares a runner that will be instantiated when the pipeline is built.</summary>
            /// <typeparam name="TRunner">The runner type (must inherit <see cref="EcsRunner"/> and implement <see cref="IEcsRunner"/>).</typeparam>
            /// <returns>The builder instance for chaining.</returns>
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
                Injections.MergeWith(other.Injections);
                foreach (var declaredRunners in other._initDeclaredRunners)
                {
                    _initDeclaredRunners.Add(declaredRunners);
                }
                foreach (var config in other.Configs.Instance.GetAllConfigs())
                {
                    Configs.Instance.Set(config.Key, config.Value);
                }
                Layers.MergeWith(other.Layers);

                foreach (ref readonly SystemNode otherRecord in new LinkedListCountIterator<SystemNode>(_systemNodes, _systemNodesCount, _startIndex))
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
            /// <summary>Removes all systems of the specified type from the builder.</summary>
            /// <typeparam name="TSystem">The system type to remove.</typeparam>
            /// <returns>The builder instance for chaining.</returns>
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

            /// <summary>Builds the pipeline with all added systems, modules, and runners.</summary>
            /// <returns>The constructed <see cref="EcsPipeline"/> instance.</returns>
            public EcsPipeline Build()
            {
#if DEBUG
                _buildMarker.Begin();
#endif
                var it = new LinkedListCountIterator<SystemNode>(_systemNodes, _systemNodesCount, _startIndex);

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
                    foreach (var item in Layers.Build())
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

                EcsPipeline pipeline = new EcsPipeline((ReadOnlySpan<IEcsProcess>)allSystems, Configs.Instance.GetContainer(), Injections.Instance);
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

            #region InitInjector
            /// <summary>
            /// Provides configuration for dependency injection within the pipeline builder.
            /// </summary>
            public readonly struct InitInjectionList
            {
                private readonly Builder _pipelineBuilder;

                /// <summary>Gets the underlying injection list.</summary>
                public readonly Injector.InjectionList Instance;
                public InitInjectionList(Injector.InjectionList instance, Builder pipelineBuilder)
                {
                    Instance = instance;
                    _pipelineBuilder = pipelineBuilder;
                }

                /// <summary>Adds an injection node for type <typeparamref name="T"/>.</summary>
                /// <typeparam name="T">The type to register for injection.</typeparam>
                /// <returns>The builder instance for chaining.</returns>
                public Builder AddNode<T>()
                {
                    Instance.AddNode<T>();
                    return _pipelineBuilder;
                }

                /// <summary>Injects a specific object instance into the pipeline.</summary>
                /// <typeparam name="T">The type of the object.</typeparam>
                /// <param name="obj">The object instance to inject.</param>
                /// <returns>The builder instance for chaining.</returns>
                public Builder Inject<T>(T obj)
                {
                    Instance.Inject(obj);
                    return _pipelineBuilder;
                }

                /// <summary>Extracts an object instance from the injection container.</summary>
                /// <typeparam name="T">The type of the object.</typeparam>
                /// <param name="obj">Outputs the extracted object.</param>
                /// <returns>The builder instance for chaining.</returns>
                public Builder Extract<T>(ref T obj)
                {
                    Instance.Extract(ref obj);
                    return _pipelineBuilder;
                }

                /// <summary>Merges another injection list into this one.</summary>
                /// <param name="other">The other injection list.</param>
                /// <returns>The builder instance for chaining.</returns>
                public Builder Merge(Injector.InjectionList other)
                {
                    Instance.MergeWith(other);
                    return _pipelineBuilder;
                }

                /// <summary>Merges another <see cref="InitInjectionList"/> into this one.</summary>
                /// <param name="other">The other injection list.</param>
                /// <returns>The builder instance for chaining.</returns>
                public Builder MergeWith(InitInjectionList other)
                {
                    Instance.MergeWith(other.Instance);
                    return _pipelineBuilder;
                }
            }
            #endregion

            #region Configurator
            /// <summary>
            /// Provides configuration container access for the pipeline builder.
            /// </summary>
            public readonly struct Configurator
            {
                private readonly IConfigContainerWriter _configs;
                private readonly Builder _builder;
                public Configurator(IConfigContainerWriter configs, Builder builder)
                {
                    _configs = configs;
                    _builder = builder;
                }

                /// <summary>Gets the underlying configuration container writer.</summary>
                public IConfigContainerWriter Instance
                {
                    get { return _configs; }
                }

                /// <summary>Sets a configuration value by type.</summary>
                /// <typeparam name="T">The type of the configuration value.</typeparam>
                /// <param name="value">The value to set.</param>
                /// <returns>The builder instance for chaining.</returns>
                public Builder Set<T>(T value)
                {
                    _configs.Set(value);
                    return _builder;
                }
            }

            #endregion

            #region LayerSystemsList
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
            /// <summary>Generates a serializable template from the current builder state.</summary>
            /// <returns>An <see cref="EcsPipelineTemplate"/> that can be serialized.</returns>
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
        /// <summary>Creates a pipeline from a single module (builds without initialization).</summary>
        /// <param name="module">The module to import.</param>
        /// <returns>The built <see cref="EcsPipeline"/> instance.</returns>
        public static EcsPipeline ToPipeline(this IEcsModule module)
        {
            return EcsPipeline.New().Add(module).Build();
        }

        /// <summary>Creates a pipeline from a single module and immediately initializes it.</summary>
        /// <param name="module">The module to import.</param>
        /// <returns>The built and initialized <see cref="EcsPipeline"/> instance.</returns>
        public static EcsPipeline ToPipelineAndInit(this IEcsModule module)
        {
            return EcsPipeline.New().Add(module).BuildAndInit();
        }

        /// <summary>Creates a pipeline from a collection of modules (builds without initialization).</summary>
        /// <param name="modules">The modules to import.</param>
        /// <returns>The built <see cref="EcsPipeline"/> instance.</returns>
        public static EcsPipeline ToPipeline(this IEnumerable<IEcsModule> modules)
        {
            var result = EcsPipeline.New();
            foreach (var module in modules)
            {
                result.Add(module);
            }
            return result.Build();
        }

        /// <summary>Creates a pipeline from a collection of modules and immediately initializes it.</summary>
        /// <param name="modules">The modules to import.</param>
        /// <returns>The built and initialized <see cref="EcsPipeline"/> instance.</returns>
        public static EcsPipeline ToPipelineAndInit(this IEnumerable<IEcsModule> modules)
        {
            var result = modules.ToPipeline();
            result.Init();
            return result;
        }
        #endregion

        #region Add IEcsProcess
        /// <summary>Adds a system to the pipeline with default parameters.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system)
        {
            return self.Add(system, AddParams.Default);
        }

        /// <summary>Adds a system to the specified layer.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="layerName">The layer name (overrides default).</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName)
        {
            return self.Add(system, new AddParams(layerName: layerName));
        }

        /// <summary>Adds a system with the specified sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="sortOrder">The sort order (lower values execute first).</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, int sortOrder)
        {
            return self.Add(system, new AddParams(sortOrder: sortOrder));
        }

        /// <summary>Adds a system with the specified uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="isUnique">Whether the system should be added as a unique instance (prevents duplicates).</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, bool isUnique)
        {
            return self.Add(system, new AddParams(isUnique: isUnique));
        }

        /// <summary>Adds a system to the specified layer with the given sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="layerName">The layer name (overrides default).</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName, int sortOrder)
        {
            return self.Add(system, new AddParams(layerName: layerName, sortOrder: sortOrder));
        }

        /// <summary>Adds a system to the specified layer with the given uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="layerName">The layer name (overrides default).</param>
        /// <param name="isUnique">Whether the system should be added as a unique instance (prevents duplicates).</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName, bool isUnique)
        {
            return self.Add(system, new AddParams(layerName: layerName, isUnique: isUnique));
        }

        /// <summary>Adds a system with the specified sort order and uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="sortOrder">The sort order (lower values execute first).</param>
        /// <param name="isUnique">Whether the system should be added as a unique instance (prevents duplicates).</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, int sortOrder, bool isUnique)
        {
            return self.Add(system, new AddParams(sortOrder: sortOrder, isUnique: isUnique));
        }

        /// <summary>Adds a system with the specified layer, sort order, and uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="layerName">The layer name (overrides default).</param>
        /// <param name="sortOrder">The sort order (lower values execute first).</param>
        /// <param name="isUnique">Whether the system should be added as a unique instance (prevents duplicates).</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEcsProcess system, string layerName, int sortOrder, bool isUnique)
        {
            return self.Add(system, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: isUnique));
        }

        /// <summary>Adds a system as a unique instance with default parameters.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system)
        {
            return self.Add(system, new AddParams(isUnique: true));
        }

        /// <summary>Adds a system as a unique instance to the specified layer.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system, string layerName)
        {
            return self.Add(system, new AddParams(layerName: layerName, isUnique: true));
        }

        /// <summary>Adds a system as a unique instance with the specified sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system, int sortOrder)
        {
            return self.Add(system, new AddParams(sortOrder: sortOrder, isUnique: true));
        }

        /// <summary>Adds a system as a unique instance to the specified layer with the given sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="system">The system to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEcsProcess system, string layerName, int sortOrder)
        {
            return self.Add(system, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: true));
        }
        #endregion

        #region AddModule IEcsModule
        /// <summary>Adds a module with default parameters.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module (its <see cref="IEcsModule.Import"/> is called) and as a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module)
        {
            return self.AddModule(module, AddParams.Default);
        }

        /// <summary>Adds a module to the specified layer.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="layerName">The layer name.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName)
        {
            return self.AddModule(module, new AddParams(layerName: layerName));
        }

        /// <summary>Adds a module with the specified sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, int sortOrder)
        {
            return self.AddModule(module, new AddParams(sortOrder: sortOrder));
        }

        /// <summary>Adds a module with the specified uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="isUnique">Whether the module (and its systems) should be added as unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, bool isUnique)
        {
            return self.AddModule(module, new AddParams(isUnique: isUnique));
        }

        /// <summary>Adds a module to the specified layer with the given sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName, int sortOrder)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, sortOrder: sortOrder));
        }

        /// <summary>Adds a module to the specified layer with the given uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="isUnique">Whether the module is unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName, bool isUnique)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, isUnique: isUnique));
        }

        /// <summary>Adds a module with the specified sort order and uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the module is unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, int sortOrder, bool isUnique)
        {
            return self.AddModule(module, new AddParams(sortOrder: sortOrder, isUnique: isUnique));
        }

        /// <summary>Adds a module with the specified layer, sort order, and uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the module is unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModule(this EcsPipeline.Builder self, IEcsModule module, string layerName, int sortOrder, bool isUnique)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: isUnique));
        }

        /// <summary>Adds a module as a unique instance with default parameters.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a unique system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module)
        {
            return self.AddModule(module, new AddParams(isUnique: true));
        }

        /// <summary>Adds a module as a unique instance to the specified layer.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="layerName">The layer name.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a unique system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module, string layerName)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, isUnique: true));
        }

        /// <summary>Adds a module as a unique instance with the specified sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a unique system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module, int sortOrder)
        {
            return self.AddModule(module, new AddParams(sortOrder: sortOrder, isUnique: true));
        }

        /// <summary>Adds a module as a unique instance to the specified layer with the given sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="module">The module to import.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <remarks>If the module also implements <see cref="IEcsProcess"/>, it is added as both a module and a unique system.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddModuleUnique(this EcsPipeline.Builder self, IEcsModule module, string layerName, int sortOrder)
        {
            return self.AddModule(module, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: true));
        }
        #endregion

        #region Add Raw
        /// <summary>Adds a raw object (system or module) with default parameters.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add (must implement <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>).</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw)
        {
            return self.Add(raw, AddParams.Default);
        }

        /// <summary>Adds a raw object to the specified layer.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName)
        {
            return self.Add(raw, new AddParams(layerName: layerName));
        }

        /// <summary>Adds a raw object with the specified sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, int sortOrder)
        {
            return self.Add(raw, new AddParams(sortOrder: sortOrder));
        }

        /// <summary>Adds a raw object with the specified uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="isUnique">Whether the object should be added as unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, bool isUnique)
        {
            return self.Add(raw, new AddParams(isUnique: isUnique));
        }

        /// <summary>Adds a raw object to the specified layer with the given sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName, int sortOrder)
        {
            return self.Add(raw, new AddParams(layerName: layerName, sortOrder: sortOrder));
        }

        /// <summary>Adds a raw object to the specified layer with the given uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="isUnique">Whether the object is unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName, bool isUnique)
        {
            return self.Add(raw, new AddParams(layerName: layerName, isUnique: isUnique));
        }

        /// <summary>Adds a raw object with the specified sort order and uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the object is unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, int sortOrder, bool isUnique)
        {
            return self.Add(raw, new AddParams(sortOrder: sortOrder, isUnique: isUnique));
        }

        /// <summary>Adds a raw object with the specified layer, sort order, and uniqueness flag.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the object is unique.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, object raw, string layerName, int sortOrder, bool isUnique)
        {
            return self.Add(raw, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: isUnique));
        }

        /// <summary>Adds a raw object as a unique instance with default parameters.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw)
        {
            return self.Add(raw, new AddParams(isUnique: true));
        }

        /// <summary>Adds a raw object as a unique instance to the specified layer.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw, string layerName)
        {
            return self.Add(raw, new AddParams(layerName: layerName, isUnique: true));
        }

        /// <summary>Adds a raw object as a unique instance with the specified sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw, int sortOrder)
        {
            return self.Add(raw, new AddParams(sortOrder: sortOrder, isUnique: true));
        }

        /// <summary>Adds a raw object as a unique instance to the specified layer with the given sort order.</summary>
        /// <param name="self">The builder instance.</param>
        /// <param name="raw">The object to add.</param>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type does not implement either <see cref="IEcsProcess"/> or <see cref="IEcsModule"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, object raw, string layerName, int sortOrder)
        {
            return self.Add(raw, new AddParams(layerName: layerName, sortOrder: sortOrder, isUnique: true));
        }
        #endregion
    }

    #region AddParams
    /// <summary>
    /// Parameters that control how a system or module is added to a pipeline.
    /// Includes layer name, sort order, uniqueness flag, and override flags.
    /// </summary>
    [Serializable]
    [DataContract]
    public struct AddParams : IEquatable<AddParams>
    {
        /// <summary>Default addition parameters (no overrides).</summary>
        public static readonly AddParams Default = new AddParams();

        /// <summary>The layer name where the system will be placed.</summary>
        [DataMember] public string layerName;

        /// <summary>Sort order within the layer (lower values execute first).</summary>
        [DataMember] public int sortOrder;

        /// <summary>Whether the system should be added as a unique instance (prevents duplicates).</summary>
        [DataMember] public bool isUnique;

        /// <summary>Flags indicating which fields should override default values.</summary>
        [DataMember] public AddParamsFlags flags;

        #region Properties
        /// <summary>Indicates whether the layer name should override the default.</summary>
        public bool IsOverwriteLayerName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return flags.IsOverwriteLayerName(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { flags = value ? flags | AddParamsFlags.OverwriteLayerName : flags & ~AddParamsFlags.OverwriteLayerName; }
        }
        /// <summary>Indicates whether the sort order should override the default.</summary>
        public bool IsOverwriteSortOrder
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return flags.IsOverwriteSortOrder(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { flags = value ? flags | AddParamsFlags.OverwriteSortOrder : flags & ~AddParamsFlags.OverwriteSortOrder; }
        }
        /// <summary>Indicates whether the uniqueness flag should override the default.</summary>
        public bool IsOverwriteIsUnique
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return flags.IsOverwriteIsUnique(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { flags = value ? flags | AddParamsFlags.OverwriteIsUnique : flags & ~AddParamsFlags.OverwriteIsUnique; }
        }
        #endregion

        #region Constructors
        /// <summary>Creates parameters with a specified layer name (overrides layer name).</summary>
        /// <param name="layerName">The layer name.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName)
        {
            this.layerName = layerName;
            this.sortOrder = default;
            this.isUnique = default;
            flags = AddParamsFlags.OverwriteLayerName;
        }

        /// <summary>Creates parameters with a specified sort order (overrides sort order).</summary>
        /// <param name="sortOrder">The sort order.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(int sortOrder)
        {
            this.layerName = default;
            this.sortOrder = sortOrder;
            this.isUnique = default;
            flags = AddParamsFlags.OverwriteSortOrder;
        }

        /// <summary>Creates parameters with a specified uniqueness flag (overrides uniqueness).</summary>
        /// <param name="isUnique">Whether the system is unique.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(bool isUnique)
        {
            this.layerName = default;
            this.sortOrder = default;
            this.isUnique = isUnique;
            flags = AddParamsFlags.OverwriteIsUnique;
        }


        /// <summary>Creates parameters with layer name and sort order.</summary>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, int sortOrder)
        {
            this.layerName = layerName;
            this.sortOrder = sortOrder;
            this.isUnique = default;
            flags = AddParamsFlags.OverwriteLayerName | AddParamsFlags.OverwriteSortOrder;
        }

        /// <summary>Creates parameters with layer name and uniqueness flag.</summary>
        /// <param name="layerName">The layer name.</param>
        /// <param name="isUnique">Whether the system is unique.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, bool isUnique)
        {
            this.layerName = layerName;
            this.sortOrder = default;
            this.isUnique = isUnique;
            flags = AddParamsFlags.OverwriteLayerName | AddParamsFlags.OverwriteIsUnique;
        }

        /// <summary>Creates parameters with sort order and uniqueness flag.</summary>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the system is unique.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(int sortOrder, bool isUnique)
        {
            this.layerName = default;
            this.sortOrder = sortOrder;
            this.isUnique = isUnique;
            flags = AddParamsFlags.OverwriteSortOrder | AddParamsFlags.OverwriteIsUnique;
        }

        /// <summary>Creates parameters with all fields specified.</summary>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the system is unique.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AddParams(string layerName, int sortOrder, bool isUnique)
        {
            this.layerName = layerName;
            this.sortOrder = sortOrder;
            this.isUnique = isUnique;
            flags = AddParamsFlags.None.SetOverwriteAll(true);
        }

        /// <summary>Creates parameters with all fields and explicit override flags.</summary>
        /// <param name="layerName">The layer name.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="isUnique">Whether the system is unique.</param>
        /// <param name="overrideFlags">Flags indicating which fields override defaults.</param>
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
        /// <summary>Overwrites this instance with values from another, respecting override flags.</summary>
        /// <param name="other">The other parameters.</param>
        /// <returns>A new <see cref="AddParams"/> instance with merged values.</returns>
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
        /// <summary>No overrides.</summary>
        None = 0,
        /// <summary>Override the layer name.</summary>
        OverwriteLayerName = 1 << 0,
        /// <summary>Override the sort order.</summary>
        OverwriteSortOrder = 1 << 1,
        /// <summary>Override the uniqueness flag.</summary>
        OverwriteIsUnique = 1 << 2,

        /// <summary>Ignore the call to <see cref="IEcsModule.Import(EcsPipeline.Builder)"/>.</summary>
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
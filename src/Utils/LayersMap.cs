using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DCFApixels.DragonECS.Core
{
    public partial class LayersMap : IEnumerable<string>
    {
        private readonly Dictionary<string, LayerID> _layerIds = new Dictionary<string, LayerID>(32);
        private StructList<LayerInfo> _layerInfos = new StructList<LayerInfo>(32);
        private LayerID GetLayerID(string layer)
        {
            if (_layerIds.TryGetValue(layer, out LayerID layerID) == false)
            {
                layerID = (LayerID)_layerInfos.Count;
                _layerInfos.Add(default);

                _layerIds[layer] = layerID;
                ref var layerInfo = ref GetLayerInfo(layerID);
                layerInfo.name = layer;
            }
            return layerID;
        }
        private ref LayerInfo GetLayerInfo(LayerID layerID)
        {
            return ref _layerInfos._items[(int)layerID];
        }
        [DebuggerDisplay("{name}")]
        private struct LayerInfo
        {
            public string name;
            public int insertionIndex;
            public bool isLocked;
            public bool isContained;
            public bool isBefore;
            //build
            public int inDegree;
            public bool hasAnyDependency;
            public LayerInfo(string name) : this()
            {
                this.name = name;
            }
        }

        private readonly EcsPipeline.Builder _source;
        private List<(LayerID From, LayerID To)> _dependencies = new List<(LayerID, LayerID)>(16);
        private readonly LayerID _rootLayerID;
        private readonly LayerID _basicLayerID;
        private int _increment = 0;
        private int _count;

        public IEnumerable<(string from, string to)> Deps
        {
            get
            {
                return _dependencies.Select(o => (GetLayerInfo(o.From).name, GetLayerInfo(o.To).name));
            }
        }

        #region Properties
        public int Count
        {
            get { return _count; }
        }
        #endregion

        #region Constructors
        public LayersMap(EcsPipeline.Builder source = null)
        {
            GetLayerID("");
            _source = source;
            _rootLayerID = LayerID.NULL;
            _basicLayerID = LayerID.NULL;
        }
        public LayersMap(EcsPipeline.Builder source, string basicLayerName)
        {
            GetLayerID("");
            _source = source;

            Add(basicLayerName);

            _rootLayerID = LayerID.NULL;
            _basicLayerID = GetLayerID(basicLayerName);
            LockLayer(basicLayerName);
        }
        public LayersMap(EcsPipeline.Builder source, string preBeginlayer, string beginlayer, string basicLayer, string endLayer, string postEndLayer)
        {
            GetLayerID("");
            _source = source;

            Add(preBeginlayer);
                //.Before(beginlayer);
            Add(beginlayer)
                //.Before(basicLayer)
                .After(preBeginlayer);
            Add(basicLayer)
                //.Before(endLayer)
                .After(beginlayer);
            Add(endLayer)
                //.Before(postEndLayer)
                .After(basicLayer);
            Add(postEndLayer)
                .After(endLayer);

            _rootLayerID = GetLayerID(preBeginlayer);
            _basicLayerID = GetLayerID(basicLayer);
            LockLayer(preBeginlayer);
            LockLayer(beginlayer);
            LockLayer(basicLayer);
            LockLayer(endLayer);
            LockLayer(postEndLayer);
        }
        #endregion

        #region Methods
        private void LockLayer(string layer)
        {
            GetLayerInfo(GetLayerID(layer)).isLocked = true;
        }
        private void Add_Internal(LayerID id)
        {
            ref var info = ref GetLayerInfo(id);
            if (info.isLocked) { return; }
            if (info.isContained == false)
            {
                _count++;
                info.isContained = true;
            }
            info.insertionIndex = _increment++;
        }
        private void Remove_Internal(LayerID id)
        {
            ref var info = ref GetLayerInfo(id);
            if (info.isLocked) { throw new Exception($"The {info.name} layer cannot be removed"); }
            if (info.isContained)
            {
                _count--;
                info.isContained = false;
            }
            info.insertionIndex = 0;
        }
        private void AddDependency_Internal(LayerID from, LayerID to, bool isBefore = false)
        {
            GetLayerInfo(from).hasAnyDependency = true;
            GetLayerInfo(to).hasAnyDependency = true;

            if (isBefore)
            {
                //GetLayerInfo(from).insertionIndex = 1000 - (_increment++);
                GetLayerInfo(from).isBefore = true;
                //_dependencies.Insert(0, (from, to));
            }
            else
            {
                //GetLayerInfo(from).insertionIndex = _increment++;
                GetLayerInfo(from).isBefore = false;
                //_dependencies.Add((from, to));
            }
            //_dependencies.Insert(0, (from, to));
            _dependencies.Add((from, to));
        }
        private void AddDependency_Internal(LayerID from, string to, bool isBefore = false)
        {
            AddDependency_Internal(from, GetLayerID(to), isBefore);
        }
        private void AddDependency_Internal(string from, LayerID to, bool isBefore = false)
        {
            AddDependency_Internal(GetLayerID(from), to, isBefore);
        }
        private void AddDependency_Internal(string from, string to, bool isBefore = false)
        {
            AddDependency_Internal(GetLayerID(from), GetLayerID(to), isBefore);
        }
        #endregion

        #region Add
        public DependencyHandler Add(string layer)
        {
            var id = GetLayerID(layer);
            Add_Internal(id);
            return new DependencyHandler(this, (int)id);
        }
        public DependencyHandler Add(params string[] layers)
        {
            return Add(layersRange: layers);
        }
        public DependencyHandler Add(IEnumerable<string> layersRange)
        {
            foreach (var layer in layersRange)
            {
                Add_Internal(GetLayerID(layer));
            }
            return new DependencyHandler(this, layersRange);
        }
        #endregion

        #region Move
        public DependencyHandler Move(string layer)
        {
            return new DependencyHandler(this, (int)GetLayerID(layer));
        }
        public DependencyHandler Move(params string[] layers)
        {
            return new DependencyHandler(this, layers);
        }
        public DependencyHandler Move(IEnumerable<string> layersRange)
        {
            return new DependencyHandler(this, layersRange);
        }
        #endregion

        #region Remove
        public void Remove(string layer)
        {
            Remove_Internal(GetLayerID(layer));
        }
        public void Remove(params string[] layers)
        {
            Remove(layersRange: layers);
        }
        public void Remove(IEnumerable<string> layersRange)
        {
            foreach (var layer in layersRange)
            {
                Remove_Internal(GetLayerID(layer));
            }
        }
        #endregion

        #region MergeWith
        public void MergeWith(LayersMap other)
        {
            foreach (var otherDependency in other._dependencies)
            {
                AddDependency_Internal(other.GetLayerInfo(otherDependency.From).name, other.GetLayerInfo(otherDependency.To).name);
            }
            for (int i = 0; i < other._layerInfos.Count; i++)
            {
                LayerID otherLayerID = (LayerID)i;
                ref var otherLayerInfo = ref other.GetLayerInfo(otherLayerID);
                Add(otherLayerInfo.name);
            }
        }
        #endregion

        private enum LayerID : int { NULL = 0 }
        public readonly ref struct DependencyHandler
        {
            private readonly LayersMap _source;
            private readonly LayerID _id;
            private readonly IEnumerable<string> _layersRange;

            #region Properties
            public EcsPipeline.Builder Back
            {
                get { return _source._source; }
            }
            #endregion

            #region Constructors
            public DependencyHandler(LayersMap source, int id)
            {
                _source = source;
                _id = (LayerID)id;
                _layersRange = null;
            }
            public DependencyHandler(LayersMap source, IEnumerable<string> layersRange)
            {
                _source = source;
                _id = LayerID.NULL;
                _layersRange = layersRange;
            }
            #endregion

            #region Before
            public DependencyHandler Before(params string[] targets)
            {
                return Before(range: targets);
            }
            public DependencyHandler Before(IEnumerable<string> range)
            {
                foreach (var target in range)
                {
                    Before(target);
                }
                return this;
            }
            public DependencyHandler Before(string targetLayer)
            {
                if (_id != LayerID.NULL)
                {
                    _source.AddDependency_Internal(_id, targetLayer, true);
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _source.AddDependency_Internal(layer, targetLayer, true);
                    }
                }
                return this;
            }
            #endregion

            #region After
            public DependencyHandler After(params string[] targets)
            {
                return After(range: targets);
            }
            public DependencyHandler After(IEnumerable<string> range)
            {
                foreach (var target in range)
                {
                    After(target);
                }
                return this;
            }
            public DependencyHandler After(string target)
            {
                if (_id != LayerID.NULL)
                {
                    _source.AddDependency_Internal(target, _id);
                }
                if (_layersRange != null)
                {
                    foreach (var id in _layersRange)
                    {
                        _source.AddDependency_Internal(target, id);
                    }
                }
                return this;
            }
            #endregion
        }

        #region Build
        public string[] Build()
        {
            LayerID[] nodes = new LayerID[_count];
            var adjacency = new List<(LayerID To, int DependencyIndex)>[_layerInfos.Count];

            for (int i = 0, j = 0; i < _layerInfos.Count; i++)
            {
                LayerID layerID = (LayerID)i;
                ref var info = ref GetLayerInfo(layerID);
                adjacency[(int)layerID] = new List<(LayerID To, int DependencyIndex)>();
                _layerInfos._items[(int)layerID].inDegree = 0;
                if (info.isContained)
                {
                    nodes[j++] = layerID;
                }
            }

            for (int i = 0; i < _dependencies.Count; i++)
            {
                var (from, to) = _dependencies[i];
                ref var fromInfo = ref GetLayerInfo(from);
                ref var toInfo = ref GetLayerInfo(to);

                if (fromInfo.isContained && toInfo.isContained)
                {
                    adjacency[(int)from].Add((to, i));
                    toInfo.inDegree += 1;
                }
            }

            //// добавление зависимостей для нод без зависимостей.
            //if (_basicLayerID != LayerID.NULL)
            //{
            //    var basicLayerAdjacencyList = adjacency[(int)_basicLayerID];
            //    int inserIndex = basicLayerAdjacencyList.Count;
            //    //for (int i = _layerInfos.Count - 1; i >= 0; i--)
            //    for (int i = 0; i < _layerInfos.Count; i++)
            //    {
            //        var id = (LayerID)i;
            //        ref var toInfo = ref _layerInfos._items[i];
            //        if(toInfo.isContained && toInfo.hasAnyDependency == false)
            //        {
            //            //toInfo.insertionIndex = -toInfo.insertionIndex;
            //            basicLayerAdjacencyList.Insert(inserIndex, (id, toInfo.insertionIndex));
            //            toInfo.inDegree += 1;
            //        }
            //    }
            //}

            List<LayerID> zeroInDegree = new List<LayerID>(nodes.Length);
            zeroInDegree.AddRange(nodes.Where(id => GetLayerInfo(id).inDegree == 0).OrderBy(id => GetLayerInfo(id).insertionIndex));

            var result = new List<string>(nodes.Length);

            while (zeroInDegree.Count > 0)
            {
                var current = zeroInDegree[0];
                zeroInDegree.RemoveAt(0);

                result.Add(GetLayerInfo(current).name);


                var adjacencyList = adjacency[(int)current];
                //for (int i = adjacencyList.Count - 1; i >= 0; i--)
                for (int i = 0; i < adjacencyList.Count; i++)
                {
                    var (neighbor, _) = adjacencyList[i];
                    ref var neighborInfo = ref GetLayerInfo(neighbor);
                    neighborInfo.inDegree--;
                    if (neighborInfo.inDegree == 0)
                    {
                        var neighborInsertionIndex = neighborInfo.insertionIndex;
                        int insertIndex = zeroInDegree.FindIndex(id => GetLayerInfo(id).insertionIndex < neighborInsertionIndex);
                        insertIndex = insertIndex < 0 ? 0 : insertIndex;
                        zeroInDegree.Insert(insertIndex, neighbor);
                
                        //zeroInDegree.Add(neighbor);
                        //zeroInDegree = zeroInDegree.OrderBy(id => GetLayerInfo(id).insertionIndex).ToList();
                    }
                }

                //var adjacencyList = adjacency[(int)current];
                ////var adjacencyListSort = adjacencyList.OrderBy(o => GetLayerInfo(o.To).insertionIndex);
                //var adjacencyListSort = adjacencyList;
                //foreach (var item in adjacencyListSort)
                //{
                //    var (neighbor, _) = item;
                //    ref var neighborInfo = ref GetLayerInfo(neighbor);
                //    neighborInfo.inDegree--;
                //    if (neighborInfo.inDegree == 0)
                //    {
                //        //var neighborInsertionIndex = neighborInfo.insertionIndex;
                //        //int insertIndex = zeroInDegree.FindIndex(id => GetLayerInfo(id).insertionIndex > neighborInsertionIndex);
                //        //insertIndex = insertIndex < 0 ? 0 : insertIndex;
                //        //
                //        //zeroInDegree.Insert(insertIndex, neighbor);
                //
                //        zeroInDegree.Add(neighbor);
                //        zeroInDegree = zeroInDegree.OrderBy(id => GetLayerInfo(id).insertionIndex).ToList();
                //    }
                //}
            }

            if (result.Count != nodes.Length)
            {
                var cycle = FindCycle(adjacency, nodes);
                string details = string.Empty;
                if (cycle != null)
                {
                    var cycleDependencies = GetCycleDependencies(cycle, adjacency);
                    details = $" Cycle edges path: {string.Join(", ", cycleDependencies)}";
                }
                throw new InvalidOperationException("Cyclic dependency detected." + details);
            }

            return result.ToArray();
        }
        #endregion

        #region FindCycles
        private List<LayerID> FindCycle(
            List<(LayerID To, int DependencyIndex)>[] adjacency,
            LayerID[] nodes)
        {
            var visited = new Dictionary<LayerID, bool>();
            var recursionStack = new Stack<LayerID>();

            foreach (var node in nodes)
            {
                if (FindCycleDFS(node, adjacency, visited, recursionStack))
                {
                    return recursionStack.Reverse().ToList();
                }
            }
            return null;
        }
        private bool FindCycleDFS(
            LayerID node,
            List<(LayerID To, int DependencyIndex)>[] adjacency,
            Dictionary<LayerID, bool> visited,
            Stack<LayerID> recursionStack)
        {
            if (!visited.TryGetValue(node, out bool isVisited))
            {
                visited[node] = true;
                recursionStack.Push(node);

                foreach (var (neighbor, _) in adjacency[(int)node])
                {
                    if (!visited.ContainsKey(neighbor) && FindCycleDFS(neighbor, adjacency, visited, recursionStack))
                    {
                        return true;
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        recursionStack.Push(neighbor);
                        return true;
                    }
                }

                recursionStack.Pop();
                return false;
            }
            return isVisited && recursionStack.Contains(node);
        }

        private string[] GetCycleDependencies(
            List<LayerID> cycle,
            List<(LayerID To, int DependencyIndex)>[] adjacency)
        {
            var cycleEdges = new HashSet<(LayerID, LayerID)>();
            for (int i = 0; i < cycle.Count - 1; i++)
            {
                cycleEdges.Add((cycle[i], cycle[i + 1]));
            }

            var dependencies = new List<string>();
            foreach (var from in cycle)
            {
                foreach (var (to, depIndex) in adjacency[(int)from])
                {
                    if (cycleEdges.Contains((from, to)) && _dependencies.Count > depIndex)
                    {
                        var dep = _dependencies[depIndex];
                        dependencies.Add($"{GetLayerInfo(dep.From).name}->{GetLayerInfo(dep.To).name}");
                    }
                }
            }
            return dependencies.Distinct().ToArray();
        }
        #endregion

        #region Other
        public bool Contains(string layer) { return GetLayerInfo(GetLayerID(layer)).isContained; }

        public Enumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator<string> IEnumerable<string>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<string>
        {
            private LayersMap _map;
            private int _index;
            public Enumerator(LayersMap map)
            {
                _map = map;
                _index = -1;
            }
            public string Current
            {
                get { return _map._layerInfos._items[_index].name; }
            }
            object IEnumerator.Current { get { return Current; } }
            public bool MoveNext()
            {
                if (_index++ >= _map._layerInfos.Count) { return false; }
                ref var info = ref _map._layerInfos._items[_index];
                if(info.isContained == false)
                {
                    return MoveNext();
                }
                else
                {
                    return true;
                }
            }
            public void Reset() { _index = -1; }
            public void Dispose() { }
        }
        #endregion

        #region Obsolete
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, string newLayer)
        {
            Add(newLayer).Before(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, string newLayer)
        {
            Add(newLayer).After(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, string newLayer)
        {
            Move(newLayer).Before(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, string newLayer)
        {
            Move(newLayer).After(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).Before(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).After(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).Before(targetLayer);
            return _source;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).After(targetLayer);
            return _source;
        }

        [Obsolete]
        public object this[int index]
        {
            get
            {
                int i = 0;
                foreach (var item in this)
                {
                    if (i == index)
                    {
                        return item;
                    }
                    i++;
                }
                return null;
            }
        }
        [Obsolete("Use MergeWith(LayersMap)")]
        public void MergeWith(IReadOnlyList<string> other)
        {
            foreach (var layer in other)
            {
                Add(layer);
            }
        }
        #endregion
    }
}
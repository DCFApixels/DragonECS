using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace DCFApixels.DragonECS.Core
{
    using VertextID = DependencyGraphVertextID;
    public enum DependencyGraphVertextID : short { NULL = 0 }
    public interface IDependencyGraph<T>
    {
        int Count { get; }
        VertextID AddVertex(T vertex, bool isLocked);
        bool ContainsVertex(T vertex);
        VertextID GetVertexID(T vertex);
        bool RemoveVertex(T vertex);
        void AddBeforeDependency(VertextID vertexID, VertextID otherVertexID);
        void AddAfterDependency(VertextID vertexID, VertextID otherVertexID);
        T[] Sort();
    }
    public class LayersMap
    {
        private readonly IDependencyGraph<string> _graph;
        private readonly EcsPipeline.Builder _pipelineBuilder;

        #region Properties
        public EcsPipeline.Builder Back
        {
            get { return _pipelineBuilder; }
        }
        public int Count
        {
            get { return _graph.Count; }
        }
        #endregion

        #region Constructors
        public LayersMap(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder)
        {
            _graph = graph;
            _pipelineBuilder = pipelineBuilder;
        }
        public LayersMap(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, string preBeginlayer, string beginlayer, string basicLayer, string endLayer, string postEndLayer)
        {
            _graph = graph;
            _pipelineBuilder = pipelineBuilder;

            graph.AddVertex(preBeginlayer, true);
            graph.AddVertex(beginlayer, true);
            graph.AddVertex(basicLayer, true);
            graph.AddVertex(endLayer, true);
            graph.AddVertex(postEndLayer, true);

            Move(preBeginlayer);
                //.Before(beginlayer);
            Move(beginlayer)
                //.Before(basicLayer)
                .After(preBeginlayer);
            Move(basicLayer)
                //.Before(endLayer)
                .After(beginlayer);
            Move(endLayer)
                //.Before(postEndLayer)
                .After(basicLayer);
            Move(postEndLayer)
                .After(endLayer);
        }
        #endregion

        #region Add
        public MoveHandler Add(string layer)
        {
            VertextID id = _graph.AddVertex(layer, false);
            return new MoveHandler(_graph, _pipelineBuilder, id);
        }
        public MoveHandler Add(params string[] layers)
        {
            return Add(layersRange: layers);
        }
        public MoveHandler Add(IEnumerable<string> layersRange)
        {
            foreach (var layer in layersRange)
            {
                Add(layer);
            }
            return new MoveHandler(_graph, _pipelineBuilder, layersRange);
        }
        #endregion

        #region Move
        public MoveHandler Move(string layer)
        {
            VertextID id = _graph.GetVertexID(layer);
            return new MoveHandler(_graph, _pipelineBuilder, id);
        }
        public MoveHandler Move(params string[] layers)
        {
            return new MoveHandler(_graph, _pipelineBuilder, layers);
        }
        public MoveHandler Move(IEnumerable<string> layersRange)
        {
            return new MoveHandler(_graph, _pipelineBuilder, layersRange);
        }
        #endregion

        public struct MoveHandler
        {
            private readonly IDependencyGraph<string> _graph;
            private readonly EcsPipeline.Builder _pipelineBuilder;
            private readonly VertextID _layerID;
            private readonly IEnumerable<string> _layersRange;

            #region Properties
            public EcsPipeline.Builder Back
            {
                get { return _pipelineBuilder; }
            }
            #endregion

            #region Constructors
            public MoveHandler(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, VertextID id)
            {
                _graph = graph;
                _pipelineBuilder = pipelineBuilder;
                _layerID = id;
                _layersRange = null;
            }
            public MoveHandler(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, IEnumerable<string> layersRange)
            {
                _graph = graph;
                _pipelineBuilder = pipelineBuilder;
                _layerID = VertextID.NULL;
                _layersRange = layersRange;
            }
            #endregion

            #region Before
            public MoveHandler Before(params string[] targets)
            {
                return Before(targetsRange: targets);
            }
            public MoveHandler Before(IEnumerable<string> targetsRange)
            {
                foreach (var target in targetsRange)
                {
                    Before(target);
                }
                return this;
            }
            public MoveHandler Before(string targetLayer)
            {
                if (_layerID != VertextID.NULL)
                {
                    _graph.AddBeforeDependency(_layerID, _graph.GetVertexID(targetLayer));
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _graph.AddBeforeDependency(_graph.GetVertexID(layer), _graph.GetVertexID(targetLayer));
                    }
                }
                return this;
            }
            #endregion

            #region After
            public MoveHandler After(params string[] targets)
            {
                return After(targetsRange: targets);
            }
            public MoveHandler After(IEnumerable<string> targetsRange)
            {
                foreach (var target in targetsRange)
                {
                    After(target);
                }
                return this;
            }
            public MoveHandler After(string targetLayer)
            {
                if (_layerID != VertextID.NULL)
                {
                    _graph.AddAfterDependency(_graph.GetVertexID(targetLayer), _layerID);
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _graph.AddBeforeDependency(_graph.GetVertexID(targetLayer), _graph.GetVertexID(layer));
                    }
                }
                return this;
            }
            #endregion
        }

        #region Other
        public bool Contains(string layer) { return _graph.ContainsVertex(layer); }

        //public Enumerator GetEnumerator() { return new Enumerator(this); }
        //IEnumerator<string> IEnumerable<string>.GetEnumerator() { return GetEnumerator(); }
        //IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        //public struct Enumerator : IEnumerator<string>
        //{
        //    private DependencyGraph _map;
        //    private int _index;
        //    public Enumerator(DependencyGraph map)
        //    {
        //        _map = map;
        //        _index = -1;
        //    }
        //    public string Current
        //    {
        //        get { return _map._vertexInfos._items[_index].name; }
        //    }
        //    object IEnumerator.Current { get { return Current; } }
        //    public bool MoveNext()
        //    {
        //        if (_index++ >= _map._vertexInfos.Count) { return false; }
        //        ref var info = ref _map._vertexInfos._items[_index];
        //        if (info.isContained == false)
        //        {
        //            return MoveNext();
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }
        //    public void Reset() { _index = -1; }
        //    public void Dispose() { }
        //}
        #endregion

        #region Build
        public string[] Build()
        {
            return _graph.Sort();
        }
        #endregion

        #region Obsolete
        [Obsolete("Use " + nameof(DependencyGraph) + ".Add(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, string newLayer)
        {
            Add(newLayer).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Add(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, string newLayer)
        {
            Add(newLayer).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Move(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, string newLayer)
        {
            Move(newLayer).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Move(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, string newLayer)
        {
            Move(newLayer).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Add(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Add(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Move(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(DependencyGraph) + ".Move(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).After(targetLayer);
            return _pipelineBuilder;
        }

        [Obsolete]
        public object this[int index]
        {
            get
            {
                //int i = 0;
                //foreach (var item in this)
                //{
                //    if (i == index)
                //    {
                //        return item;
                //    }
                //    i++;
                //}
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



    public partial class DependencyGraph : IEnumerable<string>, IDependencyGraph<string>
    {
        #region IDependencyGraph<string>
        VertextID IDependencyGraph<string>.AddVertex(string vertex, bool isLocked)
        {
            var result = GetVertexID(vertex);
            Add_Internal(result);
            if (isLocked)
            {
                LockVertex(vertex);
            }
            return result;
        }
        bool IDependencyGraph<string>.ContainsVertex(string vertex)
        {
            return GetVertexInfo(GetVertexID(vertex)).isContained;
        }
        VertextID IDependencyGraph<string>.GetVertexID(string vertex)
        {
            return GetVertexID(vertex);
        }
        bool IDependencyGraph<string>.RemoveVertex(string vertex)
        {
            var result = GetVertexID(vertex);
            return Remove_Internal(result);
        }
        void IDependencyGraph<string>.AddAfterDependency(VertextID fromoVertexID, VertextID toVertexID)
        {
            AddDependency_Internal(fromoVertexID, toVertexID, false);
        }
        void IDependencyGraph<string>.AddBeforeDependency(VertextID fromoVertexID, VertextID toVertexID)
        {
            AddDependency_Internal(fromoVertexID, toVertexID, true);
        }
        string[] IDependencyGraph<string>.Sort()
        {
            return Build();
        }
        #endregion

        private readonly Dictionary<string, VertextID> _vertexIDs = new Dictionary<string, VertextID>(32);
        private StructList<VertexInfo> _vertexInfos = new StructList<VertexInfo>(32);
        private VertextID GetVertexID(string vertext)
        {
            if (_vertexIDs.TryGetValue(vertext, out VertextID layerID) == false)
            {
                layerID = (VertextID)_vertexInfos.Count;
                _vertexInfos.Add(default);

                _vertexIDs[vertext] = layerID;
                ref var layerInfo = ref GetVertexInfo(layerID);
                layerInfo.name = vertext;
            }
            return layerID;
        }
        private ref VertexInfo GetVertexInfo(VertextID vertexID)
        {
            return ref _vertexInfos._items[(int)vertexID];
        }
        [DebuggerDisplay("{name}")]
        private struct VertexInfo
        {
            public string name;
            public int insertionIndex;
            public bool isLocked;
            public bool isContained;
            public bool isBefore;
            //build
            public bool hasAnyDependency;
            public int inDegree;
            public int sortingIndex;
            public int leftBeforeIndex;
            public VertexInfo(string name) : this()
            {
                this.name = name;
            }
        }

        private List<(VertextID from, VertextID to)> _dependencies = new List<(VertextID, VertextID)>(16);
        private readonly VertextID _basicVertexID;
        private int _increment = 0;
        private int _count;

        public IEnumerable<(string from, string to)> Deps
        {
            get
            {
                return _dependencies.Select(o => (GetVertexInfo(o.from).name, GetVertexInfo(o.to).name));
            }
        }

        #region Properties
        public int Count
        {
            get { return _count; }
        }
        #endregion

        #region Constructors
        public DependencyGraph()
        {
            GetVertexID("");
            _basicVertexID = VertextID.NULL;
        }
        public DependencyGraph(string basicVertexName)
        {
            GetVertexID("");
            _basicVertexID = GetVertexID(basicVertexName);
            LockVertex(basicVertexName);
        }
        #endregion

        #region Methods
        private void LockVertex(string veretex)
        {
            GetVertexInfo(GetVertexID(veretex)).isLocked = true;
        }
        private void Add_Internal(VertextID id)
        {
            ref var info = ref GetVertexInfo(id);
            if (info.isContained == false || info.isLocked == false)
            {
                _count++;
                info.isContained = true;
                info.insertionIndex = _increment++;
            }
        }
        private bool Remove_Internal(VertextID id)
        {
            ref var info = ref GetVertexInfo(id);
            bool result = false;
            if (info.isLocked) { throw new Exception($"The {info.name} vertex cannot be removed"); }
            if (info.isContained)
            {
                _count--;
                info.isContained = false;
                result = true;
            }
            info.insertionIndex = 0;
            return result;
        }
        private void AddDependency_Internal(VertextID from, VertextID to, bool isBefore = false)
        {

            ref var fromInfo = ref GetVertexInfo(from);
            ref var toInfo = ref GetVertexInfo(to);
            fromInfo.hasAnyDependency = true;
            toInfo.hasAnyDependency = true;

            if (isBefore)
            {
                //GetLayerInfo(from).insertionIndex = 1000 - (_increment++);
                fromInfo.isBefore = true;
                //_dependencies.Insert(0, (from, to));
            }
            else
            {
                //GetLayerInfo(from).insertionIndex = _increment++;
                fromInfo.isBefore = false;
                //_dependencies.Add((from, to));
            }
            //_dependencies.Insert(0, (from, to));
            _dependencies.Add((from, to));
        }
        private void AddDependency_Internal(string from, string to, bool isBefore = false)
        {
            AddDependency_Internal(GetVertexID(from), GetVertexID(to), isBefore);
        }
        #endregion

        #region MergeWith
        public void MergeWith(DependencyGraph other)
        {
            foreach (var otherDependency in other._dependencies)
            {
                AddDependency_Internal(other.GetVertexInfo(otherDependency.from).name, other.GetVertexInfo(otherDependency.to).name);
            }
            for (int i = 0; i < other._vertexInfos.Count; i++)
            {
                VertextID otherLayerID = (VertextID)i;
                ref var otherLayerInfo = ref other.GetVertexInfo(otherLayerID);
                Add_Internal(GetVertexID(otherLayerInfo.name));
            }
        }
        #endregion

        #region Build
        private unsafe void TopoSorting(UnsafeArray<VertextID> sortingBuffer)
        {
            VertextID[] nodes = new VertextID[_count];
            var adjacency = new List<(VertextID To, int DependencyIndex)>[_vertexInfos.Count];

            for (int i = 0, j = 0; i < _vertexInfos.Count; i++)
            {
                VertextID layerID = (VertextID)i;
                ref var info = ref GetVertexInfo(layerID);
                adjacency[(int)layerID] = new List<(VertextID To, int DependencyIndex)>();
                _vertexInfos._items[(int)layerID].inDegree = 0;
                if (info.isContained)
                {
                    nodes[j++] = layerID;
                }
            }

            for (int i = 0; i < _dependencies.Count; i++)
            {
                var (from, to) = _dependencies[i];
                ref var fromInfo = ref GetVertexInfo(from);
                ref var toInfo = ref GetVertexInfo(to);

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

            List<VertextID> zeroInDegree = new List<VertextID>(nodes.Length);
            zeroInDegree.AddRange(nodes.Where(id => GetVertexInfo(id).inDegree == 0).OrderBy(id => GetVertexInfo(id).insertionIndex));

            //List<string> result = new List<string>(nodes.Length);
            int resultCount = 0;

            while (zeroInDegree.Count > 0)
            {
                var current = zeroInDegree[0];
                zeroInDegree.RemoveAt(0);

                //result.Add(GetVertexInfo(current).name);
                GetVertexInfo(current).sortingIndex = resultCount;
                sortingBuffer.ptr[resultCount++] = current;

                var adjacencyList = adjacency[(int)current];
                //for (int i = adjacencyList.Count - 1; i >= 0; i--)
                for (int i = 0; i < adjacencyList.Count; i++)
                {
                    var (neighbor, _) = adjacencyList[i];
                    ref var neighborInfo = ref GetVertexInfo(neighbor);
                    neighborInfo.inDegree--;
                    if (neighborInfo.inDegree == 0)
                    {
                        var neighborInsertionIndex = neighborInfo.insertionIndex;
                        int insertIndex = zeroInDegree.FindIndex(id => GetVertexInfo(id).insertionIndex < neighborInsertionIndex);
                        insertIndex = insertIndex < 0 ? 0 : insertIndex;
                        zeroInDegree.Insert(insertIndex, neighbor);

                        //zeroInDegree.Add(neighbor);
                        //zeroInDegree = zeroInDegree.OrderBy(id => GetLayerInfo(id).insertionIndex).ToList();
                    }
                }
            }

            if (resultCount != nodes.Length)
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
        }
        private unsafe void ReoderInsertionIndexes(UnsafeArray<VertextID> sortingBuffer)
        {
            for (int i = 0; i < _vertexInfos.Count; i++)
            {
                ref var info = ref _vertexInfos._items[i];
                if (info.isContained == false) { continue; }
                info.leftBeforeIndex = info.isBefore ? int.MaxValue : 0;
            }

            foreach (var dependency in _dependencies)
            {
                ref var fromInfo = ref GetVertexInfo(dependency.from);
                if (fromInfo.isBefore)
                {
                    ref var toInfo = ref GetVertexInfo(dependency.to);
                    fromInfo.leftBeforeIndex = Math.Min(toInfo.sortingIndex, fromInfo.leftBeforeIndex);
                }
            }

            for (int i = sortingBuffer.Length - 1; i >= 0; i--)
            {
                var id = sortingBuffer.ptr[i];
                ref var info = ref GetVertexInfo(id);
                if (info.isBefore)
                {
                    if (info.leftBeforeIndex < sortingBuffer.Length)
                    {
                        MoveElement(ref sortingBuffer, i, info.leftBeforeIndex - 1);
                    }
                }
            }

            for (int i = 0; i < sortingBuffer.Length; i++)
            {
                ref var info = ref GetVertexInfo(sortingBuffer.ptr[i]);
                info.insertionIndex = i;

            }
        }
        private static unsafe void MoveElement<T>(ref UnsafeArray<T> array, int oldIndex, int newIndex) where T : unmanaged
        {
            var ptr = array.ptr;
            if (oldIndex == newIndex) return;

            // Сохраняем элемент для перемещения
            T item = ptr[oldIndex];

            int elementSize = sizeof(T);
            int copyLength = Math.Abs(newIndex - oldIndex);

            byte* source;
            byte* destination;
            long bytesToCopy = copyLength * elementSize;

            if (oldIndex < newIndex)
            {
                // Сдвиг вправо: копируем блок [oldIndex+1 ... newIndex] в [oldIndex ... newIndex-1]
                source = (byte*)(ptr + oldIndex + 1);
                destination = (byte*)(ptr + oldIndex);
            }
            else
            {
                // Сдвиг влево: копируем блок [newIndex ... oldIndex-1] в [newIndex+1 ... oldIndex]
                source = (byte*)(ptr + newIndex);
                destination = (byte*)(ptr + newIndex + 1);
            }

            // Копируем память
            Buffer.MemoryCopy(source: source, destination: destination, destinationSizeInBytes: bytesToCopy, sourceBytesToCopy: bytesToCopy);

            // Вставляем сохраненный элемент
            ptr[newIndex] = item;
        }

        public unsafe string[] Build()
        {
            const int BUFFER_THRESHOLD = 256;
            if(_count <= BUFFER_THRESHOLD)
            {
                var ptr = stackalloc VertextID[_count];
                var buffer = UnsafeArray<VertextID>.Manual(ptr, _count);
                TopoSorting(buffer);
                ReoderInsertionIndexes(buffer);
                TopoSorting(buffer);
                return buffer.Select(id => GetVertexInfo(id).name).ToArray();
            }
            else
            {
                var ptr = TempBuffer<VertextID>.Get(_count);
                var buffer = UnsafeArray<VertextID>.Manual(ptr, _count);
                TopoSorting(buffer);
                ReoderInsertionIndexes(buffer);
                TopoSorting(buffer);
                return buffer.Select(id => GetVertexInfo(id).name).ToArray();
            }
        }
        #endregion

        #region FindCycles
        private List<VertextID> FindCycle(
            List<(VertextID To, int DependencyIndex)>[] adjacency,
            VertextID[] nodes)
        {
            var visited = new Dictionary<VertextID, bool>();
            var recursionStack = new Stack<VertextID>();

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
            VertextID node,
            List<(VertextID To, int DependencyIndex)>[] adjacency,
            Dictionary<VertextID, bool> visited,
            Stack<VertextID> recursionStack)
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
            List<VertextID> cycle,
            List<(VertextID To, int DependencyIndex)>[] adjacency)
        {
            var cycleEdges = new HashSet<(VertextID, VertextID)>();
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
                        dependencies.Add($"{GetVertexInfo(dep.from).name}->{GetVertexInfo(dep.to).name}");
                    }
                }
            }
            return dependencies.Distinct().ToArray();
        }
        #endregion

        #region Other
        public bool Contains(string layer) { return GetVertexInfo(GetVertexID(layer)).isContained; }

        public Enumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator<string> IEnumerable<string>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<string>
        {
            private DependencyGraph _map;
            private int _index;
            public Enumerator(DependencyGraph map)
            {
                _map = map;
                _index = -1;
            }
            public string Current
            {
                get { return _map._vertexInfos._items[_index].name; }
            }
            object IEnumerator.Current { get { return Current; } }
            public bool MoveNext()
            {
                if (_index++ >= _map._vertexInfos.Count) { return false; }
                ref var info = ref _map._vertexInfos._items[_index];
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
    }
}
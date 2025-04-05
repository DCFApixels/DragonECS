using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DCFApixels.DragonECS.Core
{
    using VertexID = DependencyGraphVertextID;
    public class LayersMap : IDependencyGraph<string>
    {
        #region IDependencyGraph
        VertexID IDependencyGraph<string>.AddVertex(string vertex, bool isLocked) { return _graph.AddVertex(vertex, isLocked); }
        bool IDependencyGraph<string>.ContainsVertex(string vertex) { return _graph.ContainsVertex(vertex); }
        VertexID IDependencyGraph<string>.GetVertexID(string vertex) { return _graph.GetVertexID(vertex); }
        string IDependencyGraph<string>.GetVertexFromID(VertexID vertexID) { return _graph.GetVertexFromID(vertexID); }
        bool IDependencyGraph<string>.RemoveVertex(string vertex) { return _graph.RemoveVertex(vertex); }
        void IDependencyGraph<string>.AddDependency(VertexID fromID, VertexID toID, bool moveToRight) { _graph.AddDependency(fromID, toID, moveToRight); }
        string[] IDependencyGraph<string>.Sort() { return _graph.Sort(); }
        #endregion

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
        public ReadonlyDependenciesCollection<string> Dependencies
        {
            get { return _graph.Dependencies; }
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
            VertexID id = _graph.AddVertex(layer, false);
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
            VertexID id = _graph.GetVertexID(layer);
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

        #region MoveHandler
        public struct MoveHandler
        {
            private readonly IDependencyGraph<string> _graph;
            private readonly EcsPipeline.Builder _pipelineBuilder;
            private readonly VertexID _layerID;
            private readonly IEnumerable<string> _layersRange;

            #region Properties
            public EcsPipeline.Builder Back
            {
                get { return _pipelineBuilder; }
            }
            #endregion

            #region Constructors
            public MoveHandler(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, VertexID id)
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
                _layerID = VertexID.NULL;
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
                if (_layerID != VertexID.NULL)
                {
                    _graph.AddDependency(_layerID, _graph.GetVertexID(targetLayer), true);
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _graph.AddDependency(_graph.GetVertexID(layer), _graph.GetVertexID(targetLayer), true);
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
                if (_layerID != VertexID.NULL)
                {
                    _graph.AddDependency(_graph.GetVertexID(targetLayer), _layerID, false);
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _graph.AddDependency(_graph.GetVertexID(targetLayer), _graph.GetVertexID(layer), false);
                    }
                }
                return this;
            }
            #endregion
        }
        #endregion

        #region MergeWith
        public void MergeWith(IDependencyGraph<string> other)
        {
            _graph.MergeWith(other);
        }
        #endregion;

        #region Other
        public bool Contains(string layer)
        {
            return _graph.ContainsVertex(layer);
        }
        public IEnumerator<string> GetEnumerator() { return _graph.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _graph.GetEnumerator(); }
        #endregion

        #region Build
        public string[] Build()
        {
            return _graph.Sort();
        }
        #endregion

        #region Obsolete
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, string newLayer)
        {
            Add(newLayer).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, string newLayer)
        {
            Add(newLayer).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, string newLayer)
        {
            Move(newLayer).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, string newLayer)
        {
            Move(newLayer).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layers).After(targetLayer).Back;")]
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

    public enum DependencyGraphVertextID : short { NULL = 0 }
    public interface IDependencyGraph<T> : IReadOnlyCollection<string>
    {
        ReadonlyDependenciesCollection<T> Dependencies { get; }
        VertexID AddVertex(T vertex, bool isLocked);
        bool ContainsVertex(T vertex);
        VertexID GetVertexID(T vertex);
        T GetVertexFromID(VertexID vertexID);
        bool RemoveVertex(T vertex);
        void AddDependency(VertexID fromID, VertexID toID, bool moveToRight);
        void MergeWith(IDependencyGraph<T> other);
        T[] Sort();
    }
    public static class DependencyGraphExtensions
    {
        public static void AddDependency<T>(this IDependencyGraph<T> self, T from, T to, bool moveToRight)
        {
            self.AddDependency(self.GetVertexID(from), self.GetVertexID(to), moveToRight);
        }
    }
    public struct ReadonlyDependenciesCollection<T> : IReadOnlyCollection<(T from, T to)>
    {
        private IDependencyGraph<T> _graph;
        private IReadOnlyCollection<(VertexID from, VertexID to)> _source;
        public int Count
        {
            get { return _source.Count; }
        }
        public ReadonlyDependenciesCollection(IDependencyGraph<T> graph, IReadOnlyCollection<(VertexID from, VertexID to)> source)
        {
            _graph = graph;
            _source = source;
        }
        public Enumerator GetEnumerator() { return new Enumerator(_graph, _source.GetEnumerator()); }
        IEnumerator<(T from, T to)> IEnumerable<(T from, T to)>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<(T from, T to)>
        {
            private IDependencyGraph<T> _graph;
            private IEnumerator<(VertexID from, VertexID to)> _source;
            public Enumerator(IDependencyGraph<T> graph, IEnumerator<(VertexID from, VertexID to)> source)
            {
                _graph = graph;
                _source = source;
            }
            public (T from, T to) Current
            {
                get
                {
                    var (from, to) = _source.Current;
                    return (_graph.GetVertexFromID(from), _graph.GetVertexFromID(to));
                }
            }
            object IEnumerator.Current { get { return Current; } }
            public bool MoveNext() { return _source.MoveNext(); }
            public void Reset() { _source.Reset(); }
            public void Dispose() { }
        }
    }
    public unsafe partial class DependencyGraph : IDependencyGraph<string>
    {
        private readonly Dictionary<string, VertexID> _vertexIDs = new Dictionary<string, VertexID>(32);
        private StructList<VertexInfo> _vertexInfos = new StructList<VertexInfo>(32);

        private List<(VertexID from, VertexID to)> _dependencies = new List<(VertexID, VertexID)>(16);
        private readonly VertexID _basicVertexID;
        private int _increment = 0;
        private int _count;

        #region Properties
        public int Count
        {
            get { return _count; }
        }
        public ReadonlyDependenciesCollection<string> Dependencies
        {
            get { return new ReadonlyDependenciesCollection<string>(this, _dependencies); }
        }
        #endregion

        #region Constructors
        public DependencyGraph()
        {
            GetVertexID("");
            _basicVertexID = VertexID.NULL;
        }
        public DependencyGraph(string basicVertexName)
        {
            GetVertexID("");
            _basicVertexID = GetVertexID(basicVertexName);
            LockVertex(basicVertexName);
        }
        #endregion

        #region Methods
        public VertexID GetVertexID(string vertext)
        {
            if (_vertexIDs.TryGetValue(vertext, out VertexID layerID) == false)
            {
                layerID = (VertexID)_vertexInfos.Count;
                _vertexInfos.Add(default);

                _vertexIDs[vertext] = layerID;
                ref var layerInfo = ref GetVertexInfo(layerID);
                layerInfo.value = vertext;
            }
            return layerID;
        }
        public string GetVertexFromID(VertexID vertexID)
        {
            return GetVertexInfo(vertexID).value;
        }
        private ref VertexInfo GetVertexInfo(VertexID vertexID)
        {
            return ref _vertexInfos._items[(int)vertexID];
        }
        private ref VertexInfo GetVertexInfo(int vertexID)
        {
            return ref _vertexInfos._items[(int)vertexID];
        }
        private int GetVertexInfosCount()
        {
            return _vertexInfos.Count;
        }
        public VertexID AddVertex(string vertex, bool isLocked)
        {
            var result = GetVertexID(vertex);
            AddVertexByID(result);
            if (isLocked)
            {
                LockVertex(result);
            }
            return result;
        }
        private void LockVertex(string vertex)
        {
            LockVertex(GetVertexID(vertex));
        }
        private void LockVertex(VertexID vertexID)
        {
            GetVertexInfo(vertexID).isLocked = true;
        }
        private void AddVertexByID(VertexID id)
        {
            ref var info = ref GetVertexInfo(id);
            if (info.isContained == false || info.isLocked == false)
            {
                _count++;
                info.isContained = true;
            }
            info.insertionIndex = _increment++;
        }
        public bool RemoveVertex(string vertex)
        {
            var result = GetVertexID(vertex);
            return RemoveVertexByID(result);
        }
        private bool RemoveVertexByID(VertexID id)
        {
            ref var info = ref GetVertexInfo(id);
            bool result = false;
            if (info.isLocked) { throw new Exception($"The {info.value} vertex cannot be removed"); }
            if (info.isContained)
            {
                _count--;
                info.isContained = false;
                result = true;
            }
            info.insertionIndex = 0;
            return result;
        }
        public void AddDependency(VertexID fromVertexID, VertexID toVertexID, bool moveToRight)
        {
            ref var fromInfo = ref GetVertexInfo(fromVertexID);
            ref var toInfo = ref GetVertexInfo(toVertexID);
            fromInfo.hasAnyDependency = true;
            toInfo.hasAnyDependency = true;
            fromInfo.moveToRight = moveToRight;
            _dependencies.Add((fromVertexID, toVertexID));
        }
        #endregion

        #region MergeWith
        public void MergeWith(IDependencyGraph<string> other)
        {
            if(other is DependencyGraph graph)
            {
                foreach (var otherDependency in graph._dependencies)
                {
                    this.AddDependency(graph.GetVertexFromID(otherDependency.from), graph.GetVertexFromID(otherDependency.to), false);
                }
                for (int i = 0; i < graph.GetVertexInfosCount(); i++)
                {
                    ref var otherLayerInfo = ref graph.GetVertexInfo(i);
                    AddVertexByID(GetVertexID(graph.GetVertexFromID((VertexID)i)));
                }
            }
            foreach (var otherDependency in other.Dependencies)
            {
                this.AddDependency(otherDependency.from, otherDependency.to, false);
            }
            foreach (var vertex in other)
            {
                AddVertex(vertex, false);
            }
        }
        #endregion

        #region Sort
        public string[] Sort()
        {
            const int BUFFER_THRESHOLD = 256;
            if (_count <= BUFFER_THRESHOLD)
            {
                var ptr = stackalloc VertexID[_count];
                var buffer = UnsafeArray<VertexID>.Manual(ptr, _count);
                TopoSorting(buffer);
                ReoderInsertionIndexes(buffer);
                TopoSorting(buffer);
                return ConvertIdsToStringsArray(buffer);
            }
            else
            {
                var ptr = TempBuffer<VertexID>.Get(_count);
                var buffer = UnsafeArray<VertexID>.Manual(ptr, _count);
                TopoSorting(buffer);
                ReoderInsertionIndexes(buffer);
                TopoSorting(buffer);
                return ConvertIdsToStringsArray(buffer);
            }
        }
        private void TopoSorting(UnsafeArray<VertexID> sortingBuffer)
        {
            VertexID[] nodes = new VertexID[_count];
            var adjacency = new List<(VertexID To, int DependencyIndex)>[GetVertexInfosCount()];

            for (int i = 0, j = 0; i < GetVertexInfosCount(); i++)
            {
                VertexID layerID = (VertexID)i;
                ref var info = ref GetVertexInfo(layerID);
                adjacency[(int)layerID] = new List<(VertexID To, int DependencyIndex)>();
                GetVertexInfo(layerID).inDegree = 0;
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

            // добавление зависимостей для нод без зависимостей.
            if (_basicVertexID != VertexID.NULL)
            {
                var basicLayerAdjacencyList = adjacency[(int)_basicVertexID];
                int inserIndex = basicLayerAdjacencyList.Count;
                for (int i = 0; i < GetVertexInfosCount(); i++)
                {
                    var toID = (VertexID)i;
                    ref var toInfo = ref GetVertexInfo(i);
                    if(toInfo.isContained && toInfo.hasAnyDependency == false)
                    {
                        basicLayerAdjacencyList.Insert(inserIndex, (toID, toInfo.insertionIndex));
                        toInfo.inDegree += 1;
                    }
                }
            }

            List<VertexID> zeroInDegree = new List<VertexID>(nodes.Length);
            zeroInDegree.AddRange(nodes.Where(id => GetVertexInfo(id).inDegree == 0).OrderBy(id => GetVertexInfo(id).insertionIndex));

            int resultCount = 0;

            while (zeroInDegree.Count > 0)
            {
                var current = zeroInDegree[0];
                zeroInDegree.RemoveAt(0);

                GetVertexInfo(current).sortingIndex = resultCount;
                sortingBuffer.ptr[resultCount++] = current;

                var adjacencyList = adjacency[(int)current];
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
        private void ReoderInsertionIndexes(UnsafeArray<VertexID> sortingBuffer)
        {
            for (int i = 0; i < GetVertexInfosCount(); i++)
            {
                ref var info = ref GetVertexInfo(i);
                if (info.isContained == false) { continue; }
                info.leftBeforeIndex = info.moveToRight ? int.MaxValue : 0;
            }

            foreach (var dependency in _dependencies)
            {
                ref var fromInfo = ref GetVertexInfo(dependency.from);
                if (fromInfo.moveToRight)
                {
                    ref var toInfo = ref GetVertexInfo(dependency.to);
                    fromInfo.leftBeforeIndex = Math.Min(toInfo.sortingIndex, fromInfo.leftBeforeIndex);
                }
            }

            for (int i = sortingBuffer.Length - 1; i >= 0; i--)
            {
                var id = sortingBuffer.ptr[i];
                ref var info = ref GetVertexInfo(id);
                if (info.moveToRight)
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
        private static void MoveElement<T>(ref UnsafeArray<T> array, int oldIndex, int newIndex) where T : unmanaged
        {
            if (oldIndex == newIndex) return;

            var ptr = array.ptr;
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
            Buffer.MemoryCopy(source: source, destination: destination, destinationSizeInBytes: bytesToCopy, sourceBytesToCopy: bytesToCopy);

            ptr[newIndex] = item;
        }
        private string[] ConvertIdsToStringsArray(UnsafeArray<VertexID> buffer)
        {
            string[] result = new string[buffer.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetVertexInfo(buffer.ptr[i]).value;
            }
            return result;
        }
        #endregion

        #region FindCycles
        private List<VertexID> FindCycle(
            List<(VertexID To, int DependencyIndex)>[] adjacency,
            VertexID[] nodes)
        {
            var visited = new Dictionary<VertexID, bool>();
            var recursionStack = new Stack<VertexID>();

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
            VertexID node,
            List<(VertexID To, int DependencyIndex)>[] adjacency,
            Dictionary<VertexID, bool> visited,
            Stack<VertexID> recursionStack)
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
            List<VertexID> cycle,
            List<(VertexID To, int DependencyIndex)>[] adjacency)
        {
            var cycleEdges = new HashSet<(VertexID, VertexID)>();
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
                        dependencies.Add($"{GetVertexInfo(dep.from).value}->{GetVertexInfo(dep.to).value}");
                    }
                }
            }
            return dependencies.Distinct().ToArray();
        }
        #endregion

        #region Other
        public bool ContainsVertex(string vertex)
        {
            return GetVertexInfo(GetVertexID(vertex)).isContained;
        }
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
                get { return _map.GetVertexInfo(_index).value; }
            }
            object IEnumerator.Current { get { return Current; } }
            public bool MoveNext()
            {
                if (_index++ >= _map.GetVertexInfosCount()) { return false; }
                ref var info = ref _map.GetVertexInfo(_index);
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

        #region VertexInfo
        [DebuggerDisplay("{value}")]
        private struct VertexInfo
        {
            public string value;
            public int insertionIndex;
            public bool isLocked;
            public bool isContained;
            public bool moveToRight;
            //build
            public bool hasAnyDependency;
            public int inDegree;
            public int sortingIndex;
            public int leftBeforeIndex;
            public VertexInfo(string name) : this()
            {
                this.value = name;
            }
        }
        #endregion
    }
}
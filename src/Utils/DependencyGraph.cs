using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DCFApixels.DragonECS.Core
{
    using VertexID = DependencyGraphVertextID;
    public enum DependencyGraphVertextID : short { NULL = 0 }
    /// <summary>
    /// Represents a directed dependency graph over values of type T.
    /// Provides vertex management, dependency registration and topological sorting.
    /// </summary>
    /// <typeparam name="T">Vertex value type.</typeparam>
    public interface IDependencyGraph<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Read-only collection of registered dependency edges as pairs (from, to).
        /// </summary>
        ReadonlyDependenciesCollection<T> Dependencies { get; }
        /// <summary>
        /// Adds a vertex to the result set. Re-adding a vertex refreshes its insertion order.
        /// </summary>
        /// <param name="vertex">Vertex value to add.</param>
        /// <param name="isLocked">Whether subsequent removal attempts should throw.</param>
        /// <returns>The stable internal identifier assigned to the vertex value.</returns>
        VertexID AddVertex(T vertex, bool isLocked);
        /// <summary>
        /// Checks whether the vertex is currently included in the graph's result set.
        /// </summary>
        /// <param name="vertex">Vertex value to check.</param>
        /// <returns>True when the vertex is included; otherwise false.</returns>
        bool ContainsVertex(T vertex);
        /// <summary>
        /// Resolves the stable identifier for a vertex value, creating a virtual vertex when necessary.
        /// </summary>
        /// <param name="vertex">Vertex value to resolve.</param>
        /// <returns>The stable internal vertex identifier.</returns>
        VertexID GetVertexID(T vertex);
        /// <summary>
        /// Gets the original vertex value from its internal identifier.
        /// </summary>
        /// <param name="vertexID">Internal vertex identifier.</param>
        /// <returns>The corresponding vertex value.</returns>
        T GetVertexFromID(VertexID vertexID);
        /// <summary>
        /// Removes a vertex from the result set without removing dependency edges that reference it.
        /// </summary>
        /// <param name="vertex">Vertex value to remove.</param>
        /// <returns>True when the vertex was included and has been removed; otherwise false.</returns>
        bool RemoveVertex(T vertex);
        /// <summary>
        /// Adds a directed dependency edge from <paramref name="fromID"/> to <paramref name="toID"/>.
        /// </summary>
        /// <param name="fromID">Identifier of the vertex that must be ordered first.</param>
        /// <param name="toID">Identifier of the vertex that must be ordered later.</param>
        /// <param name="moveToRight">True to bias the source toward the right; false to bias the destination toward the left.</param>
        void AddDependency(VertexID fromID, VertexID toID, bool moveToRight);
        /// <summary>
        /// Merge another dependency graph into this graph.
        /// </summary>
        /// <param name="other">Graph whose vertices and edges should be merged.</param>
        void MergeWith(IDependencyGraph<T> other);
        /// <summary>
        /// Performs a topological sort. Virtual vertices referenced only by dependencies participate
        /// in ordering but are omitted from the returned array.
        /// </summary>
        /// <returns>The included vertices in dependency order.</returns>
        /// <exception cref="InvalidOperationException">The graph contains a dependency cycle.</exception>
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



    public class DependencyGraph<T> : IDependencyGraph<T>
    {
        private readonly Dictionary<T, VertexID> _vertexIDs = new Dictionary<T, VertexID>(32);
        private StructList<VertexInfo> _vertexInfos = new StructList<VertexInfo>(32);

        private List<(VertexID from, VertexID to)> _dependencies = new List<(VertexID, VertexID)>(16);
        private const sbyte MOVE_AFTER = -1;
        private const sbyte MOVE_NONE = 0;
        private const sbyte MOVE_BEFORE = 1;
        private readonly VertexID _basicVertexID;
        private int _increment = 1;
        private int _count;

        #region Properties
        public int Count
        {
            get { return _count; }
        }
        public ReadonlyDependenciesCollection<T> Dependencies
        {
            get { return new ReadonlyDependenciesCollection<T>(this, _dependencies); }
        }
        #endregion

        #region Constructors
        public DependencyGraph()
        {
            //GetVertexID("");
            _vertexInfos.Add(default);
            _basicVertexID = VertexID.NULL;
        }
        public DependencyGraph(T basicVertexName)
        {
            //GetVertexID("");
            _vertexInfos.Add(default);
            _basicVertexID = GetVertexID(basicVertexName);
            LockVertex(basicVertexName);
        }
        #endregion

        #region Methods
        public VertexID GetVertexID(T vertext)
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
        public T GetVertexFromID(VertexID vertexID)
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
        public VertexID AddVertex(T vertex, bool isLocked)
        {
            var result = GetVertexID(vertex);
            AddVertexByID(result);
            if (isLocked)
            {
                LockVertex(result);
            }
            return result;
        }
        private void LockVertex(T vertex)
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

            // Every Add refreshes ordering, including repeated Add for locked vertices.
            // Locking controls removal only.
            info.insertionIndex = _increment++;
            if (info.isContained == false)
            {
                _count++;
                info.isContained = true;
            }
        }
        public bool RemoveVertex(T vertex)
        {
            if (_vertexIDs.TryGetValue(vertex, out VertexID id) == false)
            {
                return false;
            }
            return RemoveVertexByID(id);
        }
        private bool RemoveVertexByID(VertexID id)
        {
            ref var info = ref GetVertexInfo(id);
            bool result = false;
            if (info.isLocked) { Throw.DependencyGraph_LockedVertexCannotBeRemoved(info.value); }
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
            // Before moves the source to the right; After moves the destination to the
            // left. Assigning the mode here also implements last-operation-wins.
            if (moveToRight)
            {
                fromInfo.moveDirection = MOVE_BEFORE;
            }
            else
            {
                toInfo.moveDirection = MOVE_AFTER;
            }
            _dependencies.Add((fromVertexID, toVertexID));
        }
        #endregion

        #region MergeWith
        public void MergeWith(IDependencyGraph<T> other)
        {
            if (other is DependencyGraph<T> graph)
            {
                foreach (var otherDependency in graph._dependencies)
                {
                    this.AddDependency(graph.GetVertexFromID(otherDependency.from), graph.GetVertexFromID(otherDependency.to), false);
                }
                for (int i = 1; i < graph.GetVertexInfosCount(); i++)
                {
                    ref var otherLayerInfo = ref graph.GetVertexInfo(i);
                    if (otherLayerInfo.isContained)
                    {
                        AddVertex(graph.GetVertexFromID((VertexID)i), otherLayerInfo.isLocked);
                    }
                }
                return;
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
        public unsafe T[] Sort()
        {
            const int BUFFER_THRESHOLD = 256;
            if (_count <= BUFFER_THRESHOLD)
            {
                var ptr = stackalloc VertexID[_count];
                var buffer = new UnsafeSegment<VertexID>(ptr, _count);
                TopoSorting(buffer);
                return ConvertIdsToTsArray(buffer);
            }
            else
            {
                using (var memory = TempAllocator.Alloc<VertexID>(_count))
                {
                    var buffer = memory.AsSegment();
                    TopoSorting(buffer);
                    return ConvertIdsToTsArray(buffer);
                }
            }
        }
        private unsafe void TopoSorting(UnsafeSegment<VertexID> sortingBuffer)
        {
            var adjacency = new List<(VertexID To, int DependencyIndex)>[GetVertexInfosCount()];

            for (int i = 0; i < GetVertexInfosCount(); i++)
            {
                VertexID layerID = (VertexID)i;
                ref var info = ref GetVertexInfo(layerID);
                adjacency[(int)layerID] = new List<(VertexID To, int DependencyIndex)>();
                info.inDegree = 0;
                info.hasAnyDependency = false;
                info.isBasicAutoAttached = false;
            }

            for (int i = 0; i < _dependencies.Count; i++)
            {
                var (from, to) = _dependencies[i];
                ref var fromInfo = ref GetVertexInfo(from);
                ref var toInfo = ref GetVertexInfo(to);

                // Dependency endpoints participate in sorting even when they are virtual
                // (not contained). Virtual vertices are filtered only from the result.
                fromInfo.hasAnyDependency = true;
                toInfo.hasAnyDependency = true;
                adjacency[(int)from].Add((to, i));
                toInfo.inDegree += 1;
            }

            // Add implicit Basic dependencies only for completely isolated contained
            // vertices. Mark them so they are scheduled before explicit Basic successors.
            if (_basicVertexID != VertexID.NULL)
            {
                var basicLayerAdjacencyList = adjacency[(int)_basicVertexID];
                for (int i = 0; i < GetVertexInfosCount(); i++)
                {
                    var toID = (VertexID)i;
                    ref var toInfo = ref GetVertexInfo(i);
                    if (toID != _basicVertexID && toInfo.isContained && toInfo.hasAnyDependency == false)
                    {
                        basicLayerAdjacencyList.Add((toID, -1));
                        GetVertexInfo(_basicVertexID).hasAnyDependency = true;
                        toInfo.inDegree += 1;
                        toInfo.isBasicAutoAttached = true;
                    }
                }
            }

            // Along with contained vertices, sort every virtual vertex referenced by an edge.
            int sortingNodesCount = 0;
            for (int i = 0; i < GetVertexInfosCount(); i++)
            {
                ref var info = ref GetVertexInfo(i);
                if (info.isContained || info.hasAnyDependency)
                {
                    sortingNodesCount++;
                }
            }

            using (var nodesMemory = TempAllocator.Alloc<VertexID>(sortingNodesCount))
            {
                var nodes = nodesMemory.AsSegment();
                for (int i = 0, j = 0; i < GetVertexInfosCount(); i++)
                {
                    ref var info = ref GetVertexInfo(i);
                    if (info.isContained || info.hasAnyDependency)
                    {
                        nodes.Ptr[j++] = (VertexID)i;
                    }
                }

                List<VertexID> zeroInDegree = new List<VertexID>(nodes.Length);
                for (int i = 0; i < nodes.Length; i++)
                {
                    VertexID id = nodes.Ptr[i];
                    if (GetVertexInfo(id).inDegree == 0)
                    {
                        zeroInDegree.Add(id);
                    }
                }

                int processedCount = 0;
                int resultCount = 0;

                while (zeroInDegree.Count > 0)
                {
                    int currentIndex = SelectNextVertex(zeroInDegree);
                    var current = zeroInDegree[currentIndex];
                    zeroInDegree.RemoveAt(currentIndex);

                    processedCount++;
                    if (GetVertexInfo(current).isContained)
                    {
                        sortingBuffer.Ptr[resultCount++] = current;
                    }

                    var adjacencyList = adjacency[(int)current];
                    for (int i = 0; i < adjacencyList.Count; i++)
                    {
                        var (neighbor, _) = adjacencyList[i];
                        ref var neighborInfo = ref GetVertexInfo(neighbor);
                        neighborInfo.inDegree--;
                        if (neighborInfo.inDegree == 0)
                        {
                            zeroInDegree.Add(neighbor);
                        }
                    }
                }

                if (processedCount != nodes.Length)
                {
                    var cycle = FindCycle(adjacency, nodes);
                    string[] cycleDependencies = null;
                    if (cycle != null)
                    {
                        cycleDependencies = GetCycleDependencies(cycle, adjacency);
                    }
                    Throw.DependencyGraph_CyclicDependencyDetected(cycleDependencies);
                }
            }
        }

        private int SelectNextVertex(List<VertexID> candidates)
        {
            int bestIndex = 0;
            for (int i = 1; i < candidates.Count; i++)
            {
                if (CompareSchedulingPriority(candidates[i], candidates[bestIndex]) < 0)
                {
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private int CompareSchedulingPriority(VertexID leftID, VertexID rightID)
        {
            ref var left = ref GetVertexInfo(leftID);
            ref var right = ref GetVertexInfo(rightID);

            var leftCategory = GetSchedulingCategory(ref left);
            var rightCategory = GetSchedulingCategory(ref right);
            int result = ((int)leftCategory).CompareTo((int)rightCategory);
            if (result != 0)
            {
                return result;
            }

            if (leftCategory == SchedulingCategory.After)
            {
                // For the same After target, the newest vertex is closest to the target.
                result = right.insertionIndex.CompareTo(left.insertionIndex);
                if (result != 0)
                {
                    return result;
                }
                return ((int)rightID).CompareTo((int)leftID);
            }

            // Normal, auto-attached and Before vertices retain insertion order. Delayed
            // Before vertices therefore put the newest member closest to their target.
            result = left.insertionIndex.CompareTo(right.insertionIndex);
            if (result != 0)
            {
                return result;
            }
            return ((int)leftID).CompareTo((int)rightID);
        }

        private static SchedulingCategory GetSchedulingCategory(ref VertexInfo info)
        {
            if (info.isBasicAutoAttached)
            {
                return SchedulingCategory.BasicAutoAttached;
            }
            if (info.moveDirection == MOVE_AFTER)
            {
                return SchedulingCategory.After;
            }
            if (info.moveDirection == MOVE_BEFORE)
            {
                return SchedulingCategory.Before;
            }
            return SchedulingCategory.Normal;
        }

        private enum SchedulingCategory : byte
        {
            BasicAutoAttached,
            After,
            Normal,
            Before,
        }
        private unsafe T[] ConvertIdsToTsArray(UnsafeSegment<VertexID> buffer)
        {
            T[] result = new T[buffer.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetVertexInfo(buffer.Ptr[i]).value;
            }
            return result;
        }
        #endregion

        #region FindCycles
        private unsafe List<VertexID> FindCycle(
            List<(VertexID To, int DependencyIndex)>[] adjacency,
            UnsafeSegment<VertexID> nodes)
        {
            var visited = new Dictionary<VertexID, bool>();
            var recursionStack = new Stack<VertexID>();

            for (int i = 0; i < nodes.Length; i++)
            {
                VertexID node = nodes.Ptr[i];
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
        public bool ContainsVertex(T vertex)
        {
            return _vertexIDs.TryGetValue(vertex, out VertexID id) && GetVertexInfo(id).isContained;
        }
        public Enumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<T>
        {
            private DependencyGraph<T> _graph;
            private int _index;
            public Enumerator(DependencyGraph<T> graph)
            {
                _graph = graph;
                _index = -1;
            }
            public T Current
            {
                get { return _graph.GetVertexInfo(_index).value; }
            }
            object IEnumerator.Current { get { return Current; } }
            public bool MoveNext()
            {
                while (++_index < _graph.GetVertexInfosCount())
                {
                    if (_graph.GetVertexInfo(_index).isContained)
                    {
                        return true;
                    }
                }
                return false;
            }
            public void Reset() { _index = -1; }
            public void Dispose() { }
        }
        #endregion

        #region VertexInfo
        [DebuggerDisplay("{value}")]
        private struct VertexInfo
        {
            public T value;
            public int insertionIndex;
            public bool isLocked;
            public bool isContained;
            public sbyte moveDirection;
            //build
            public bool hasAnyDependency;
            public bool isBasicAutoAttached;
            public int inDegree;
            public VertexInfo(T name) : this()
            {
                this.value = name;
            }
        }
        #endregion
    }
}

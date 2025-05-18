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
    public interface IDependencyGraph<T> : IReadOnlyCollection<T>
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



    public class DependencyGraph<T> : IDependencyGraph<T>
    {
        private readonly Dictionary<T, VertexID> _vertexIDs = new Dictionary<T, VertexID>(32);
        private StructList<VertexInfo> _vertexInfos = new StructList<VertexInfo>(32);

        private List<(VertexID from, VertexID to)> _dependencies = new List<(VertexID, VertexID)>(16);
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

            if (info.isContained == false && info.isLocked || info.isLocked == false)
            {
                info.insertionIndex = _increment++;
            }
            if (info.isContained == false)
            {
                _count++;
                info.isContained = true;
            }
        }
        public bool RemoveVertex(T vertex)
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
        public void MergeWith(IDependencyGraph<T> other)
        {
            if (other is DependencyGraph<T> graph)
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
        public unsafe T[] Sort()
        {
            const int BUFFER_THRESHOLD = 256;
            if (_count <= BUFFER_THRESHOLD)
            {
                var ptr = stackalloc VertexID[_count];
                var buffer = UnsafeArray<VertexID>.Manual(ptr, _count);
                TopoSorting(buffer);
                ReoderInsertionIndexes(buffer);
                TopoSorting(buffer);
                return ConvertIdsToTsArray(buffer);
            }
            else
            {
                var ptr = TempBuffer<VertexID, VertexID>.Get(_count);
                var buffer = UnsafeArray<VertexID>.Manual(ptr, _count);
                TopoSorting(buffer);
                ReoderInsertionIndexes(buffer);
                TopoSorting(buffer);
                return ConvertIdsToTsArray(buffer);
            }
        }
        private unsafe void TopoSorting(UnsafeArray<VertexID> sortingBuffer)
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
                    if (toInfo.isContained && toInfo.hasAnyDependency == false)
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
        private unsafe void ReoderInsertionIndexes(UnsafeArray<VertexID> sortingBuffer)
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
        private static unsafe void MoveElement<TValue>(ref UnsafeArray<TValue> array, int oldIndex, int newIndex) where TValue : unmanaged
        {
            if (oldIndex == newIndex) return;

            var ptr = array.ptr;
            TValue item = ptr[oldIndex];

            int elementSize = sizeof(TValue);
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
        private unsafe T[] ConvertIdsToTsArray(UnsafeArray<VertexID> buffer)
        {
            T[] result = new T[buffer.Length];
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
        public bool ContainsVertex(T vertex)
        {
            return GetVertexInfo(GetVertexID(vertex)).isContained;
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
                if (_index++ >= _graph.GetVertexInfosCount()) { return false; }
                ref var info = ref _graph.GetVertexInfo(_index);
                if (info.isContained == false)
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
            public T value;
            public int insertionIndex;
            public bool isLocked;
            public bool isContained;
            public bool moveToRight;
            //build
            public bool hasAnyDependency;
            public int inDegree;
            public int sortingIndex;
            public int leftBeforeIndex;
            public VertexInfo(T name) : this()
            {
                this.value = name;
            }
        }
        #endregion
    }
}
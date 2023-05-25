using System;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public class EntityLinkedList
    {
        private const int ENTER = 0;

        private Node[] _nodes;
        private int _count;
        private int _lastNodeIndex;

        #region Properties
        public int Count => _count;
        public int Capacity => _nodes.Length;
        public int Last => _lastNodeIndex;
        #endregion

        #region Constructors
        public EntityLinkedList(int capacity)
        {
            _nodes = new Node[capacity + 10];
            Clear();
        }
        #endregion

        public void Resize(int newCapacity)
        {
            Array.Resize(ref _nodes, newCapacity + 10);
        }

        public void Clear()
        {
            //ArrayUtility.Fill(_nodes, Node.Empty);
            for (int i = 0; i < _nodes.Length; i++)
                _nodes[i].next = 0;
            _lastNodeIndex = ENTER;
            _count = 0;
        }

        public void Set(int nodeIndex, int entityID) => _nodes[nodeIndex].entityID = entityID;
        public int Get(int nodeIndex) => _nodes[nodeIndex].entityID;

        /// <summary> Insert after</summary>
        /// <returns> new node index</returns>
        public int Insert(int nodeIndex, int entityID)
        {
            _nodes[++_count].Set(entityID, _nodes[nodeIndex].next);
            _nodes[nodeIndex].next = _count;
            _lastNodeIndex = _count;
            return _count;
        }

        public int Add(int entityID) => Insert(_lastNodeIndex, entityID);

        public Enumerator GetEnumerator() => new Enumerator(_nodes);
        public EnumerableSpan Span(int startNodeIndex, int count) => new EnumerableSpan(this, startNodeIndex, count);

        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { entityID = 0, next = -1 };
            public int entityID;
            /// <summary>next node index</summary>
            public int next;

            public void Set(int entityID, int next)
            {
                this.entityID = entityID;
                this.next = next;
            }
        }

        public struct Enumerator
        {
            private readonly Node[] _nodes;
            private int _index;
            private int _next;
            public Enumerator(Node[] nodes)
            {
                _nodes = nodes;
                _index = -1;
                _next = ENTER;
            }
            public int Current => _nodes[_index].entityID;
            public bool MoveNext()
            {
                _index = _next;
                _next = _nodes[_next].next;
                return _index > 0;
            }
        }

        public readonly ref struct EnumerableSpan
        {
            private readonly EntityLinkedList _source;
            private readonly int _startNodeIndex;
            private readonly int _count;
            public EnumerableSpan(EntityLinkedList source, int startNodeIndex, int count)
            {
                _source = source;
                _startNodeIndex = startNodeIndex;
                _count = count;
            }
            public SpanEnumerator GetEnumerator() => new SpanEnumerator(_source._nodes, _startNodeIndex, _count);
        }
        public struct SpanEnumerator
        {
            private readonly Node[] _nodes;
            private int _index;
            private int _count;
            private int _next;
            public SpanEnumerator(Node[] nodes, int startIndex, int count)
            {
                _nodes = nodes;
                _index = -1;
                _count = count;
                _next = startIndex;
            }
            public int Current => _nodes[_index].entityID;
            public bool MoveNext()
            {
                _index = _next;
                _next = _nodes[_next].next;
                return _index > 0 && _count-- > 0;
            }
        }
        #endregion
    }
}

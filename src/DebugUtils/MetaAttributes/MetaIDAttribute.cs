#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaIDAttribute : EcsMetaAttribute
    {
        public readonly string ID;
        public MetaIDAttribute(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                EcsDebug.PrintError("The identifier cannot be empty or null");
                id = string.Empty;
            }
            if (MetaID.IsValidID(id) == false)
            {
                EcsDebug.PrintError($"Identifier {id} contains invalid characters. Allowed charset: {MetaID.ALLOWED_CHARSET}");
                id = string.Empty;
            }
            ID = id;
        }
    }

    public static unsafe class MetaID
    {
        public const string ALLOWED_CHARSET = "_0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        //public static bool IsFixedNameType(Type type)
        //{
        //    if (type.IsPrimitive)
        //    {
        //        return true;
        //    }
        //    if(type == typeof(string))
        //    {
        //        return true;
        //    }
        //    return false;
        //}
        public static bool IsGenericID(string id)
        {
            return id[id.Length - 1] == '>' || Regex.IsMatch(id, @"^[^,<>\s]*$");
        }
        public static bool IsValidID(string id)
        {
            return Regex.IsMatch(id, @"^[a-zA-Z0-9_]+$");
        }


        [ThreadStatic]
        private static uint _randonState;
        public static string GenerateNewUniqueID()
        {
            const int BYTES = 16;
            const int CHARS = BYTES * 2;
            const string CHARSET = "0123456789ABCDEF";

            if (_randonState == 0)
            {
                IntPtr prt = Marshal.AllocHGlobal(1);
                long alloc = (long)prt;
                Marshal.FreeHGlobal(prt);
                _randonState = (uint)alloc ^ (uint)DateTime.Now.Millisecond;
            }

            byte* bytes = stackalloc byte[BYTES];
            Span<byte> x = new Span<byte>(bytes, BYTES);
            long* bytesLong = (long*)bytes;
            uint* bytesUInt = (uint*)bytes;
            bytesLong[0] = DateTime.Now.Ticks;
            _randonState = BitsUtility.NextXorShiftState(_randonState);
            bytesUInt[2] = _randonState;
            _randonState = BitsUtility.NextXorShiftState(_randonState);
            bytesUInt[3] = _randonState;


            char* str = stackalloc char[CHARS];
            for (int i = 0, j = 0; i < BYTES; i++)
            {
                byte b = bytes[i];
                str[j++] = CHARSET[b & 0x0000_000F];
                str[j++] = CHARSET[(b >> 4) & 0x0000_000F];
            }

            return new string(str, 0, CHARS);
        }
        public static string IDToAttribute(string id)
        {
            return $"[MetaID(\"{id}\")]";
        }
        public static string ConvertIDToTypeName(string id)
        {
            id = id.Replace("_1", "__");
            id = id.Replace("_2", "__");
            id = id.Replace("_3", "__");

            id = id.Replace("<", "_1");
            id = id.Replace(">", "_2");
            id = id.Replace(",", "_3");
            return "_" + id;
        }
        public static string ParseIDFromTypeName(string name)
        {
            char* buffer = TempBuffer<char>.Get(name.Length);
            int count = 0;
            //skip name[0] char
            for (int i = 1, iMax = name.Length; i < iMax; i++)
            {
                char current = name[i];
                if (current == '_')
                {
                    if (++i >= iMax) { break; }
                    current = name[i];
                    switch (current)
                    {
                        case '1': current = '<'; break;
                        case '2': current = '>'; break;
                        case '3': current = ','; break;
                    }
                }
                buffer[count++] = current;
            }
            return new string(buffer, 0, count);
        }

        public static string GenerateNewUniqueIDWithAttribute()
        {
            return IDToAttribute(GenerateNewUniqueID());
        }


        public static bool TryFindMetaIDCollisions(IEnumerable<TypeMeta> metas, out CollisionList collisions)
        {
            collisions = new CollisionList(metas);
            return collisions.IsHasAnyCollision;
        }
        public static CollisionList FindMetaIDCollisions(IEnumerable<TypeMeta> metas)
        {
            return new CollisionList(metas);
        }

        #region CollisionList
        [DebuggerTypeProxy(typeof(DebuggerProxy))]
        [DebuggerDisplay("HasAnyCollision: {IsHasAnyCollision} ListsCount: {Count}")]
        public class CollisionList : IEnumerable<CollisionList.Collision>
        {
            private LinkedList[] _linkedLists;
            private Entry[] _entries;
            private int _collisionsCount;
            private int _listsCount;
            private HashSet<string> _collidingIDs;

            public int CollisionsCount
            {
                get { return _collisionsCount; }
            }
            public int Count
            {
                get { return _listsCount; }
            }
            public bool IsHasAnyCollision
            {
                get { return _listsCount > 0; }
            }
            public Collision this[int index]
            {
                get
                {
                    var list = _linkedLists[index];
                    return new Collision(this, list.headNode, list.count);
                }
            }

            public bool IsCollidingID(string id)
            {
                if(_collidingIDs== null)
                {
                    return false;
                }
                return _collidingIDs.Contains(id);
            }

            public CollisionList(IEnumerable<TypeMeta> metas)
            {
                var metasCount = metas.Count();
                Dictionary<string, int> listIndexes = new Dictionary<string, int>(metasCount);
                _linkedLists = new LinkedList[metasCount];
                _entries = new Entry[metasCount];

                bool hasCollision = false;

                _listsCount = 0;
                foreach (var meta in metas)
                {
                    if (meta.IsHasMetaID() == false) { continue; }
                    if (listIndexes.TryGetValue(meta.MetaID, out int headIndex))
                    {
                        hasCollision = true;
                    }
                    else
                    {
                        headIndex = _listsCount++;
                        listIndexes.Add(meta.MetaID, headIndex);
                    }
                    int nodeIndex = _collisionsCount++;

                    ref var list = ref _linkedLists[headIndex];
                    ref Entry entry = ref _entries[nodeIndex];
                    if (list.count == 0)
                    {
                        entry.next = -1;
                    }
                    else
                    {
                        entry.next = list.headNode;
                    }
                    entry.meta = meta;
                    list.headNode = nodeIndex;
                    listIndexes[meta.MetaID] = headIndex;
                    list.count++;
                }

                if (hasCollision)
                {
                    _collidingIDs = new HashSet<string>();
                    for (int i = 0; i < _listsCount; i++)
                    {
                        ref var list = ref _linkedLists[i];
                        if (list.count <= 1)
                        {
                            _linkedLists[i--] = _linkedLists[--_listsCount];
                        }
                    }
                    for (int i = 0; i < _listsCount; i++)
                    {
                        _collidingIDs.Add(this[i].MetaID);
                    }
                }
                else
                {
                    _listsCount = 0;
                }
            }

            [DebuggerDisplay("Count: {count}")]
            private struct LinkedList
            {
                public int count;
                public int headNode;
            }
            [DebuggerDisplay("ID: {meta.MetaID} next: {next}")]
            public struct Entry
            {
                public TypeMeta meta;
                public int next;
            }

            #region Enumerator
            public Enumerator GetEnumerator() { return new Enumerator(this, _listsCount); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            IEnumerator<Collision> IEnumerable<Collision>.GetEnumerator() { return GetEnumerator(); }
            public struct Enumerator : IEnumerator<Collision>
            {
                private readonly CollisionList _collisions;
                private readonly int _count;
                private int _index;
                public Collision Current
                {
                    get
                    {
                        var list = _collisions._linkedLists[_index];
                        return new Collision(_collisions, list.headNode, list.count);
                    }
                }
                object IEnumerator.Current { get { return Current; } }
                public Enumerator(CollisionList collisions, int count)
                {
                    _collisions = collisions;
                    _count = count;
                    _index = -1;
                }
                public bool MoveNext() { return ++_index < _count; }
                public void Dispose() { }
                public void Reset() { _index = -1; }
            }
            #endregion

            [DebuggerDisplay("Count: {Count}")]
            [DebuggerTypeProxy(typeof(DebuggerProxy))]
            public readonly struct Collision : IEnumerable<TypeMeta>
            {
                private readonly CollisionList _collisions;
                private readonly string _metaID;
                private readonly int _head;
                private readonly int _count;
                public int Count
                {
                    get { return _count; }
                }
                public string MetaID
                {
                    get { return _metaID; }
                }
                internal Collision(CollisionList collisions, int head, int count)
                {
                    _collisions = collisions;
                    if (count == 0)
                    {
                        _head = 0;
                        _metaID = string.Empty;
                    }
                    else
                    {
                        _head = head;
                        _metaID = collisions._entries[_head].meta.MetaID;
                    }
                    _head = head;
                    _count = count;
                }

                #region Enumerator
                public Enumerator GetEnumerator() { return new Enumerator(_collisions._entries, _head); }
                IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
                IEnumerator<TypeMeta> IEnumerable<TypeMeta>.GetEnumerator() { return GetEnumerator(); }
                public struct Enumerator : IEnumerator<TypeMeta>
                {
                    private readonly Entry[] _linkedEntries;
                    private readonly int _head;
                    private int _nextIndex;
                    private int _index;
                    public TypeMeta Current { get { return _linkedEntries[_index].meta; } }
                    object IEnumerator.Current { get { return Current; } }
                    public Enumerator(Entry[] linkedEntries, int head)
                    {
                        _linkedEntries = linkedEntries;
                        _head = head;
                        _nextIndex = _head;
                        _index = -1;
                    }
                    public bool MoveNext()
                    {
                        if (_nextIndex < 0) { return false; }
                        _index = _nextIndex;
                        _nextIndex = _linkedEntries[_index].next;
                        return true;
                    }
                    public void Dispose() { }
                    public void Reset()
                    {
                        _nextIndex = _head;
                        _index = -1;
                    }
                }
                #endregion

                #region DebuggerProxy
                private class DebuggerProxy
                {
                    public Type[] Types;
                    public DebuggerProxy(Collision collision)
                    {
                        Types = collision.Select(o => o.Type).ToArray();
                    }
                }
                #endregion
            }
            #region DebuggerProxy
            private class DebuggerProxy
            {
                public string[][] Lists;
                public DebuggerProxy(CollisionList collisions)
                {
                    Lists = new string[collisions.Count][];
                    int i = 0;
                    foreach (var list in collisions)
                    {
                        int j = 0;
                        Lists[i] = new string[list.Count];
                        foreach (var typeMeta in list)
                        {
                            Lists[i][j] = typeMeta.MetaID;
                            j++;
                        }
                        i++;
                    }
                }
            }
            #endregion
        }
        #endregion
    }
}
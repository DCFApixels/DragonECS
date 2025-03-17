﻿#if DISABLE_DEBUG
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
                Throw.ArgumentNull(nameof(id));
            }
            if (MetaID.IsGenericID(id) == false)
            {
                Throw.ArgumentException($"Identifier {id} contains invalid characters: ,<>");
            }
            id = string.Intern(id);
            ID = id;
        }
    }

    public static class MetaID
    {
        [ThreadStatic]
        private static Random _randon;
        [ThreadStatic]
        private static byte[] _buffer;
        [ThreadStatic]
        private static bool _isInit;


        public static bool TryFindMetaIDCollisions(IEnumerable<TypeMeta> metas, out CollisionList collisions)
        {
            collisions = new CollisionList(metas);
            return collisions.IsHasAnyCollision;
        }
        public static CollisionList FindMetaIDCollisions(IEnumerable<TypeMeta> metas)
        {
            return new CollisionList(metas);
        }

        public static bool IsGenericID(string id)
        {
            return Regex.IsMatch(id, @"^[^,<>\s]*$");
        }

        public static unsafe string GenerateNewUniqueID()
        {
            if (_isInit == false)
            {
                IntPtr prt = Marshal.AllocHGlobal(1);
                long alloc = (long)prt;
                Marshal.Release(prt);
                _randon = new Random((int)alloc);
                _buffer = new byte[8];
                _isInit = true;
            }

            byte* hibits = stackalloc byte[8];
            long* hibitsL = (long*)hibits;
            hibitsL[0] = DateTime.Now.Ticks;
            hibitsL[1] = _randon.Next();

            for (int i = 0; i < 8; i++)
            {
                _buffer[i] = hibits[i];
            }

            return BitConverter.ToString(_buffer).Replace("-", "");
        }
        public static string IDToAttribute(string id)
        {
            return $"[MetaID(\"id\")]";
        }
        public static string GenerateNewUniqueIDWithAttribute()
        {
            return IDToAttribute(GenerateNewUniqueID());
        }

        #region CollisionList
        [DebuggerTypeProxy(typeof(DebuggerProxy))]
        [DebuggerDisplay("HasAnyCollision: {IsHasAnyCollision} ListsCount: {ListsCount}")]
        public class CollisionList : IEnumerable<CollisionList.Collision>
        {
            private LinkedList[] _linkedLists;
            private Entry[] _entries;
            private int _collisionsCount;
            private int _listsCount;
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
                    for (int i = 0; i < _listsCount; i++)
                    {
                        ref var list = ref _linkedLists[i];
                        if (list.count <= 1)
                        {
                            _linkedLists[i--] = _linkedLists[--_listsCount];
                        }
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
                    if(count == 0)
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
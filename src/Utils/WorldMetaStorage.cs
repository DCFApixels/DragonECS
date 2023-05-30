using DCFApixels.DragonECS.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    internal static class WorldMetaStorage
    {
        private static int _tokenCount = 0;
        private static List<ResizerBase> _resizers = new List<ResizerBase>();
        private static WorldTypeMeta[] _metas = new WorldTypeMeta[0];
        private static Dictionary<Type, int> _worldIds = new Dictionary<Type, int>();
        private static class WorldIndex<TWorldArchetype>
        {
            public static int id = GetWorldID(typeof(TWorldArchetype));
        }
        private static int GetToken()
        {
            WorldTypeMeta meta = new WorldTypeMeta();
            meta.id = _tokenCount;
            Array.Resize(ref _metas, ++_tokenCount);
            _metas[_tokenCount - 1] = meta;

            foreach (var item in _resizers)
                item.Resize(_tokenCount);
            return _tokenCount - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldID(Type archetype)
        {
            if (!_worldIds.TryGetValue(archetype, out int id))
            {
                id = GetToken();
                _worldIds.Add(archetype, id);
            }
            return id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldId<TWorldArchetype>() => WorldIndex<TWorldArchetype>.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetComponentId<T>(int worldID) => Component<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSubjectId<T>(int worldID) => Subject<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetExecutorId<T>(int worldID) => Executor<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldComponentId<T>(int worldID) => WorldComponent<T>.Get(worldID);
        public static bool IsComponentTypeDeclared(int worldID, Type type) => _metas[worldID].IsDeclaredType(type);
        public static Type GetComponentType(int worldID, int componentID) => _metas[worldID].GetComponentType(componentID);

        private abstract class ResizerBase
        {
            public abstract void Resize(int size);
        }
        private static class Component<T>
        {
            public static int[] ids;
            static Component()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizers.Add(new Resizer());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                {
                    var meta = _metas[token];
                    id = (ushort)meta.componentCount++;
                    meta.AddType(id, typeof(T));
                }
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override void Resize(int size)
                {
                    int oldSize = ids.Length;
                    Array.Resize(ref ids, size);
                    ArrayUtility.Fill(ids, -1, oldSize, size);
                }
            }
        }
        private static class Subject<T>
        {
            public static int[] ids;
            static Subject()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizers.Add(new Resizer());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = _metas[token].subjectsCount++;
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override void Resize(int size)
                {
                    int oldSize = ids.Length;
                    Array.Resize(ref ids, size);
                    ArrayUtility.Fill(ids, -1, oldSize, size);
                }
            }
        }
        private static class Executor<T>
        {
            public static int[] ids;
            static Executor()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizers.Add(new Resizer());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = _metas[token].executorsCount++;
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override void Resize(int size)
                {
                    int oldSize = ids.Length;
                    Array.Resize(ref ids, size);
                    ArrayUtility.Fill(ids, -1, oldSize, size);
                }
            }
        }
        private static class WorldComponent<T>
        {
            public static int[] ids;
            static WorldComponent()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizers.Add(new Resizer());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = _metas[token].worldComponentCount++;
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override void Resize(int size)
                {
                    int oldSize = ids.Length;
                    Array.Resize(ref ids, size);
                    ArrayUtility.Fill(ids, -1, oldSize, size);
                }
            }
        }
        private class WorldTypeMeta
        {
            public int id;
            public int componentCount;
            public int subjectsCount;
            public int executorsCount;
            public int worldComponentCount;
            private Type[] _types;
            private HashSet<Type> _declaredComponentTypes;
            public void AddType(int id, Type type)
            {
                if (_types.Length <= id)
                    Array.Resize(ref _types, id + 10);
                _types[id] = type;

                _declaredComponentTypes.Add(type);
            }
            public Type GetComponentType(int componentID) => _types[componentID];
            public bool IsDeclaredType(Type type) => _declaredComponentTypes.Contains(type);
            public WorldTypeMeta()
            {
                _types = new Type[10];
                _declaredComponentTypes = new HashSet<Type>();
            }
        }
    }
}

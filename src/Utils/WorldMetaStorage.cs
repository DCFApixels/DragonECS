using DCFApixels.DragonECS.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //TODO этот класс требует переработки, изначально такая конструкция имела хорошую производительность, но сейчас он слишком раздулся
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
        private static int GetToken(Type worldType)
        {
            WorldTypeMeta meta = new WorldTypeMeta(worldType);
            meta.id = _tokenCount;
            Array.Resize(ref _metas, ++_tokenCount);
            _metas[_tokenCount - 1] = meta;

            foreach (var item in _resizers)
                item.Resize(_tokenCount);
            return _tokenCount - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldID(Type worldType)
        {
            if (!_worldIds.TryGetValue(worldType, out int id))
            {
                id = GetToken(worldType);
                _worldIds.Add(worldType, id);
            }
            return id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetWorldType(int worldTypeID) => _metas[worldTypeID].worldType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldID<TWorldArchetype>() => WorldIndex<TWorldArchetype>.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetComponentID<T>(int worldID) => Component<T>.Get(worldID);
        public static int GetComponentID(Type type, int worldID) => _metas[worldID].GetComponentID(type);
        public static bool IsComponentTypeDeclared(int worldID, Type type) => _metas[worldID].IsDeclaredComponentType(type);
        public static Type GetComponentType(int worldID, int componentID) => _metas[worldID].GetComponentType(componentID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPoolID<T>(int worldID) => Pool<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetAspectID<T>(int worldID) => Aspect<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetExecutorID<T>(int worldID) => Executor<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldComponentID<T>(int worldID) => WorldComponent<T>.Get(worldID);
        public static int GetWorldComponentID(Type type, int worldID) => _metas[worldID].GetWorldComponentID(type);


        private abstract class ResizerBase
        {
            public abstract Type Type { get; }
            public abstract int[] IDS { get; }
            public abstract void Resize(int size);
        }

        #region Containers
        public static class PoolComponentIdArrays
        {
            private static Dictionary<Type, int[]> _componentTypeArrayPairs = new Dictionary<Type, int[]>();

            public static int[] GetIdsArray(Type type)
            {
                int targetSize = _tokenCount;
                if (!_componentTypeArrayPairs.TryGetValue(type, out int[] result))
                {
                    result = new int[targetSize];
                    for (int i = 0; i < result.Length; i++)
                        result[i] = -1;
                    _componentTypeArrayPairs.Add(type, result);
                }
                else
                {
                    if (result.Length < targetSize)
                    {
                        int oldSize = result.Length;
                        Array.Resize(ref result, targetSize);
                        ArrayUtility.Fill(result, -1, oldSize, targetSize);
                        _componentTypeArrayPairs[type] = result;
                    }
                }

                return result;
            }

            public static int GetComponentID(Type type, int token)
            {
                GetIdsArray(type);
                ref int id = ref _componentTypeArrayPairs[type][token];
                if (id < 0)
                    id = _metas[token].DeclareComponentType(type);
                return id;
            }
        }
        private static class Pool<T>
        {
            public static int[] ids;
            private static Type componentType = typeof(T).GetGenericArguments()[0];
            static Pool()
            {
                ids = PoolComponentIdArrays.GetIdsArray(componentType);
                _resizers.Add(new Resizer());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                {
                    id = PoolComponentIdArrays.GetComponentID(componentType, token);
                }
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override Type Type => typeof(T);
                public override int[] IDS => ids;
                public override void Resize(int size)
                {
                    ids = PoolComponentIdArrays.GetIdsArray(componentType);
                }
            }
        }
        private static class Component<T>
        {
            public static int[] ids;
            static Component()
            {
                ids = PoolComponentIdArrays.GetIdsArray(typeof(T));
                _resizers.Add(new Resizer());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                {
                    id = PoolComponentIdArrays.GetComponentID(typeof(T), token);
                }
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override Type Type => typeof(T);
                public override int[] IDS => ids;
                public override void Resize(int size)
                {
                    ids = PoolComponentIdArrays.GetIdsArray(typeof(T));
                }
            }
        }
        private static class Aspect<T>
        {
            public static int[] ids;
            static Aspect()
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
                    id = _metas[token].aspectsCount++;
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override Type Type => typeof(T);
                public override int[] IDS => ids;
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
                public override Type Type => typeof(T);
                public override int[] IDS => ids;
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
                    id = _metas[token].GetWorldComponentID(typeof(T));
                return id;
            }
            private sealed class Resizer : ResizerBase
            {
                public override Type Type => typeof(T);
                public override int[] IDS => ids;
                public override void Resize(int size)
                {
                    int oldSize = ids.Length;
                    Array.Resize(ref ids, size);
                    ArrayUtility.Fill(ids, -1, oldSize, size);
                }
            }
        }
        #endregion
        private class WorldTypeMeta
        {
            public readonly Type worldType;
            public int id;
            public int componentCount;
            public int aspectsCount;
            public int executorsCount;
            public int worldComponentCount;
            private Type[] _types = new Type[10];
            private Dictionary<Type, int> _declaredComponentTypes = new Dictionary<Type, int>();
            private Dictionary<Type, int> _declaredWorldComponentTypes = new Dictionary<Type, int>();

            public WorldTypeMeta(Type worldType)
            {
                this.worldType = worldType;
            }

            public int DeclareComponentType(Type type)
            {
                int id = componentCount++;
                if (_types.Length <= id)
                    Array.Resize(ref _types, id + 10);
                _types[id] = type;
                _declaredComponentTypes.Add(type, id);
                return id;
            }
            public bool IsDeclaredComponentType(Type type) => _declaredComponentTypes.ContainsKey(type);
            public Type GetComponentType(int componentID) => _types[componentID];
            public int GetComponentID(Type type) => PoolComponentIdArrays.GetComponentID(type, id);


            public int DeclareWorldComponentType(Type type)
            {
                int id = worldComponentCount++;
                _declaredWorldComponentTypes.Add(type, id);
                return id;
            }
            public bool IsDeclaredWorldComponentType(Type type) => _declaredWorldComponentTypes.ContainsKey(type);
            public int GetWorldComponentID(Type type)
            {
                if (!_declaredWorldComponentTypes.TryGetValue(type, out int id))
                    id = DeclareWorldComponentType(type);
                return id;
            }
        }
    }
}

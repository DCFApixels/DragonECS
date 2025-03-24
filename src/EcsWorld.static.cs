#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        #region Consts
        private const short NULL_WORLD_ID = 0;

        private const short GEN_STATUS_SEPARATOR = 0;
        private const short GEN_WAKEUP_MASK = 0x7fff;
        private const short GEN_SLEEP_MASK = ~GEN_WAKEUP_MASK;

        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 5;
        private const int DEL_ENT_BUFFER_MIN_SIZE = 64;
        #endregion

        private static EcsWorld[] _worlds = Array.Empty<EcsWorld>();
        private static readonly IdDispenser _worldIdDispenser = new IdDispenser(4, 0, n => Array.Resize(ref _worlds, n));
        private static StructList<WorldComponentPoolAbstract> _allWorldComponentPools = new StructList<WorldComponentPoolAbstract>(64);
        private static readonly object _worldLock = new object();

        private StructList<WorldComponentPoolAbstract> _worldComponentPools;
        private int _builtinWorldComponentsCount = 0;

        static EcsWorld()
        {
            _worlds[NULL_WORLD_ID] = new NullWorld();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsWorld GetWorld(short worldID)
        {// ts
            return _worlds[worldID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetWorld(short worldID, out EcsWorld world)
        {// ts
            world = _worlds[worldID];
            return
                world != null &&
                world.IsDestroyed != false &&
                worldID != 0;
        }

        private void ReleaseData(short worldID)
        {// ts
            lock (_worldLock)
            {
                foreach (var controller in _worldComponentPools)
                {
                    controller.Release(worldID);
                }
                _worldComponentPools.Clear();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetData<T>(short worldID)
        {
            return ref WorldComponentPool<T>.GetForWorld(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasData<T>(short worldID)
        {
            return WorldComponentPool<T>.Has(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetDataUnchecked<T>(short worldID)
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(worldID);
        }

        public static void ResetStaticState()
        {
            var nullworld = _worlds[0];
            for (int i = 1; i < _worlds.Length; i++)
            {
                var world = _worlds[i];
                if (world == null) { continue; }

                if (world.IsDestroyed == false)
                {
                    world.Destroy();
                }
                world = null;
            }
            _worlds = new EcsWorld[_worldIdDispenser.Capacity];
            _worlds[0] = nullworld;
            _worldIdDispenser.ReleaseAll();
        }

        #region WorldComponentPool
        public ReadOnlySpan<WorldComponentPoolAbstract> GetWorldComponents()
        {
            return new ReadOnlySpan<WorldComponentPoolAbstract>(
                _worldComponentPools._items,
                _builtinWorldComponentsCount,
                _worldComponentPools._count - _builtinWorldComponentsCount);
            //return new ReadOnlySpan<WorldComponentPoolAbstract>(_worldComponentPools._items, 0, _builtinWorldComponentsCount);
        }
        public ReadOnlySpan<WorldComponentPoolAbstract> GetAllWorldComponents()
        {
            return _worldComponentPools.ToReadOnlySpan();
        }
        public abstract class WorldComponentPoolAbstract
        {
            protected static readonly Type[] _builtinTypes = new Type[]
            {
                typeof(AspectCache<>),
                typeof(PoolCache<>),
                typeof(WhereQueryCache<,>),
                typeof(EcsMask.WorldMaskComponent),
            };
            internal readonly bool _isBuiltin;
            protected WorldComponentPoolAbstract()
            {
                Type type = ComponentType;
                if (type.IsGenericType) { type = type.GetGenericTypeDefinition(); }
                _isBuiltin = Array.IndexOf(_builtinTypes, type) >= 0;
            }
            public abstract Type ComponentType { get; }
            public abstract void Has(short worldID);
            public abstract void Release(short worldID);
            public abstract object GetRaw(short worldID);
            public abstract void SetRaw(short worldID, object raw);
        }
        private static class WorldComponentPool<T>
        {
            private static T[] _items = new T[4];
            private static short[] _mapping = new short[4];
            private static short _count;
            private static short[] _recycledItems = new short[4];
            private static short _recycledItemsCount;
            private static readonly IEcsWorldComponent<T> _interface = EcsWorldComponentHandler<T>.instance;
            private static readonly Abstract _controller = new Abstract();
            static WorldComponentPool()
            {
                _allWorldComponentPools.Add(_controller);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetItem(int itemIndex)
            {// ts
                return ref _items[itemIndex];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorld(short worldID)
            {// зависит от GetItemIndex
                return ref GetItem(GetItemIndex(worldID));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorldUnchecked(short worldID)
            {// ts
#if DEBUG
                if (_mapping[worldID] <= 0) { Throw.ArgumentOutOfRange(); }
#endif
                return ref _items[_mapping[worldID]];
            }
            public static int GetItemIndex(short worldID)
            {// ts
                if (_mapping.Length < _worlds.Length)
                {
                    lock (_worldLock)
                    {
                        if (_mapping.Length < _worlds.Length)
                        {
                            Array.Resize(ref _mapping, _worlds.Length);
                        }
                    }
                }
                short itemIndex = _mapping[worldID];

                if (itemIndex == 0)
                {
                    lock (_worldLock)
                    {
                        itemIndex = _mapping[worldID];
                        if (itemIndex <= 0)
                        {
                            if (_recycledItemsCount > 0)
                            {
                                _count++;
                                itemIndex = _recycledItems[--_recycledItemsCount];
                            }
                            else
                            {
                                itemIndex = ++_count;
                            }
                            _mapping[worldID] = itemIndex;

                            if (_items.Length <= itemIndex)
                            {
                                Array.Resize(ref _items, _items.Length << 1);
                            }

#if DEBUG
                            AllowedInWorldsAttribute.CheckAllows<T>(_worlds[worldID]);
#endif

                            _interface.Init(ref _items[itemIndex], _worlds[worldID]);

                            var world = GetWorld(worldID);
                            world._worldComponentPools.Add(_controller);
                            if (_controller._isBuiltin)
                            {
                                world._builtinWorldComponentsCount++;
                                world._worldComponentPools.SwapAt(
                                    world._worldComponentPools.Count - 1,
                                    world._builtinWorldComponentsCount - 1);
                            }
                        }
                    }
                }
                return itemIndex;
            }
            private static void Release(short worldID)
            {// ts
                lock (_worldLock)
                {
                    if (_mapping.Length < _worlds.Length)
                    {
                        Array.Resize(ref _mapping, _worlds.Length);
                    }
                    ref short itemIndex = ref _mapping[worldID];
                    if (itemIndex != 0)
                    {
                        _interface.OnDestroy(ref _items[itemIndex], _worlds[worldID]);
                        if (_recycledItemsCount >= _recycledItems.Length)
                        {
                            Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
                        }
                        _recycledItems[_recycledItemsCount++] = itemIndex;
                        itemIndex = 0;
                    }
                }
            }
            public static bool Has(short worldID)
            {// ts
                if (_mapping.Length < _worlds.Length)
                {
                    lock (_worldLock)
                    {
                        if (_mapping.Length < _worlds.Length)
                        {
                            Array.Resize(ref _mapping, _worlds.Length);
                        }
                    }
                }
                short itemIndex = _mapping[worldID];
                return itemIndex > 0;
            }
            private sealed class Abstract : WorldComponentPoolAbstract
            {
                public sealed override Type ComponentType
                {
                    get { return typeof(T); }
                }
                public override void SetRaw(short worldID, object raw)
                {
                    WorldComponentPool<T>.GetItem(worldID) = (T)raw;
                }
                public sealed override void Has(short worldID)
                {
                    WorldComponentPool<T>.Has(worldID);
                }
                public sealed override object GetRaw(short worldID)
                {
                    return WorldComponentPool<T>.GetItem(worldID);
                }
                public sealed override void Release(short worldID)
                {
                    WorldComponentPool<T>.Release(worldID);
                }
            }
        }
        #endregion

        #region NullWorld
        private sealed class NullWorld : EcsWorld
        {
            internal NullWorld() : base(new EcsWorldConfig(4, 4, 4, 4, 4), null, 0) { }
        }

        #endregion

        #region DebuggerProxy
        protected partial class DebuggerProxy
        {
            private short _worldID;
            public IEnumerable<object> WorldComponents
            {
                get
                {
                    _worldID = _world.ID;
                    return _world._worldComponentPools.ToEnumerable().Skip(_world._builtinWorldComponentsCount).Select(o => o.GetRaw(_worldID));
                }
            }
            public IEnumerable<object> AllWorldComponents
            {
                get
                {
                    _worldID = _world.ID;
                    return _world._worldComponentPools.ToEnumerable().Select(o => o.GetRaw(_worldID));
                }
            }
        }
        #endregion

        #region Obsolete
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use EcsWorld.ID")]
        public short id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ID; }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The GetPoolInstance(int componentTypeID) method will be removed in future updates, use FindPoolInstance(Type componentType)")]
        public IEcsPool GetPoolInstance(int componentTypeID)
        {
            return FindPoolInstance(componentTypeID);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The GetPoolInstance(Type componentType) method will be removed in future updates, use FindPoolInstance(Type componentType)")]
        public IEcsPool GetPoolInstance(Type componentType)
        {
            return FindPoolInstance(componentType);
        }
        #endregion
    }
}
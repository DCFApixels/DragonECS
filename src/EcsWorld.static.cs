using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private const short NULL_WORLD_ID = 0;

        private const short GEN_STATUS_SEPARATOR = 0;
        private const short GEN_WAKEUP_MASK = 0x7fff;
        private const short GEN_SLEEP_MASK = ~GEN_WAKEUP_MASK;

        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 5;
        private const int DEL_ENT_BUFFER_MIN_SIZE = 64;

        private static EcsWorld[] _worlds = Array.Empty<EcsWorld>();
        private static IdDispenser _worldIdDispenser = new IdDispenser(4, 0, n => Array.Resize(ref _worlds, n));

        private static List<DataReleaser> _dataReleaseres = new List<DataReleaser>();
        //public static int Copacity => Worlds.Length;

        private static readonly object _worldLock = new object();

        static EcsWorld()
        {
            _worlds[NULL_WORLD_ID] = new NullWorld();
        }
        private static void ReleaseData(int worldID)
        {// ts
            lock (_worldLock)
            {
                for (int i = 0, iMax = _dataReleaseres.Count; i < iMax; i++)
                {
                    _dataReleaseres[i].Release(worldID);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsWorld GetWorld(short worldID)
        {// ts
            return _worlds[worldID];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetData<T>(int worldID)
        {
            return ref WorldComponentPool<T>.GetForWorld(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetDataUnchecked<T>(int worldID)
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(worldID);
        }


        private abstract class DataReleaser
        {
            public abstract void Release(int worldID);
        }
        private static class WorldComponentPool<T>
        {
            private static T[] _items = new T[4];
            private static short[] _mapping = new short[4];
            private static short _count;
            private static short[] _recycledItems = new short[4];
            private static short _recycledItemsCount;
            private static IEcsWorldComponent<T> _interface = EcsWorldComponentHandler<T>.instance;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetItem(int itemIndex)
            {// ts
                return ref _items[itemIndex];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorld(int worldID)
            {// зависит от GetItemIndex
                return ref GetItem(GetItemIndex(worldID));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorldUnchecked(int worldID)
            {// ts
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_mapping[worldID] <= 0) { Throw.ArgumentOutOfRange(); }
#endif
                return ref _items[_mapping[worldID]];
            }
            public static int GetItemIndex(int worldID)
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

                            _interface.Init(ref _items[itemIndex], _worlds[worldID]);
                            _dataReleaseres.Add(new Releaser());
                        }
                    }
                }
                return itemIndex;
            }
            private static void Release(int worldID)
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
            private sealed class Releaser : DataReleaser
            {
                public sealed override void Release(int worldID)
                {
                    WorldComponentPool<T>.Release(worldID);
                }
            }
        }
        private sealed class NullWorld : EcsWorld
        {
            internal NullWorld() : base(new EcsWorldConfig(4, 4, 4, 4, 4), 0) { }
        }

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

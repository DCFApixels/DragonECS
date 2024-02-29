using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public struct EcsWorldCmp<T> where T : struct
    {
        private int _worldID;
        public EcsWorldCmp(int worldID) => _worldID = worldID;
        public EcsWorld World => EcsWorld.GetWorld(_worldID);
        public ref T Value => ref EcsWorld.GetData<T>(_worldID);
    }
    public partial class EcsWorld
    {
        private const short GEN_MASK = 0x7fff;
        private const short SLEEPING_GEN_FLAG = short.MinValue;
        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 5;
        private const int DEL_ENT_BUFFER_MIN_SIZE = 64;

        private static EcsWorld[] _worlds = Array.Empty<EcsWorld>();
        private static IdDispenser _worldIdDispenser = new IdDispenser(4, 0, OnWorldIdDispenser);

        private static List<DataReleaser> _dataReleaseres = new List<DataReleaser>();
        //public static int Copacity => Worlds.Length;

        static EcsWorld()
        {
            _worlds[0] = new NullWorld();
        }
        private static void ReleaseData(int worldID)
        {
            for (int i = 0, iMax = _dataReleaseres.Count; i < iMax; i++)
            {
                _dataReleaseres[i].Release(worldID);
            }
        }
        public static void OnWorldIdDispenser(int newSize)
        {
            Array.Resize(ref _worlds, newSize);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsWorld GetWorld(int worldID) => _worlds[worldID];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetData<T>(int worldID) => ref WorldComponentPool<T>.GetForWorld(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetDataUnchecked<T>(int worldID) => ref WorldComponentPool<T>.GetForWorldUnchecked(worldID);

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
            public static ref T Get(int itemIndex)
            {
                return ref _items[itemIndex];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorld(int worldID)
            {
                int index = GetItemIndex(worldID);
                return ref _items[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorldUnchecked(int worldID)
            {
#if (DEBUG && !DISABLE_DEBUG)
                if (_mapping[worldID] <= 0)
                    throw new Exception();
#endif
                return ref _items[_mapping[worldID]];
            }
            public static int GetItemIndex(int worldID)
            {
                if (_mapping.Length < _worlds.Length)
                {
                    Array.Resize(ref _mapping, _worlds.Length);
                }
                ref short itemIndex = ref _mapping[worldID];
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
                    if (_items.Length <= itemIndex)
                    {
                        Array.Resize(ref _items, _items.Length << 1);
                    }
                    _interface.Init(ref _items[itemIndex], _worlds[worldID]);
                    _dataReleaseres.Add(new Releaser());
                }
                return itemIndex;
            }
            private static void Release(int worldID)
            {
                ref short itemIndex = ref _mapping[worldID];
                if (itemIndex != 0)
                {
                    _interface.OnDestroy(ref _items[itemIndex], _worlds[worldID]);
                    _recycledItems[_recycledItemsCount++] = itemIndex;
                    itemIndex = 0;
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
            internal NullWorld() : base(EcsWorldConfig.Empty, 0) { }
        }
    }
}

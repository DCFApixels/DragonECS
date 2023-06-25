using DCFApixels.DragonECS.Utils;
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
    public abstract partial class EcsWorld
    {
        private const short GEN_BITS = 0x7fff;
        private const short DEATH_GEN_BIT = short.MinValue;
        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 2;

        private static EcsWorld[] Worlds = new EcsWorld[4];
        private static IntDispenser _worldIdDispenser = new IntDispenser(0);

        private static List<DataReleaser> _dataReleaseres = new List<DataReleaser>();

        static EcsWorld()
        {
            Worlds[0] = new EcsNullWorld();
        }
        private static void ReleaseData(int worldID)
        {
            for (int i = 0, iMax = _dataReleaseres.Count; i < iMax; i++)
                _dataReleaseres[i].Release(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsWorld GetWorld(int worldID) => Worlds[worldID];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetData<T>(int worldID) => ref WorldComponentPool<T>.GetForWorld(worldID);

        private abstract class DataReleaser
        {
            public abstract void Release(int worldID);
        }
        private static class WorldComponentPool<T>
        {
            private static T[] _items = new T[4];
            private static int[] _mapping = new int[4];
            private static int _count;
            private static int[] _recycledItems = new int[4];
            private static int _recycledItemsCount;
            private static IEcsWorldComponent<T> _interface = EcsWorldComponentHandler<T>.instance;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T Get(int itemIndex) => ref _items[itemIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref T GetForWorld(int worldID) => ref _items[GetItemIndex(worldID)];
            public static int GetItemIndex(int worldID)
            {
                if (_mapping.Length < Worlds.Length)
                    Array.Resize(ref _mapping, Worlds.Length);

                ref int itemIndex = ref _mapping[worldID];
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
                    _interface.Init(ref _items[itemIndex], Worlds[worldID]);
                    _dataReleaseres.Add(new Releaser());
                }
                return itemIndex;
            }
            private static void Release(int worldID)
            {
                ref int itemIndex = ref _mapping[worldID];
                if (itemIndex != 0)
                {
                    _interface.OnDestroy(ref _items[itemIndex], Worlds[worldID]);
                    _recycledItems[_recycledItemsCount++] = itemIndex;
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
    }
    internal sealed class EcsNullWorld : EcsWorld { }
}

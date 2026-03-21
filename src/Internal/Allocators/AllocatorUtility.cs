using System;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.Core.Internal.MemoryAllocator;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static unsafe class AllocatorUtility
    {
        public static void ClearAllocatedMemory(IntPtr ptr, int startByte, int lengthInBytes)
        {
            ClearAllocatedMemory((byte*)ptr, startByte, lengthInBytes);
        }
        public static void ClearAllocatedMemory(byte* ptr, int startByte, int lengthInBytes)
        {
            Span<byte> memorySpan = new Span<byte>(ptr + startByte, lengthInBytes);
            memorySpan.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CacheTo(this EcsMaskIterator it, EcsSpan source, ref HMem<int> array)
        {
            switch (it.MaskFlags)
            {
                case EcsMaskFlags.Empty:
                    {
                        if(array.Length < source.Count)
                        {
                            array = Realloc<int>(array, source.Count);
                        }
                        source.AsSystemSpan().CopyTo(array.AsSpan());
                        return source.Count;
                    }
                case EcsMaskFlags.Inc:
                    {
                        return it.IterateOnlyInc(source).CacheTo(ref array);
                    }
                case EcsMaskFlags.Exc:
                case EcsMaskFlags.Any:
                case EcsMaskFlags.IncExc:
                case EcsMaskFlags.IncAny:
                case EcsMaskFlags.ExcAny:
                case EcsMaskFlags.IncExcAny:
                    {
                        return it.Iterate(source).CacheTo(ref array);
                    }
                case EcsMaskFlags.Broken:
                    {
                        return 0;
                    }
                default:
                    {
                        Throw.UndefinedException();
                        return 0;
                    }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CacheTo(this EcsMaskIterator.OnlyIncEnumerable e, ref HMem<int> array)
        {
            int count = 0;
            var enumerator = e.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (array.Length <= count)
                {
                    array = Realloc<int>(array, array.Length << 1);
                }
                array.Ptr[count++] = enumerator.Current;
            }
            return count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CacheTo(this EcsMaskIterator.Enumerable e, ref HMem<int> array)
        {
            int count = 0;
            var enumerator = e.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (array.Length <= count)
                {
                    array = Realloc<int>(array, array.Length << 1);
                }
                array.Ptr[count++] = enumerator.Current;
            }
            return count;
        }
    }
}

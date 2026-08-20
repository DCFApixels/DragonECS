using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    internal static unsafe class DragonUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearMemory(IntPtr ptr, int startByte, int lengthInBytes)
        {
            ClearMemory((byte*)ptr, startByte, lengthInBytes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearMemory(byte* ptr, int startByte, int lengthInBytes)
        {
            new Span<byte>(ptr + startByte, lengthInBytes).Clear();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearMemory(HMem<int> memory)
        {
            memory.AsSpan().Clear();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeMemory<T>(T* ptr, int length, T value) where T : unmanaged
        {
            new Span<T>(ptr, length).Fill(value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignOf<T>() where T : unmanaged
        {
            return TypeAlignment<T>.Alignment;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Align<T>(void* pointer) where T : unmanaged
        {
            return TypeAlignment<T>.Align(pointer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Align<T>(T* pointer) where T : unmanaged
        {
            return TypeAlignment<T>.Align(pointer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSegment<T> Align<T>(UnsafeSegment<T> memory) where T : unmanaged
        {
            return new UnsafeSegment<T>(Align(memory.Ptr), memory.Length);
        }
        internal static int CalculateSizeOf<T>() where T : struct
        {
#if UNITY_2020_3_OR_NEWER
            return UnsafeUtility.SizeOf<T>();
#else
            T value = default;
            Span<T> span = MemoryMarshal.CreateSpan(ref value, 1);
            return MemoryMarshal.AsBytes(span).Length;
#endif
        }



#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        internal static Array CreateArray_Debug(Type type, int elementSize, byte* data, int byteLength)
        {
            int count = byteLength / elementSize;
            var array = Array.CreateInstance(type, count);
            if (array.Length > 0)
            {
                ByteRawArraysUnion union = default;
                union.array = array;
                fixed (byte* arrayPtr = union.bytes)
                {
                    for (int i = 0; i < byteLength; i++)
                    {
                        arrayPtr[i] = data[i];
                    }
                }
            }
            return array;
        }
        [StructLayout(LayoutKind.Explicit)]
        private struct ByteRawArraysUnion
        {
            [FieldOffset(0)]
            public byte[] bytes;
            [FieldOffset(0)]
            public Array array;
        }
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

        #region itterator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CacheTo(this EcsMaskIterator it, EcsSpan source, ref HMem<int> array)
        {
            switch (it.MaskFlags)
            {
                case EcsMaskFlags.Empty:
                    {
                        if (array.Length < source.Count)
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
                    array = Realloc<int>(array, Math.Max(array.Length << 1, 4));
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
                    array = Realloc<int>(array, Math.Max(array.Length << 1, 4));
                }
                array.Ptr[count++] = enumerator.Current;
            }
            return count;
        }
        #endregion
    }
}

namespace DCFApixels.DragonECS.Core.Internal
{
    using System;
    using System.Runtime.CompilerServices;
#if UNITY_2020_3_OR_NEWER
    using Unity.Collections.LowLevel.Unsafe;
#endif
    internal static unsafe class TypeAlignment<T> where T : unmanaged
    {
#if !UNITY_2020_3_OR_NEWER
#pragma warning disable CS0649
        private struct AlignmentProbe
        {
            public byte Prefix;
            public T Value;
        }
#pragma warning restore CS0649
#endif

        public static readonly int Alignment = Calculate();

        /// <summary>
        /// Returns an aligned view of <paramref name="pointer"/>. Ownership remains with
        /// the original pointer, which must still be used for reallocation and release.
        /// The allocation must have at least <c>Value - 1</c> trailing bytes available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Align(void* pointer)
        {
            if (pointer == null) { return null; }

            ulong address = (ulong)pointer;
            ulong alignmentMask = (uint)Alignment - 1UL;
            return (T*)((address + alignmentMask) & ~alignmentMask);
        }

        private static int Calculate()
        {
#if UNITY_2020_3_OR_NEWER
            int alignment = UnsafeUtility.AlignOf<T>();
#else
            int alignment =  DragonUnsafe.CalculateSizeOf<AlignmentProbe>() - DragonUnsafe.CalculateSizeOf<T>();
#endif
            if (alignment <= 0 || (alignment & (alignment - 1)) != 0)
            {
                throw new InvalidOperationException($"Unable to determine a valid alignment for {typeof(T)}.");
            }
            return alignment;
        }
    }
}

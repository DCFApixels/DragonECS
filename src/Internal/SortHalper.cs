#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static unsafe class SortHalper
    {
        #region OrderBy
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SortBy<T, TKey>(Span<T> span, Func<T, TKey> keySelector)
            where TKey : IComparable<TKey>
        {
            var c = new ComparisonWrapper<T>((a, b) => keySelector(a).CompareTo(keySelector(b)));
            SortHalper<T, ComparisonWrapper<T>>.Sort(span, ref c);
        }
        #endregion

        #region Span
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(Span<T> span)
        {
            var c = new ComparerWrapper<T>(Comparer<T>.Default);
            SortHalper<T, ComparerWrapper<T>>.Sort(span, ref c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(Span<T> span, bool _ = false)
            where T : IComparable<T>
        {
            var c = new ComparableWrapper<T>();
            SortHalper<T, ComparableWrapper<T>>.Sort(span, ref c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(Span<T> span, Comparer<T> comparer)
        {
            var c = new ComparerWrapper<T>(comparer);
            SortHalper<T, ComparerWrapper<T>>.Sort(span, ref c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(Span<T> span, Comparison<T> comparison)
        {
            var c = new ComparisonWrapper<T>(comparison);
            SortHalper<T, ComparisonWrapper<T>>.Sort(span, ref c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T, TComparer>(Span<T> span, ref TComparer comparer)
            where TComparer : struct, IComparer<T>
        {
            SortHalper<T, TComparer>.Sort(span, ref comparer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T, TComparer>(Span<T> span, TComparer comparer)
            where TComparer : struct, IComparer<T>
        {
            SortHalper<T, TComparer>.Sort(span, ref comparer);
        }
        #endregion

        #region Utils
        // Таблица для De Bruijn-умножения (позиция старшего бита для чисел 0..31)
        private const int Log2DeBruijn32_Length = 32;
        private static readonly uint* Log2DeBruijn32 = MemoryAllocator.From(new uint[Log2DeBruijn32_Length]
        {
            0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
            8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
        }).Ptr;

        /// <summary>32-битный логарифм по основанию 2 (округление вниз). Для value = 0 возвращает 0.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(uint value)
        {
            // Для нуля сразу возвращаем 0 (по договорённости, чтобы избежать исключения)
            if (value == 0) return 0;

            // Заполняем все биты справа от старшего единицей: превращаем число в 2^n - 1
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;

            // Умножение на константу De Bruijn и сдвиг для получения индекса в таблице
            return (int)Log2DeBruijn32[(value * 0x07C4ACDDu) >> 27];
        }

        /// <summary>64-битный логарифм по основанию 2 (округление вниз). Для value = 0 возвращает 0.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(ulong value)
        {
            if (value == 0) { return 0; }

            uint high = (uint)(value >> 32);
            if (high == 0)
            {
                return Log2((uint)value);
            }
            else
            {
                return 32 + Log2(high);
            }
        }
        #endregion
    }
    // a > b = return > 0
    // int Compare(T a, T b);

    //IComparer<T> comparer
    //if (comparer == null)
    //{
    //    comparer = Comparer<T>.Default;
    //}
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class SortHalper<T, TComparer>
        where TComparer : struct, IComparer<T>
    {
        private const int IntrosortSizeThreshold = 16;

        #region Public
        public static void Sort(Span<T> keys, ref TComparer comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            IntrospectiveSort(keys, ref comparer);
        }
        public static int BinarySearch(T[] array, int index, int length, T value, ref TComparer comparer)
        {
            Debug.Assert(array != null, "Check the arguments in the caller!");
            Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(array[i], value);

                if (order == 0) return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
        #endregion

        #region Internal
        internal static void IntrospectiveSort(Span<T> keys, ref TComparer comparer)
        {
            //Debug.Assert(comparer != null);
            if (keys.Length > 1)
            {
                IntroSort(keys, 2 * (SortHalper.Log2((uint)keys.Length) + 1), ref comparer);
            }
        }

        // IntroSort is recursive; block it from being inlined into itself as
        // this is currenly not profitable.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void IntroSort(Span<T> keys, int depthLimit, ref TComparer comparer)
        {
            Debug.Assert(!keys.IsEmpty);
            Debug.Assert(depthLimit >= 0);
            //Debug.Assert(comparer != null);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= IntrosortSizeThreshold)
                {

                    if (partitionSize == 2)
                    {
                        SwapIfGreater(keys, ref comparer, 0, 1);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreater(keys, ref comparer, 0, 1);
                        SwapIfGreater(keys, ref comparer, 0, 2);
                        SwapIfGreater(keys, ref comparer, 1, 2);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize), ref comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize), ref comparer);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize), ref comparer);

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys.Slice(p + 1, partitionSize - (p + 1)), depthLimit, ref comparer);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(Span<T> keys, ref TComparer comparer)
        {
            Debug.Assert(keys.Length >= IntrosortSizeThreshold);
            //Debug.Assert(comparer != null);

            int hi = keys.Length - 1;

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = hi >> 1;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreater(keys, ref comparer, 0, middle);  // swap the low with the mid point
            SwapIfGreater(keys, ref comparer, 0, hi);   // swap the low with the high
            SwapIfGreater(keys, ref comparer, middle, hi); // swap the middle with the high

            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0) ;
                while (comparer.Compare(pivot, keys[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(keys, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(keys, left, hi - 1);
            }
            return left;
        }

        private static void HeapSort(Span<T> keys, ref TComparer comparer)
        {
            Debug.Assert(!keys.IsEmpty);
            //Debug.Assert(comparer != null);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, i, n, ref comparer);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(keys, 0, i - 1);
                DownHeap(keys, 1, i - 1, ref comparer);
            }
        }

        private static void DownHeap(Span<T> keys, int i, int n, ref TComparer comparer)
        {
            //Debug.Assert(comparer != null);

            T d = keys[i - 1];
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && comparer.Compare(keys[child - 1], keys[child]) < 0)
                {
                    child++;
                }

                if (!(comparer.Compare(d, keys[child - 1]) < 0))
                    break;

                keys[i - 1] = keys[child - 1];
                i = child;
            }

            keys[i - 1] = d;
        }

        private static void InsertionSort(Span<T> keys, ref TComparer comparer)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                T t = keys[i + 1];

                int j = i;
                while (j >= 0 && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    j--;
                }

                keys[j + 1] = t;
            }
        }
        #endregion

        #region Swaps
        private static void SwapIfGreater(Span<T> keys, ref TComparer comparer, int i, int j)
        {
            Debug.Assert(i != j);
            if (comparer.Compare(keys[i], keys[j]) > 0)
            {
                T key = keys[i];
                keys[i] = keys[j];
                keys[j] = key;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(Span<T> a, int i, int j)
        {
            Debug.Assert(i != j);
            T t = a[i];
            a[i] = a[j];
            a[j] = t;
        }
        #endregion
    }

    #region Wrappers
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal readonly struct ComparableWrapper<T> : IComparer<T>
        where T : IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y) { return x.CompareTo(y); }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal readonly struct ComparisonWrapper<T> : IComparer<T>
    {
        public readonly Comparison<T> Comparison;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComparisonWrapper(Comparison<T> comparison)
        {
            Comparison = comparison;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return Comparison(x, y);
        }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal readonly struct ComparerWrapper<T> : IComparer<T>
    {
        public readonly Comparer<T> Comparer;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComparerWrapper(Comparer<T> comparer)
        {
            Comparer = comparer;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return Comparer.Compare(x, y);
        }
    }
    #endregion
}

































//namespace DCFApixels.DragonECS.Core.Internal
//{
//    internal interface IStructComparer<T> : IComparer<T>
//    {
//        // a > b = return > 0
//        // int Compare(T a, T b);
//    }
//
//#if ENABLE_IL2CPP
//    [Il2CppSetOption(Option.NullChecks, false)]
//    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
//#endif
//    internal static class ArraySortHalperX<T>
//    {
//        private const int IntrosortSizeThreshold = 16;
//
//        #region IStructComparer
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void SwapIfGreater<TComparer>(T[] items, ref TComparer comparer, int i, int j) where TComparer : IStructComparer<T>
//        {
//            if (comparer.Compare(items[i], items[j]) > 0)
//            {
//                T key = items[i];
//                items[i] = items[j];
//                items[j] = key;
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void InsertionSort<TComparer>(T[] items, ref TComparer comparer) where TComparer : IStructComparer<T>
//        {
//            for (int i = 0; i < items.Length - 1; i++)
//            {
//                T t = items[i + 1];
//
//                int j = i;
//                while (j >= 0 && comparer.Compare(t, items[j]) < 0)
//                {
//                    items[j + 1] = items[j];
//                    j--;
//                }
//
//                items[j + 1] = t;
//            }
//        }
//
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void Sort<TComparer>(T[] items, ref TComparer comparer) where TComparer : IStructComparer<T>
//        {
//            int length = items.Length;
//            if (length == 1)
//            {
//                return;
//            }
//
//            if (length <= IntrosortSizeThreshold)
//            {
//
//                if (length == 2)
//                {
//                    SwapIfGreater(items, ref comparer, 0, 1);
//                    return;
//                }
//
//                if (length == 3)
//                {
//                    SwapIfGreater(items, ref comparer, 0, 1);
//                    SwapIfGreater(items, ref comparer, 0, 2);
//                    SwapIfGreater(items, ref comparer, 1, 2);
//                    return;
//                }
//
//                InsertionSort(items, ref comparer);
//                return;
//            }
//
//            IStructComparer<T> packed = comparer;
//            Array.Sort(items, 0, items.Length, packed);
//            comparer = (TComparer)packed;
//        }
//        #endregion
//
//        #region Comparison
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void SwapIfGreater(T[] items, Comparison<T> comparison, int i, int j)
//        {
//            if (comparison(items[i], items[j]) > 0)
//            {
//                T key = items[i];
//                items[i] = items[j];
//                items[j] = key;
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void InsertionSort(T[] items, int length, Comparison<T> comparison)
//        {
//            for (int i = 0; i < length - 1; i++)
//            {
//                T t = items[i + 1];
//
//                int j = i;
//                while (j >= 0 && comparison(t, items[j]) < 0)
//                {
//                    items[j + 1] = items[j];
//                    j--;
//                }
//
//                items[j + 1] = t;
//            }
//        }
//
//#if ENABLE_IL2CPP
//        [Il2CppSetOption(Option.NullChecks, false)]
//        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
//#endif
//        private class ComparisonHach : IComparer<T>
//        {
//            public static readonly ComparisonHach Instance = new ComparisonHach();
//            public Comparison<T> comparison;
//            private ComparisonHach() { }
//            public int Compare(T x, T y) { return comparison(x, y); }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void Sort(T[] items, Comparison<T> comparison)
//        {
//            Sort(items, comparison, items.Length);
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void Sort(T[] items, Comparison<T> comparison, int length)
//        {
//            if (length <= IntrosortSizeThreshold)
//            {
//                if (length == 1)
//                {
//                    return;
//                }
//                if (length == 2)
//                {
//                    SwapIfGreater(items, comparison, 0, 1);
//                    return;
//                }
//                if (length == 3)
//                {
//                    SwapIfGreater(items, comparison, 0, 1);
//                    SwapIfGreater(items, comparison, 0, 2);
//                    SwapIfGreater(items, comparison, 1, 2);
//                    return;
//                }
//                InsertionSort(items, length, comparison);
//                return;
//            }
//            ComparisonHach.Instance.comparison = comparison;
//            Array.Sort(items, 0, length, ComparisonHach.Instance);
//        }
//        #endregion
//    }
//
//#if ENABLE_IL2CPP
//    [Il2CppSetOption(Option.NullChecks, false)]
//    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
//#endif
//    internal static unsafe class UnsafeArraySortHalperX<T> where T : unmanaged
//    {
//        private const int IntrosortSizeThreshold = 16;
//
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void SwapIfGreater<TComparer>(T* items, ref TComparer comparer, int i, int j) where TComparer : IStructComparer<T>
//        {
//            if (comparer.Compare(items[i], items[j]) > 0)
//            {
//                T key = items[i];
//                items[i] = items[j];
//                items[j] = key;
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void InsertionSort_Unchecked<TComparer>(T* items, int length, ref TComparer comparer) where TComparer : IStructComparer<T>
//        {
//            for (int i = 0; i < length - 1; i++)
//            {
//                T t = items[i + 1];
//
//                int j = i;
//                while (j >= 0 && comparer.Compare(t, items[j]) < 0)
//                {
//                    items[j + 1] = items[j];
//                    j--;
//                }
//
//                items[j + 1] = t;
//            }
//        }
//        public static void InsertionSort<TComparer>(T* items, int length, ref TComparer comparer) where TComparer : IStructComparer<T>
//        {
//            if (length == 1)
//            {
//                return;
//            }
//
//            if (length == 2)
//            {
//                SwapIfGreater(items, ref comparer, 0, 1);
//                return;
//            }
//
//            if (length == 3)
//            {
//                SwapIfGreater(items, ref comparer, 0, 1);
//                SwapIfGreater(items, ref comparer, 0, 2);
//                SwapIfGreater(items, ref comparer, 1, 2);
//                return;
//            }
//
//            InsertionSort_Unchecked(items, length, ref comparer);
//        }
//    }
//}
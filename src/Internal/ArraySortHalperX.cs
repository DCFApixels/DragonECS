#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Internal
{
    internal interface IStructComparer<T> : IComparer<T>
    {
        // a > b = return > 0
        // int Compare(T a, T b);
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class ArraySortHalperX<T>
    {
        private const int IntrosortSizeThreshold = 16;

        #region IStructComparer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SwapIfGreater<TComparer>(T[] items, ref TComparer comparer, int i, int j) where TComparer : IStructComparer<T>
        {
            if (comparer.Compare(items[i], items[j]) > 0)
            {
                T key = items[i];
                items[i] = items[j];
                items[j] = key;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertionSort<TComparer>(T[] items, ref TComparer comparer) where TComparer : IStructComparer<T>
        {
            for (int i = 0; i < items.Length - 1; i++)
            {
                T t = items[i + 1];

                int j = i;
                while (j >= 0 && comparer.Compare(t, items[j]) < 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = t;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TComparer>(T[] items, ref TComparer comparer) where TComparer : IStructComparer<T>
        {
            int length = items.Length;
            if (length == 1)
            {
                return;
            }

            if (length <= IntrosortSizeThreshold)
            {

                if (length == 2)
                {
                    SwapIfGreater(items, ref comparer, 0, 1);
                    return;
                }

                if (length == 3)
                {
                    SwapIfGreater(items, ref comparer, 0, 1);
                    SwapIfGreater(items, ref comparer, 0, 2);
                    SwapIfGreater(items, ref comparer, 1, 2);
                    return;
                }

                InsertionSort(items, ref comparer);
                return;
            }

            IStructComparer<T> packed = comparer;
            Array.Sort(items, 0, items.Length, packed);
            comparer = (TComparer)packed;
        }
        #endregion

        #region Comparison
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SwapIfGreater(T[] items, Comparison<T> comparison, int i, int j)
        {
            if (comparison(items[i], items[j]) > 0)
            {
                T key = items[i];
                items[i] = items[j];
                items[j] = key;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertionSort(T[] items, int length, Comparison<T> comparison)
        {
            for (int i = 0; i < length - 1; i++)
            {
                T t = items[i + 1];

                int j = i;
                while (j >= 0 && comparison(t, items[j]) < 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = t;
            }
        }

#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        private class ComparisonHach : IComparer<T>
        {
            public static readonly ComparisonHach Instance = new ComparisonHach();
            public Comparison<T> comparison;
            private ComparisonHach() { }
            public int Compare(T x, T y) { return comparison(x, y); }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(T[] items, Comparison<T> comparison)
        {
            Sort(items, comparison, items.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(T[] items, Comparison<T> comparison, int length)
        {
            if (length <= IntrosortSizeThreshold)
            {
                if (length == 1)
                {
                    return;
                }
                if (length == 2)
                {
                    SwapIfGreater(items, comparison, 0, 1);
                    return;
                }
                if (length == 3)
                {
                    SwapIfGreater(items, comparison, 0, 1);
                    SwapIfGreater(items, comparison, 0, 2);
                    SwapIfGreater(items, comparison, 1, 2);
                    return;
                }
                InsertionSort(items, length, comparison);
                return;
            }
            ComparisonHach.Instance.comparison = comparison;
            Array.Sort(items, 0, length, ComparisonHach.Instance);
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static unsafe class UnsafeArraySortHalperX<T> where T : unmanaged
    {
        private const int IntrosortSizeThreshold = 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SwapIfGreater<TComparer>(T* items, ref TComparer comparer, int i, int j) where TComparer : IStructComparer<T>
        {
            if (comparer.Compare(items[i], items[j]) > 0)
            {
                T key = items[i];
                items[i] = items[j];
                items[j] = key;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertionSort_Unchecked<TComparer>(T* items, int length, ref TComparer comparer) where TComparer : IStructComparer<T>
        {
            for (int i = 0; i < length - 1; i++)
            {
                T t = items[i + 1];

                int j = i;
                while (j >= 0 && comparer.Compare(t, items[j]) < 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = t;
            }
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void OptimizedBubbleSort_Unchecked<TComparer>(T* items, int length, ref TComparer comparer) where TComparer : IComparerX<T>
        //{
        //    for (int i = 0, n = length - 1; i < n; i++)
        //    {
        //        bool noSwaped = true;
        //        for (int j = 0; j < n - i;)
        //        {
        //            ref T j0 = ref items[j++];
        //            if(comparer.Compare(j0, items[j]) < 0)
        //            {
        //                T tmp = items[j];
        //                items[j] = j0;
        //                j0 = tmp;
        //                noSwaped = false;
        //            }
        //        }
        //        if (noSwaped)
        //            break;
        //    }
        //}
        public static void InsertionSort<TComparer>(T* items, int length, ref TComparer comparer) where TComparer : IStructComparer<T>
        {
            if (length == 1)
            {
                return;
            }

            if (length == 2)
            {
                SwapIfGreater(items, ref comparer, 0, 1);
                return;
            }

            if (length == 3)
            {
                SwapIfGreater(items, ref comparer, 0, 1);
                SwapIfGreater(items, ref comparer, 0, 2);
                SwapIfGreater(items, ref comparer, 1, 2);
                return;
            }

            InsertionSort_Unchecked(items, length, ref comparer);
        }
        //public static void OptimizedBubbleSort<TComparer>(T* items, int length, ref TComparer comparer) where TComparer : IComparerX<T>
        //{
        //    if (length == 1)
        //    {
        //        return;
        //    }
        //
        //    if (length == 2)
        //    {
        //        SwapIfGreater(items, ref comparer, 0, 1);
        //        return;
        //    }
        //
        //    if (length == 3)
        //    {
        //        SwapIfGreater(items, ref comparer, 0, 1);
        //        SwapIfGreater(items, ref comparer, 0, 2);
        //        SwapIfGreater(items, ref comparer, 1, 2);
        //        return;
        //    }
        //
        //    OptimizedBubbleSort_Unchecked(items, length, ref comparer);
        //}
    }
}



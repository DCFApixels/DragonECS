using System;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace DCFApixels.DragonECS
{
    #region Incs/Excs base
    public interface IMaskCondition
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>;
    }
    #endregion

    #region Incs
    public interface IInc : IMaskCondition { }
    public struct Inc : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype> => Array.Empty<int>();
    }
    public struct Inc<T0> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T6>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T6>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T7>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T6>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T7>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T8>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T6>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T7>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T8>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T9>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T6>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T7>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T8>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T9>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T10>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T6>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T7>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T8>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T9>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T10>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T11>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    #endregion

    #region Excs
    public interface IExc : IMaskCondition { }
    public struct Exc : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype> => Array.Empty<int>();
    }
    public struct Exc<T0> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Exc<T0, T1> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Exc<T0, T1, T2> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Exc<T0, T1, T2, T3> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4, T5> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : EcsWorld<TWorldArchetype>
        {
            return new int[]
            {
                ComponentIndexer.GetComponentId<T0>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T1>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T2>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T3>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T4>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
                ComponentIndexer.GetComponentId<T5>(ComponentIndexer.GetWorldId<TWorldArchetype>()),
            };
        }
    }
    #endregion

    #region EcsMask
    public class EcsComponentMask
    {
        internal Type WorldArchetypeType;
        internal int[] Inc;
        internal int[] Exc;
        public override string ToString()
        {
            return $"Inc({string.Join(", ", Inc)}) Exc({string.Join(", ", Exc)})";
        }
    }


    public sealed class EcsMask : EcsComponentMask
    {
       // internal readonly Type WorldArchetypeType;
        internal readonly int UniqueID;
        //internal readonly int[] Inc;
        //internal readonly int[] Exc;

        //internal int IncCount
        //{
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    get => Inc.Length;
        //}
        //internal int ExcCount
        //{
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    get => Exc.Length;
        //}
        internal EcsMask(Type worldArchetypeType, int uniqueID, int[] inc, int[] exc)
        {
            WorldArchetypeType = worldArchetypeType;
            UniqueID = uniqueID;
            Inc = inc;
            Exc = exc;
        }
    }

    public static class EcsMaskMap<TWorldArchetype>
        where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        private static int _count;
        private static int _capacity;

        public static int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
        public static int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _capacity;
        }

        public static EcsMask GetMask<TInc, TExc>()
            where TInc : struct, IInc
            where TExc : struct, IExc
        {
            return Activator<TInc, TExc>.instance;
        }

        private class Activator<TInc, TExc>
            where TInc : struct, IInc
            where TExc : struct, IExc
        {
            static Activator()
            {
            //    var inc_ = new TInc().GetComponentsIDs<TWorldArchetype>();
            //    var exc_ = new TExc().GetComponentsIDs<TWorldArchetype>();
            //    Array.Sort(inc_);
            //    Array.Sort(exc_);
            //
            //    Type thisType = typeof(Activator<TInc, TExc>);
            //
            //    Type sortedIncType = typeof(TInc);
            //    if (sortedIncType.IsGenericType)
            //    {
            //        Type[] sortedInc = new Type[inc_.Length];
            //        for (int i = 0; i < sortedInc.Length; i++)
            //            sortedInc[i] =  EcsWorld<TWorldArchetype>.ComponentType.types[inc_[i]];
            //        sortedIncType = sortedIncType.GetGenericTypeDefinition().MakeGenericType(sortedInc);
            //    }
            //    Type sortedExcType = typeof(TExc);
            //    if (sortedExcType.IsGenericType)
            //    {
            //        Type[] sortedExc = new Type[exc_.Length];
            //        for (int i = 0; i < sortedExc.Length; i++)
            //            sortedExc[i] = EcsWorld<TWorldArchetype>.ComponentType.types[exc_[i]];
            //        sortedExcType = sortedExcType.GetGenericTypeDefinition().MakeGenericType(sortedExc);
            //    }
            //
            //    Type targetType = typeof(Activator<,>).MakeGenericType(typeof(TWorldArchetype), sortedIncType, sortedExcType);
            //
            //    if (targetType != thisType)
            //    {
            //        instance = (EcsMask)targetType.GetField(nameof(instance), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            //        return;
            //    }
            //
            //    var id = _count++;
            //    if (_count >= _capacity)
            //        _capacity <<= 1;
            //
            //    instance = new EcsMask(typeof(TWorldArchetype), id, inc_, exc_);
            }

            public readonly static EcsMask instance;
        }
    }
    #endregion
}

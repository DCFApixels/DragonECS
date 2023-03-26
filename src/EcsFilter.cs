using System;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace DCFApixels.DragonECS
{
    #region Incs/Excs base
    public interface IMaskCondition
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype;
    }
    #endregion

    #region Incs
    public interface IInc : IMaskCondition { }
    public struct Inc : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype => Array.Empty<int>();
    }
    public struct Inc<T0> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T6>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T6>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T7>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T6>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T7>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T8>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T6>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T7>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T8>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T9>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T6>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T7>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T8>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T9>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T10>.uniqueID,
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T6>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T7>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T8>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T9>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T10>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T11>.uniqueID,
            };
        }
    }
    #endregion

    #region Excs
    public interface IExc : IMaskCondition { }
    public struct Exc : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype => Array.Empty<int>();
    }
    public struct Exc<T0> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
            };
        }
    }
    public struct Exc<T0, T1> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
            };
        }
    }
    public struct Exc<T0, T1, T2> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
            };
        }
    }
    public struct Exc<T0, T1, T2, T3> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4, T5> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T2>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T3>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T4>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T5>.uniqueID,
            };
        }
    }
    #endregion

    #region EcsMask
    public sealed class EcsMask
    {
        internal readonly Type WorldArchetypeType;
        internal readonly int UniqueID;
        internal readonly int[] Inc;
        internal readonly int[] Exc;

        internal int IncCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Inc.Length;
        }
        internal int ExcCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Exc.Length;
        }
        internal EcsMask(Type worldArchetypeType, int uniqueID, int[] inc, int[] exc)
        {
            WorldArchetypeType = worldArchetypeType;
            UniqueID = uniqueID;
            Inc = inc;
            Exc = exc;
        }
    }

    public static class EcsMaskMap<TWorldArchetype>
        where TWorldArchetype : IWorldArchetype
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
                var inc = new TInc().GetComponentsIDs<TWorldArchetype>();
                var exc = new TExc().GetComponentsIDs<TWorldArchetype>();
                Array.Sort(inc);
                Array.Sort(exc);

                Type thisType = typeof(Activator<TInc, TExc>);

                Type sortedIncType = typeof(TInc);
                if (sortedIncType.IsGenericType)
                {
                    Type[] sortedInc = new Type[inc.Length];
                    for (int i = 0; i < sortedInc.Length; i++)
                        sortedInc[i] = EcsWorld<TWorldArchetype>.ComponentType.types[inc[i]];
                    sortedIncType = sortedIncType.GetGenericTypeDefinition().MakeGenericType(sortedInc);
                }
                Type sortedExcType = typeof(TExc);
                if (sortedExcType.IsGenericType)
                {
                    Type[] sortedExc = new Type[exc.Length];
                    for (int i = 0; i < sortedExc.Length; i++)
                        sortedExc[i] = EcsWorld<TWorldArchetype>.ComponentType.types[exc[i]];
                    sortedExcType = sortedExcType.GetGenericTypeDefinition().MakeGenericType(sortedExc);
                }

                Type targetType = typeof(Activator<,>).MakeGenericType(typeof(TWorldArchetype), sortedIncType, sortedExcType);

                if(targetType != thisType)
                {
                    instance = (EcsMask)targetType.GetField(nameof(instance), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                    return;
                }

                var id = _count++;
                if (_count >= _capacity)
                    _capacity <<= 1;

                instance = new EcsMask(typeof(TWorldArchetype), id, inc, exc);
            }

            public readonly static EcsMask instance;
        }
    }
    #endregion

    #region Filter
    public interface IEcsFilter
    {
        public IEcsWorld World { get; }
        public EcsMask Mask { get; }
        public IEcsReadonlyGroup Entities { get; }
        public int EntitiesCount { get; }
    }

    public class EcsFilter : IEcsFilter
    {
        private readonly IEcsWorld _source;
        private readonly EcsGroup _entities;
        private readonly EcsMask _mask;

        #region Properties
        public IEcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source;
        }
        public EcsMask Mask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _mask;
        }
        public IEcsReadonlyGroup Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entities;
        }
        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entities.Count;
        }
        #endregion

        #region Constrcutors
        internal EcsFilter(IEcsWorld source, EcsMask mask, int capasity)
        {
            _source = source;
            _mask = mask;
            _entities = new EcsGroup(source, capasity);
        }
        #endregion

        #region EntityChangedReact
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(int entityID)
        {
            _entities.Add(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(int entityID)
        {
            _entities.Remove(entityID);
        }
        #endregion
    }
    #endregion
}

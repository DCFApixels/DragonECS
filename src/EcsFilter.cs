using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    #region Incs/Excs base
    public interface ICondition
    {
        public int[] GetComponentsIDs();
    }
    #endregion

    #region Incs
    public interface IInc : ICondition { }

    public struct Inc<T0> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID,
                ComponentType<T7>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID,
                ComponentType<T7>.uniqueID,
                ComponentType<T8>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID,
                ComponentType<T7>.uniqueID,
                ComponentType<T8>.uniqueID,
                ComponentType<T9>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID,
                ComponentType<T7>.uniqueID,
                ComponentType<T8>.uniqueID,
                ComponentType<T9>.uniqueID,
                ComponentType<T10>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IInc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID,
                ComponentType<T7>.uniqueID,
                ComponentType<T8>.uniqueID,
                ComponentType<T9>.uniqueID,
                ComponentType<T10>.uniqueID,
                ComponentType<T11>.uniqueID
            };
        }
    }
    #endregion

    #region Excs
    public interface IExc : ICondition { }

    public struct Exc<T0> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1, T2> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1, T2, T3> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4, T5> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4, T5, T6> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID
            };
        }
    }
    public struct Exc<T0, T1, T2, T3, T4, T5, T6, T7> : IExc
    {
        public int[] GetComponentsIDs()
        {
            return new int[]
            {
                ComponentType<T0>.uniqueID,
                ComponentType<T1>.uniqueID,
                ComponentType<T2>.uniqueID,
                ComponentType<T3>.uniqueID,
                ComponentType<T4>.uniqueID,
                ComponentType<T5>.uniqueID,
                ComponentType<T6>.uniqueID,
                ComponentType<T7>.uniqueID
            };
        }
    }
    #endregion

    #region Masks
    public abstract class Mask
    {
        protected internal static int _typeIDIncrement = 0;

        internal abstract int[] Include { get; }
        internal abstract int[] Exclude { get; }

        public abstract int ID { get; }

        public int IncCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Include.Length;
        }
        public int ExcCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Exclude.Length;
        }
    }
    public sealed class Mask<TInc> : Mask
        where TInc : struct, IInc
    {
        internal static readonly int[] include = new TInc().GetComponentsIDs();
        internal static readonly int[] exclude = Array.Empty<int>();
        public static readonly int id = _typeIDIncrement++;
        private static Mask<TInc> _instance = new Mask<TInc>();

        private Mask() { }

        public static Mask<TInc> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance;
        }
        public override int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => id;
        }
        internal override int[] Include
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => include;
        }
        internal override int[] Exclude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => exclude;
        }
    }
    public sealed class Mask<TInc, TExc> : Mask
        where TInc : struct, IInc
        where TExc : struct, IExc
    {
        internal static readonly int[] include = new TInc().GetComponentsIDs();
        internal static readonly int[] exclude = new TExc().GetComponentsIDs();
        public static readonly int id = _typeIDIncrement++;
        private static Mask<TInc, TExc> _instance = new Mask<TInc, TExc>();

        private Mask() { }

        public static Mask<TInc, TExc> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance;
        }
        public override int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => id;
        }
        internal override int[] Include
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => include;
        }
        internal override int[] Exclude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => exclude;
        }
    }
    #endregion

    #region Filter
    public interface IEcsFilter
    {
        public EcsWorld World { get; }
        public Mask Mask { get; }
        public IEcsReadonlyGroup Entities { get; }
        public int EntitiesCount { get; }
    }

    public class EcsFilter : IEcsFilter
    {
        private readonly EcsWorld _source;
        private readonly EcsGroup _entities;
        private readonly Mask _mask;

        #region Properties
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source;
        }
        public Mask Mask
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
        internal EcsFilter(EcsWorld source, Mask mask, int capasity)
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

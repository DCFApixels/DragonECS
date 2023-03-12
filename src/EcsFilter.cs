using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    #region Incs/Excs base
    public interface ICondition
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype;
    }
    #endregion

    #region Incs
    public interface IInc : ICondition { }

    public struct Inc : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype => Array.Empty<int>();
    }
    public struct Inc<T0> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>()
            where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID
            };
        }
    }
    public struct Inc<T0, T1> : IInc
    {
        public int[] GetComponentsIDs<TWorldArchetype>()
            where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID
            };
        }
    }
    #endregion

    #region Excs
    public interface IExc : ICondition { }

    public struct Exc : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>() where TWorldArchetype : IWorldArchetype => Array.Empty<int>();
    }
    public struct Exc<T0> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>()
            where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
            };
        }
    }
    public struct Exc<T0, T1> : IExc
    {
        public int[] GetComponentsIDs<TWorldArchetype>()
            where TWorldArchetype : IWorldArchetype
        {
            return new int[]
            {
                EcsWorld<TWorldArchetype>.ComponentType<T0>.uniqueID,
                EcsWorld<TWorldArchetype>.ComponentType<T1>.uniqueID
            };
        }
    }
    #endregion

    #region BakedMask
    public abstract class BakedMask
    {
        internal readonly int[] Inc;
        internal readonly int[] Exc;
        internal readonly Mask Mask;

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

        //Уникальный айди в рамках одного архиетипа мира
        internal abstract int UniqueID { get; }
        internal abstract Type WorldArchetypeType { get; }

        protected BakedMask(int[] inc, int[] exc, Mask mask)
        {
            Inc = inc;
            Exc = exc;
            Mask = mask;
        }
    }

    public abstract class BakedMask<TWorldArchetype> : BakedMask
    {
        internal static int increment = 1;
        internal static int capacity = 512;

        protected BakedMask(int[] inc, int[] exc, Mask mask) : base(inc, exc, mask) { }


    }

    public sealed class BakedMask<TWorldArchetype, TMask> : BakedMask<TWorldArchetype>
        where TWorldArchetype : IWorldArchetype
        where TMask : Mask, new()
    {
        public static readonly int uniqueID;

        static BakedMask()
        {
            uniqueID = increment++;
#if DEBUG || DCFAECS_NO_SANITIZE_CHECKS
            if (uniqueID >= ushort.MaxValue)
                throw new EcsFrameworkException($"No more room for new BakedMask for this {typeof(TWorldArchetype).FullName} IWorldArchetype");
#endif
            if (increment > capacity)
                capacity <<= 1;

            _instance = new BakedMask<TWorldArchetype, TMask>();
        }

        private BakedMask() : base(
            MaskSingleton<TMask>.Instance.MakeInc<IWorldArchetype>(),
            MaskSingleton<TMask>.Instance.MakeExc<IWorldArchetype>(),
            MaskSingleton<TMask>.Instance)
        { }

        private static readonly BakedMask<TWorldArchetype, TMask> _instance;
        public static BakedMask<TWorldArchetype, TMask> Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance;
        }
        internal override int UniqueID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => uniqueID;
        }
        internal override Type WorldArchetypeType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => typeof(IWorldArchetype);
        }
    }
    #endregion

    #region Masks
    public abstract class Mask
    {
        internal abstract int[] MakeInc<TWorldArchetype>() where TWorldArchetype : IWorldArchetype;
        internal abstract int[] MakeExc<TWorldArchetype>() where TWorldArchetype : IWorldArchetype;
        public abstract BakedMask GetBaked<TWorldArchetype>() where TWorldArchetype : IWorldArchetype;
    }
    public abstract class MaskSingleton<TSelf> 
        where TSelf : Mask, new()
    {
        protected static TSelf _instance = new TSelf();
        internal static TSelf Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _instance;
        }
    }
    public class Mask<TInc> : Mask where TInc : struct, IInc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int[] MakeInc<TWorldArchetype>() => new TInc().GetComponentsIDs<TWorldArchetype>().Sort();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int[] MakeExc<TWorldArchetype>() => Array.Empty<int>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BakedMask GetBaked<TWorldArchetype>() => BakedMask<TWorldArchetype, Mask<TInc>>.Instance;
    }
    public class Mask<TInc, TExc> : Mask where TInc : struct, IInc where TExc : struct, IExc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int[] MakeInc<TWorldArchetype>() => new TInc().GetComponentsIDs<TWorldArchetype>().Sort();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int[] MakeExc<TWorldArchetype>() => new TExc().GetComponentsIDs<TWorldArchetype>().Sort();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BakedMask GetBaked<TWorldArchetype>() => BakedMask<TWorldArchetype, Mask<TInc, TExc>>.Instance;
    }

    internal static class ArrayExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T[] Sort<T>(this T[] self)
        {
            Array.Sort(self);
            return self;
        }
    }
    #endregion

    #region Filter
    public interface IEcsFilter
    {
        public IEcsWorld World { get; }
        public BakedMask Mask { get; }
        public IEcsReadonlyGroup Entities { get; }
        public int EntitiesCount { get; }
    }

    public class EcsFilter : IEcsFilter
    {
        private readonly IEcsWorld _source;
        private readonly EcsGroup _entities;
        private readonly BakedMask _mask;

        #region Properties
        public IEcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source;
        }
        public BakedMask Mask
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
        internal EcsFilter(IEcsWorld source, BakedMask mask, int capasity)
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

#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;

namespace DCFApixels.DragonECS
{
    public interface IEntityStorage
    {
        EcsWorld World { get; }
        EcsSpan ToSpan();
    }
    public static class QueriesExtensions
    {
        #region Where
        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect);
        }
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : new()
        {
            return group.ToSpan().Where(out aspect);
        }
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        public static EcsSpan Where<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask);
        }
        public static EcsSpan Where(this EcsReadonlyGroup group, IComponentMask mask)
        {
            return group.ToSpan().Where(mask);
        }
        public static EcsSpan Where(this EcsSpan span, IComponentMask mask)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        #endregion

        #region Where with sort
        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect, comparison);
        }
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
        {
            return group.ToSpan().Where(out aspect, comparison);
        }
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span, comparison);
        }

        public static EcsSpan Where<TCollection>(this TCollection entities, IComponentMask mask, Comparison<int> comparison)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask, comparison);
        }
        public static EcsSpan Where(this EcsReadonlyGroup group, IComponentMask mask, Comparison<int> comparison)
        {
            return group.ToSpan().Where(mask, comparison);
        }
        public static EcsSpan Where(this EcsSpan span, IComponentMask mask, Comparison<int> comparison)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        #endregion

        #region WhereToGroup
        public static EcsReadonlyGroup WhereToGroup<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out aspect);
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : new()
        {
            return group.ToSpan().WhereToGroup(out aspect);
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereToGroupExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        public static EcsReadonlyGroup WhereToGroup<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(mask);
        }
        public static EcsReadonlyGroup WhereToGroup(this EcsReadonlyGroup group, IComponentMask mask)
        {
            return group.ToSpan().WhereToGroup(mask);
        }
        public static EcsReadonlyGroup WhereToGroup(this EcsSpan span, IComponentMask mask)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereToGroupExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        #endregion
    }
}
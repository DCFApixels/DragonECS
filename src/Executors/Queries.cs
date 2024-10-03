using DCFApixels.DragonECS.Internal;
using System;

namespace DCFApixels.DragonECS
{
    public interface IEntityStorage
    {
        EcsSpan ToSpan();
    }
    public static class Queries
    {
        #region Where
        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect);
        }
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().Where(out aspect);
        }
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : EcsAspect, new()
        {
            EcsWorld world = span.World;
            world.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }
        public static EcsSpan Where<TCollection>(this TCollection entities, EcsStaticMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask);
        }
        public static EcsSpan Where(this EcsReadonlyGroup group, EcsStaticMask mask)
        {
            return group.ToSpan().Where(mask);
        }
        public static EcsSpan Where(this EcsSpan span, EcsStaticMask mask)
        {
            EcsWorld world = span.World;
            var executor = world.GetExecutor<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        public static EcsSpan Where<TCollection>(this TCollection entities, EcsMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask);
        }
        public static EcsSpan Where(this EcsReadonlyGroup group, EcsMask mask)
        {
            return group.ToSpan().Where(mask);
        }
        public static EcsSpan Where(this EcsSpan span, EcsMask mask)
        {
            EcsWorld world = span.World;
            var executor = world.GetExecutor<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        #endregion

        #region Where with sort
        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities, out TAspect aspect, Comparison<int> comparison)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect, comparison);
        }
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, Comparison<int> comparison)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().Where(out aspect, comparison);
        }
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect, Comparison<int> comparison)
            where TAspect : EcsAspect, new()
        {
            EcsWorld world = span.World;
            world.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span, comparison);
        }
        public static EcsSpan Where<TCollection>(this TCollection entities, EcsStaticMask mask, Comparison<int> comparison)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask, comparison);
        }
        public static EcsSpan Where(this EcsReadonlyGroup group, EcsStaticMask mask, Comparison<int> comparison)
        {
            return group.ToSpan().Where(mask, comparison);
        }
        public static EcsSpan Where(this EcsSpan span, EcsStaticMask mask, Comparison<int> comparison)
        {
            EcsWorld world = span.World;
            var executor = world.GetExecutor<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        public static EcsSpan Where<TCollection>(this TCollection entities, EcsMask mask, Comparison<int> comparison)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask, comparison);
        }
        public static EcsSpan Where(this EcsReadonlyGroup group, EcsMask mask, Comparison<int> comparison)
        {
            return group.ToSpan().Where(mask, comparison);
        }
        public static EcsSpan Where(this EcsSpan span, EcsMask mask, Comparison<int> comparison)
        {
            EcsWorld world = span.World;
            var executor = world.GetExecutor<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span, comparison);
        }
        #endregion

        #region WhereToGroup
        public static EcsReadonlyGroup WhereToGroup<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out aspect);
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().WhereToGroup(out aspect);
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : EcsAspect, new()
        {
            EcsWorld world = span.World;
            world.GetQueryCache(out EcsWhereToGroupExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }
        public static EcsReadonlyGroup WhereToGroup<TCollection>(this TCollection entities, EcsStaticMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(mask);
        }
        public static EcsReadonlyGroup WhereToGroup(this EcsReadonlyGroup group, EcsStaticMask mask)
        {
            return group.ToSpan().WhereToGroup(mask);
        }
        public static EcsReadonlyGroup WhereToGroup(this EcsSpan span, EcsStaticMask mask)
        {
            EcsWorld world = span.World;
            var executor = world.GetExecutor<EcsWhereToGroupExecutor>(mask);
            return executor.ExecuteFor(span);
        }

        public static EcsReadonlyGroup WhereToGroup<TCollection>(this TCollection entities, EcsMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(mask);
        }
        public static EcsReadonlyGroup WhereToGroup(this EcsReadonlyGroup group, EcsMask mask)
        {
            return group.ToSpan().WhereToGroup(mask);
        }
        public static EcsReadonlyGroup WhereToGroup(this EcsSpan span, EcsMask mask)
        {
            EcsWorld world = span.World;
            var executor = world.GetExecutor<EcsWhereToGroupExecutor>(mask);
            return executor.ExecuteFor(span);
        }
        #endregion
    }
}
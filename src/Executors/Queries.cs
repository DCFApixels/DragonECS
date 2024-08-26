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
            var executor = world.GetExecutor<EcsWhereExecutor<TAspect>>();
            aspect = executor.Aspect;
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
            var executor = world.GetExecutor<EcsWhereExecutor<TAspect>>();
            aspect = executor.Aspect;
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
            var executor = world.GetExecutor<EcsWhereToGroupExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(span);
        }
        #endregion
    }
}

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
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
                return executor.Execute();
            }
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
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        public static EcsSpan Where<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                var executor = entities.World.GetExecutorForMask<EcsWhereExecutor>(mask);
                return executor.Execute();
            }
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
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
                return executor.Execute(comparison);
            }
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
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span, comparison);
        }

        public static EcsSpan Where<TCollection>(this TCollection entities, IComponentMask mask, Comparison<int> comparison)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                EcsWhereExecutor executor = entities.World.GetExecutorForMask<EcsWhereExecutor>(mask);
                return executor.Execute(comparison);
            }
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
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out EcsWhereToGroupExecutor executor, out aspect);
                return executor.Execute();
            }
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
            span.World.GetQueryCache(out EcsWhereToGroupExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        public static EcsReadonlyGroup WhereToGroup<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                EcsWhereToGroupExecutor executor = entities.World.GetExecutorForMask<EcsWhereToGroupExecutor>(mask);
                return executor.Execute();
            }
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
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
            where TAspect : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect);
        }
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : EcsAspect
        {
            return group.ToSpan().Where(out aspect);
        }
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : EcsAspect
        {
            EcsWorld world = span.World;
            if (world.IsEnableReleaseDelEntBuffer)
            {
                world.ReleaseDelEntityBufferAll();
            }
            var executor = world.GetExecutor<EcsWhereToGroupExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(span);
        }


        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities)
            where TAspect : EcsAspect
            where TCollection : IEntitiesCollection
        {
            return entities.ToSpan().Where<TAspect>();
        }
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group)
            where TAspect : EcsAspect
        {
            return group.ToSpan().Where<TAspect>();
        }
        public static EcsSpan Where<TAspect>(this EcsSpan span)
            where TAspect : EcsAspect
        {
            EcsWorld world = span.World;
            if (world.IsEnableReleaseDelEntBuffer)
            {
                world.ReleaseDelEntityBufferAll();
            }
            return world.GetExecutor<EcsWhereToGroupExecutor<TAspect>>().ExecuteFor(span);
        }
        #endregion

        #region WhereToGroup
        public static EcsReadonlyGroup WhereToGroup<TCollection ,TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out aspect);
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : EcsAspect
        {
            return group.ToSpan().WhereToGroup(out aspect);
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsSpan span, out TAspect aspect) 
            where TAspect : EcsAspect
        {
            EcsWorld world = span.World;
            if (world.IsEnableReleaseDelEntBuffer)
            {
                world.ReleaseDelEntityBufferAll();
            }
            var executor = world.GetExecutor<EcsWhereToGroupExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(span);
        }


        public static EcsReadonlyGroup WhereToGroup<TCollection, TAspect>(this TCollection entities)
            where TAspect : EcsAspect
            where TCollection : IEntitiesCollection
        {
            return entities.ToSpan().WhereToGroup<TAspect>();
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsReadonlyGroup group)
            where TAspect : EcsAspect
        {
            return group.ToSpan().WhereToGroup<TAspect>();
        }
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsSpan span) 
            where TAspect : EcsAspect
        {
            EcsWorld world = span.World;
            if (world.IsEnableReleaseDelEntBuffer)
            {
                world.ReleaseDelEntityBufferAll();
            }
            return world.GetExecutor<EcsWhereToGroupExecutor<TAspect>>().ExecuteFor(span);
        }
        #endregion
    }
}

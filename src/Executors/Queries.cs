#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
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

        /// <summary>
        /// Filters entities from the storage using the specified aspect and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered entity IDs.</returns>
        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified aspect and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered entity IDs.</returns>
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : new()
        {
            return group.ToSpan().Where(out aspect);
        }

        /// <summary>
        /// Filters entities from the span using the specified aspect and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered entity IDs.</returns>
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        /// <summary>
        /// Filters entities from the storage using the specified component mask and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered entity IDs.</returns>
        public static EcsSpan Where<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified component mask and returns a span of matching entity IDs.
        /// </summary>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered entity IDs.</returns>
        public static EcsSpan Where(this EcsReadonlyGroup group, IComponentMask mask)
        {
            return group.ToSpan().Where(mask);
        }

        /// <summary>
        /// Filters entities from the span using the specified component mask and returns a span of matching entity IDs.
        /// </summary>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered entity IDs.</returns>
        public static EcsSpan Where(this EcsSpan span, IComponentMask mask)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }

        #endregion

        #region Where with sort

        /// <summary>
        /// Filters entities from the storage using the specified aspect, sorts the result using the provided comparison,
        /// and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsSpan Where<TCollection, TAspect>(this TCollection entities, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out aspect, comparison);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified aspect, sorts the result using the provided comparison,
        /// and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsSpan Where<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
        {
            return group.ToSpan().Where(out aspect, comparison);
        }

        /// <summary>
        /// Filters entities from the span using the specified aspect, sorts the result using the provided comparison,
        /// and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsSpan Where<TAspect>(this EcsSpan span, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span, comparison);
        }

        /// <summary>
        /// Filters entities from the storage using the specified component mask, sorts the result using the provided comparison,
        /// and returns a span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsSpan Where<TCollection>(this TCollection entities, IComponentMask mask, Comparison<int> comparison)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(mask, comparison);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified component mask, sorts the result using the provided comparison,
        /// and returns a span of matching entity IDs.
        /// </summary>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsSpan Where(this EcsReadonlyGroup group, IComponentMask mask, Comparison<int> comparison)
        {
            return group.ToSpan().Where(mask, comparison);
        }

        /// <summary>
        /// Filters entities from the span using the specified component mask, sorts the result using the provided comparison,
        /// and returns a span of matching entity IDs.
        /// </summary>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsSpan Where(this EcsSpan span, IComponentMask mask, Comparison<int> comparison)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span, comparison);
        }

        #endregion

        #region WhereUnsafe

        /// <summary>
        /// Filters entities from the storage using the specified aspect and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereUnsafe(out aspect);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified aspect and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : new()
        {
            return group.ToSpan().WhereUnsafe(out aspect);
        }

        /// <summary>
        /// Filters entities from the span using the specified aspect and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        /// <summary>
        /// Filters entities from the storage using the specified component mask and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereUnsafe(mask);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified component mask and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe(this EcsReadonlyGroup group, IComponentMask mask)
        {
            return group.ToSpan().WhereUnsafe(mask);
        }

        /// <summary>
        /// Filters entities from the span using the specified component mask and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe(this EcsSpan span, IComponentMask mask)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span);
        }

        #endregion

        #region WhereUnsafe with sort

        /// <summary>
        /// Filters entities from the storage using the specified aspect, sorts the result using the provided comparison,
        /// and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TCollection, TAspect>(this TCollection entities, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereUnsafe(out aspect, comparison);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified aspect, sorts the result using the provided comparison,
        /// and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
        {
            return group.ToSpan().WhereUnsafe(out aspect, comparison);
        }

        /// <summary>
        /// Filters entities from the span using the specified aspect, sorts the result using the provided comparison,
        /// and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TAspect>(this EcsSpan span, out TAspect aspect, Comparison<int> comparison)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereExecutor executor, out aspect);
            return executor.ExecuteFor(span, comparison);
        }

        /// <summary>
        /// Filters entities from the storage using the specified component mask, sorts the result using the provided comparison,
        /// and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe<TCollection>(this TCollection entities, IComponentMask mask, Comparison<int> comparison)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereUnsafe(mask, comparison);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified component mask, sorts the result using the provided comparison,
        /// and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe(this EcsReadonlyGroup group, IComponentMask mask, Comparison<int> comparison)
        {
            return group.ToSpan().WhereUnsafe(mask, comparison);
        }

        /// <summary>
        /// Filters entities from the span using the specified component mask, sorts the result using the provided comparison,
        /// and returns an unsafe span of matching entity IDs.
        /// </summary>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <param name="comparison">The comparison delegate used to order the resulting entity IDs.</param>
        /// <returns>An <see cref="EcsUnsafeSpan"/> containing the filtered and sorted entity IDs.</returns>
        public static EcsUnsafeSpan WhereUnsafe(this EcsSpan span, IComponentMask mask, Comparison<int> comparison)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereExecutor>(mask);
            return executor.ExecuteFor(span, comparison);
        }

        #endregion

        #region WhereToGroup

        /// <summary>
        /// Filters entities from the storage using the specified aspect and returns a read‑only group of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsReadonlyGroup"/> containing the filtered entity IDs.</returns>
        public static EcsReadonlyGroup WhereToGroup<TCollection, TAspect>(this TCollection entities, out TAspect aspect)
            where TAspect : new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out aspect);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified aspect and returns a read‑only group of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsReadonlyGroup"/> containing the filtered entity IDs.</returns>
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsReadonlyGroup group, out TAspect aspect)
            where TAspect : new()
        {
            return group.ToSpan().WhereToGroup(out aspect);
        }

        /// <summary>
        /// Filters entities from the span using the specified aspect and returns a read‑only group of matching entity IDs.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have a parameterless constructor).</typeparam>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="aspect">Output aspect instance (created internally).</param>
        /// <returns>An <see cref="EcsReadonlyGroup"/> containing the filtered entity IDs.</returns>
        public static EcsReadonlyGroup WhereToGroup<TAspect>(this EcsSpan span, out TAspect aspect)
            where TAspect : new()
        {
            span.World.GetQueryCache(out EcsWhereToGroupExecutor executor, out aspect);
            return executor.ExecuteFor(span);
        }

        /// <summary>
        /// Filters entities from the storage using the specified component mask and returns a read‑only group of matching entity IDs.
        /// </summary>
        /// <typeparam name="TCollection">Type of the entity storage (must implement <see cref="IEntityStorage"/>).</typeparam>
        /// <param name="entities">The entity storage to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsReadonlyGroup"/> containing the filtered entity IDs.</returns>
        public static EcsReadonlyGroup WhereToGroup<TCollection>(this TCollection entities, IComponentMask mask)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(mask);
        }

        /// <summary>
        /// Filters entities from the read‑only group using the specified component mask and returns a read‑only group of matching entity IDs.
        /// </summary>
        /// <param name="group">The read‑only group to query.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsReadonlyGroup"/> containing the filtered entity IDs.</returns>
        public static EcsReadonlyGroup WhereToGroup(this EcsReadonlyGroup group, IComponentMask mask)
        {
            return group.ToSpan().WhereToGroup(mask);
        }

        /// <summary>
        /// Filters entities from the span using the specified component mask and returns a read‑only group of matching entity IDs.
        /// </summary>
        /// <param name="span">The span of entity IDs to filter.</param>
        /// <param name="mask">The component mask to filter by.</param>
        /// <returns>An <see cref="EcsReadonlyGroup"/> containing the filtered entity IDs.</returns>
        public static EcsReadonlyGroup WhereToGroup(this EcsSpan span, IComponentMask mask)
        {
            var executor = span.World.GetExecutorForMask<EcsWhereToGroupExecutor>(mask);
            return executor.ExecuteFor(span);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public abstract class EcsQueryBase
    {
        internal EcsWorld source;
        internal EcsGroup groupFilter;
        internal EcsQueryMask mask;

        #region Properties
        internal EcsQueryMask Mask => mask;
        public EcsWorld World => source;
        #endregion

        #region Builder
        protected virtual void Init(Builder b) { }
        protected abstract void OnBuild(Builder b);
        public abstract void ExecuteWhere();
        public sealed class Builder : EcsQueryBuilderBase
        {
            private EcsWorld _world;
            private List<int> _inc;
            private List<int> _exc;

            public EcsWorld World => _world;

            private Builder(EcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }
            internal static TQuery Build<TQuery>(EcsWorld world) where TQuery : EcsQueryBase
            {
                Builder builder = new Builder(world);
                Type queryType = typeof(TQuery);
                ConstructorInfo constructorInfo = queryType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                EcsQueryBase newQuery;
                if (constructorInfo != null)
                {
                    newQuery = (EcsQueryBase)constructorInfo.Invoke(new object[] { builder });
                }
                else
                {
                    newQuery = (EcsQueryBase)Activator.CreateInstance(typeof(TQuery));
                    newQuery.Init(builder);
                }
                newQuery.groupFilter = EcsGroup.New(world);
                newQuery.source = world;
                newQuery.OnBuild(builder);
                builder.End(out newQuery.mask);
                return (TQuery)(object)newQuery;
            }

            public sealed override TPool Include<TComponent, TPool>()
            {
                _inc.Add(_world.GetComponentID<TComponent>());
                return _world.GetPool<TComponent, TPool>();
            }
            public sealed override TPool Exclude<TComponent, TPool>()
            {
                _exc.Add(_world.GetComponentID<TComponent>());
                return _world.GetPool<TComponent, TPool>();
            }
            public sealed override TPool Optional<TComponent, TPool>()
            {
                return _world.GetPool<TComponent, TPool>();
            }

            private void End(out EcsQueryMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsQueryMask(_world.Archetype, _inc.ToArray(), _exc.ToArray());
                _world = null;
                _inc = null;
                _exc = null;
            }
        }
        #endregion

        protected void ExecuteWhere(EcsReadonlyGroup group, EcsGroup result)
        {
            var pools = World.Pools;
            result.Clear();
            foreach (var e in group)
            {
                for (int i = 0, iMax = mask.Inc.Length; i < iMax; i++)
                {
                    if (!pools[mask.Inc[i]].Has(e))
                        goto next;
                }
                for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
                {
                    if (pools[mask.Exc[i]].Has(e))
                        goto next;
                }
                result.AggressiveAdd(e);
                next: continue;
            }
        }
        protected void ExecuteWhereAndSort(EcsReadonlyGroup group, EcsGroup result)
        {
            ExecuteWhere(group, result);
            result.Sort();
        }
    }

    public abstract class EcsQuery : EcsQueryBase
    {
        private ProfilerMarker _execute = new ProfilerMarker("EcsQuery.ExecuteWhere");
        protected sealed override void OnBuild(Builder b) { }
        public sealed override void ExecuteWhere()
        {
            using (_execute.Auto())
            {
                ExecuteWhereAndSort(World.Entities, groupFilter);
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
        }
    }

    public class EcsQueryMask : EcsComponentMask
    {
        public EcsQueryMask(Type worldArchetypeType, int[] inc, int[] exc)
        {
            WorldArchetype = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }
    public abstract class EcsQueryBuilderBase
    {
        public abstract TPool Include<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
        public abstract TPool Exclude<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
        public abstract TPool Optional<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
    }
}

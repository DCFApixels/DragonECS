using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public abstract class EcsQueryBase
    {
        internal IEcsWorld source;
        internal EcsGroup groupFilter;
        internal EcsQueryMask mask;
        public IEcsWorld World => source;

        #region Builder
        protected virtual void Init(Builder b) { }
        protected abstract void OnBuilt();
        public abstract void Execute();
        public sealed class Builder : EcsQueryBuilderBase
        {
            private IEcsWorld _world;
            private List<int> _inc;
            private List<int> _exc;

            private Builder(IEcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }
            internal static TQuery Build<TQuery>(IEcsWorld world) where TQuery : EcsQueryBase
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
                builder.End(out newQuery.mask);
                newQuery.OnBuilt();
                return (TQuery)(object)newQuery;
            }

            public override inc_<TComponent> Include<TComponent>() where TComponent : struct
            {
                _inc.Add(_world.GetComponentID<TComponent>());
                return new inc_<TComponent>(_world.GetPool<TComponent>());
            }
            public override exc_<TComponent> Exclude<TComponent>() where TComponent : struct
            {
                _exc.Add(_world.GetComponentID<TComponent>());
                return new exc_<TComponent>(_world.GetPool<TComponent>());
            }
            public override opt_<TComponent> Optional<TComponent>() where TComponent : struct
            {
                return new opt_<TComponent>(_world.GetPool<TComponent>());
            }

            private void End(out EcsQueryMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsQueryMask(_world.ArchetypeType, _inc.ToArray(), _exc.ToArray());
                _world = null;
                _inc = null;
                _exc = null;
            }
        }
        #endregion
    }

    public abstract class EcsJoinQuery : EcsQueryBase
    {
        private EcsPool<Attach> attachPool;

        private ProfilerMarker _getEnumerator = new ProfilerMarker("EcsQuery.Execute");
        protected sealed override void OnBuilt()
        {
            attachPool = World.GetPool<Attach>();
        }
        public sealed override void Execute()
        {
            using (_getEnumerator.Auto())
            {
                throw new NotImplementedException();
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
        } 
    }

    public abstract class EcsQuery : EcsQueryBase
    {
        private ProfilerMarker _getEnumerator = new ProfilerMarker("EcsQuery.Execute");
        protected sealed override void OnBuilt() { }
        public sealed override void Execute()
        {
            using (_getEnumerator.Auto())
            {
                var pools = World.GetAllPools();

                EcsReadonlyGroup all = World.Entities;
                groupFilter.Clear();
                foreach (var e in all)
                {
                    int entityID = e.id;
                    for (int i = 0, iMax = mask.Inc.Length; i < iMax; i++)
                    {
                        if (!pools[mask.Inc[i]].Has(entityID))
                            continue;
                    }
                    for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
                    {
                        if (pools[mask.Exc[i]].Has(entityID))
                            continue;
                    }
                    groupFilter.AggressiveAdd(entityID);
                }
                groupFilter.Sort();
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
            WorldArchetypeType = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }
    public abstract class EcsQueryBuilderBase
    {
        public abstract inc_<TComponent> Include<TComponent>() where TComponent : struct;
        public abstract exc_<TComponent> Exclude<TComponent>() where TComponent : struct;
        public abstract opt_<TComponent> Optional<TComponent>() where TComponent : struct;
    }
}

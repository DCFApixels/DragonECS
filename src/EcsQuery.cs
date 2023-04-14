using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace DCFApixels.DragonECS
{
    public abstract class EcsQuery
    {
        internal EcsGroup groupFilter;
        internal EcsQueryMask mask;
        public IEcsWorld World => groupFilter.World;

        private ProfilerMarker _getEnumerator = new ProfilerMarker("EcsQuery.GetEnumerator");


        public EcsGroup.Enumerator GetEnumerator()
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
                        {
                            continue;
                        }
                    }
                    for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
                    {
                        if (pools[mask.Exc[i]].Has(entityID))
                        {
                            continue;
                        }
                    }
                    groupFilter.AggressiveAdd(entityID);
                }
                groupFilter.Sort();
                return groupFilter.GetEnumerator();
            }
        }
        protected virtual void Init(Builder b) { }

        #region Builder
        public sealed class Builder : EcsQueryBuilder
        {
            private IEcsWorld _world;
            private List<int> _inc;
            private List<int> _exc;

            internal static TQuery Build<TQuery>(IEcsWorld world) where TQuery : EcsQuery
            {
                Builder builder = new Builder(world);

                Type queryType = typeof(TQuery);
                ConstructorInfo constructorInfo = queryType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                EcsQuery newQuery;
                if (constructorInfo != null)
                {
                    newQuery = (EcsQuery)constructorInfo.Invoke(new object[] { builder });
                }
                else
                {
                    newQuery = (EcsQuery)Activator.CreateInstance(typeof(TQuery));
                    newQuery.Init(builder);
                }

                builder.End(out newQuery.mask);
                // newQuery.groupFilter = new EcsGroup(world);
                newQuery.groupFilter = EcsGroup.New(world);
                return (TQuery)(object)newQuery;
            }

            private Builder(IEcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }

            public override inc<TComponent> Include<TComponent>() where TComponent : struct
            {
                _inc.Add(_world.GetComponentID<TComponent>());
                return new inc<TComponent>(_world.GetPool<TComponent>());
            }
            public override exc<TComponent> Exclude<TComponent>() where TComponent : struct
            {
                _exc.Add(_world.GetComponentID<TComponent>());
                return new exc<TComponent>(_world.GetPool<TComponent>());
            }
            public override opt<TComponent> Optional<TComponent>() where TComponent : struct
            {
                return new opt<TComponent>(_world.GetPool<TComponent>());
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

    public class EcsQueryMask : EcsComponentMask
    {
        public EcsQueryMask(Type worldArchetypeType, int[] inc, int[] exc)
        {
            WorldArchetypeType = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }
    public abstract class EcsQueryBuilder
    {
        public abstract inc<TComponent> Include<TComponent>() where TComponent : struct;
        public abstract exc<TComponent> Exclude<TComponent>() where TComponent : struct;
        public abstract opt<TComponent> Optional<TComponent>() where TComponent : struct;
    }
}

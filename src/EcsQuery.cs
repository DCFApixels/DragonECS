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
                // groupFilter.Clear();
                 var pools = World.GetAllPools();
                //
                // if (mask.Inc.Length > 0)
                // {
                //     groupFilter.CopyFrom(pools[mask.Inc[0]].entities);
                //     for (int i = 1; i < mask.Inc.Length; i++)
                //     {
                //         groupFilter.AndWith(pools[mask.Inc[i]].entities);
                //     }
                // }
                // else
                // {
                //     groupFilter.CopyFrom(World.Entities);
                // }
                // for (int i = 0; i < mask.Exc.Length; i++)
                // {
                //     groupFilter.RemoveGroup(pools[mask.Exc[i]].entities);
                // }
                //
                // groupFilter.Sort();
                // return groupFilter.GetEnumerator();
                //
                EcsReadonlyGroup sum = World.Entities;
                for (int i = 0; i < mask.Inc.Length; i++)
                {
                    sum = EcsGroup.And(sum.GetGroupInternal(), pools[mask.Inc[i]].entities);
                   // Debug.Log("inc " + sum.ToString());
                }
                for (int i = 0; i < mask.Exc.Length; i++)
                {
                    sum = EcsGroup.Remove(sum.GetGroupInternal(), pools[mask.Exc[i]].entities);
                   // Debug.Log("exc " + sum.ToString());
                }
                //sum.GetGroupInternal().Sort();
                return sum.GetEnumerator();
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
                newQuery.groupFilter = new EcsGroup(world);
                return (TQuery)(object)newQuery;
            }

            private Builder(IEcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }

            #region Init query member methods
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
            #endregion

            private void End(out EcsQueryMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsQueryMask(_world.ArchetypeType, _inc.ToArray(), _exc.ToArray());

                _world = null;
                _inc.Clear();
                _inc = null;
                _exc.Clear();
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

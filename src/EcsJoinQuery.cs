using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public abstract class EcsJoinQueryBase : EcsQueryBase
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteWhere()
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
                        goto next;
                }
                for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
                {
                    if (pools[mask.Exc[i]].Has(entityID))
                        goto next;
                }
                groupFilter.AggressiveAdd(entityID);
                next: continue;
            }
            groupFilter.Sort();
        }
    }
    public abstract class EcsJoinAttachQuery<TAttachComponent> : EcsJoinQueryBase
        where TAttachComponent : struct, IEcsAttachComponent
    {
        private EcsWorld _targetWorld;
        private EcsAttachPool<TAttachComponent> _targetPool;

        private ProfilerMarker _execute = new ProfilerMarker("EcsJoinAttachQuery.Execute");
        protected sealed override void OnBuild(Builder b)
        {
            _targetPool = b.Include<TAttachComponent>();
        }
        public sealed override void Execute()
        {
            using (_execute.Auto())
            {
                ExecuteWhere();
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
        }
    }
    public abstract class EcsJoinRelationQuery<TRelationComponent> : EcsJoinQueryBase
        where TRelationComponent : struct, IEcsRelationComponent
    {
        private EcsWorld _firstWorld;
        private EcsWorld _secondWorld;
        private EcsRelationPool<TRelationComponent> _targetPool;

        private ProfilerMarker _execute = new ProfilerMarker("EcsJoinRelationQuery.Execute");
        protected sealed override void OnBuild(Builder b)
        {
            _targetPool = source.GetPool<TRelationComponent>();
        }
        public sealed override void Execute()
        {
            using (_execute.Auto())
            {
                ExecuteWhere();
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
        }
    }
}

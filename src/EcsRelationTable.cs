using DCFApixels.DragonECS;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
  /*  public interface IEcsRealationTable : IEcsReadonlyTable
    {
        public EcsFilter Relations<TComponent>() where TComponent : struct;
  rr
    }
    public sealed class EcsRelationTable<TWorldArchetype> : IEcsRealationTable
         where TWorldArchetype : EcsRelationTableArchetypeBase
    {
        public readonly IEcsWorld leftWorld;
        public readonly IEcsWorld rightWorld;

        private int[] _relations; //dense
        private int[] _leftMapping;
        private int[] _rgihtMapping;

        private int _relationsCount;

        private IEcsPool[] _pools;
        private EcsNullPool _nullPool;

        #region Properties
        public Type ArchetypeType => typeof(TWorldArchetype);
        public int EntitesCount => _relationsCount;
        public int EntitesCapacity => _relations.Length;
        #endregion

        #region Constructors
        internal EcsRelationTable(IEcsWorld leftWorld, IEcsWorld rightWorld)
        {
            this.leftWorld = leftWorld;
            this.rightWorld = rightWorld;

            _relations = new int[512];
            _leftMapping = new int[512];
            _rgihtMapping = new int[512];

            _relationsCount = 0;
        }
        #endregion

        #region RealtionControls
        public void AddRelation(int leftEnttiyID, int rightEntityID)
        {

        }
        public void RemoveRelationLeft(int entityID)
        {

        }
        public void RemoveRelationRight(int entityID)
        {

        }
        #endregion

        public ReadOnlySpan<IEcsPool> GetAllPools()
        {
            throw new NotImplementedException();
        }

        public int GetComponentID<T>()
        {
            throw new NotImplementedException();
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            throw new NotImplementedException();
        }

        public EcsPool<T> UncheckedGetPool<T>() where T : struct
        {
            throw new NotImplementedException();
        }

        public EcsFilter Entities<TComponent>() where TComponent : struct
        {
            throw new NotImplementedException();
        }

        public EcsFilter Filter<TInc>() where TInc : struct, IInc
        {
            throw new NotImplementedException();
        }

        public EcsFilter Filter<TInc, TExc>()
            where TInc : struct, IInc
            where TExc : struct, IExc
        {
            throw new NotImplementedException();
        }

        public bool IsMaskCompatible<TInc>(int entity) where TInc : struct, IInc
        {
            throw new NotImplementedException();
        }

        public bool IsMaskCompatible<TInc, TExc>(int entity)
            where TInc : struct, IInc
            where TExc : struct, IExc
        {
            throw new NotImplementedException();
        }

        public bool IsMaskCompatible(EcsMask mask, int entity)
        {
            throw new NotImplementedException();
        }

        public bool IsMaskCompatibleWithout(EcsMask mask, int entity, int otherPoolID)
        {
            throw new NotImplementedException();
        }

        void IEcsReadonlyTable.OnEntityComponentAdded(int entityID, int changedPoolID)
        {
            throw new NotImplementedException();
        }

        void IEcsReadonlyTable.OnEntityComponentRemoved(int entityID, int changedPoolID)
        {
            throw new NotImplementedException();
        }

        void IEcsReadonlyTable.RegisterGroup(EcsGroup group)
        {
            throw new NotImplementedException();
        }
    }*/
}

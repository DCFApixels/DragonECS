using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsTable
    {
        private IEcsPool[] _pools;
        private EcsNullPool _nullPool;

        private short[] _gens;
        private short[] _componentCounts;

        private int _entitiesCount;

        private List<EcsQueryBase>[] _filtersByIncludedComponents;
        private List<EcsQueryBase>[] _filtersByExcludedComponents;

        private EcsQueryBase[] _queries;

        private List<EcsGroup> _groups;


        #region Properties
        public int Count => _entitiesCount;
        public int Capacity => _gens.Length;
        public ReadOnlySpan<IEcsPool> GetAllPools() => new ReadOnlySpan<IEcsPool>(_pools);
        #endregion

        #region internal Add/Has/Remove
        internal void Add(int entityID)
        {
            int entity;
            if (_entitiesCount >= _gens.Length)
            {
                Array.Resize(ref _gens, _gens.Length << 1);
                Array.Resize(ref _componentCounts, _componentCounts.Length << 1);
            }
            _gens[_entitiesCount++]++;
            _componentCounts[_entitiesCount++] = 0;

            // if (_gens.Length <= entityID)
            // {
            //     //TODO есть проблема что если передать слишком большой id такой алогоритм не сработает
            // }


        }
        internal void Has(int entityID)
        {

        }
        internal void Remove(int entityID)
        {

        }
        #endregion

        //public int GetComponentID<T>() => ;
    }


}

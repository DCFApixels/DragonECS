using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsTable
    {
        private IEcsPool[] _pools;
        private EcsNullPool _nullPool;

        private int[] _denseEntities;
        private int[] _sparceEntities;
        private int _entitiesCount;

        private List<EcsQueryBase>[] _filtersByIncludedComponents;
        private List<EcsQueryBase>[] _filtersByExcludedComponents;

        private EcsQueryBase[] _queries;

        private List<EcsGroup> _groups;

        #region Internal Properties
        public int Count => _entitiesCount;
        public int Capacity => _denseEntities.Length;
        #endregion

        public ReadOnlySpan<IEcsPool> GetAllPools() => new ReadOnlySpan<IEcsPool>(_pools);
        //public int GetComponentID<T>() => ;
    }


}

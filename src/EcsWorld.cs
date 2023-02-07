using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsWorld
    {
        public const int MAX_WORLDS = byte.MaxValue; //Номер последнего мира 254
        public const int DEAD_WORLD_ID = byte.MaxValue; //Зарезервированный номер мира для мертвых сущьностей

        private byte _id = DEAD_WORLD_ID;
        
        private Dictionary<Type, IEcsPool> _pools;
        private SparseSet _entities = new SparseSet();
        private short[] _gens;

        private List<EcsFilter>[] _filtersByIncludedComponents;
        private List<EcsFilter>[] _filtersByExcludedComponents;

        //private Dictionary<Type, IEcsEntityTable> _tables;

        #region Properties
        public int ID => _id;
        public bool IsAlive => _id != DEAD_WORLD_ID;
        #endregion

        #region Constructors
        public EcsWorld()
        {
            _pools = new Dictionary<Type, IEcsPool>();
            _entities = new SparseSet();
        }
        #endregion

        #region ID
        internal void SetId(byte id)
        {
            _id = id;
        }
        #endregion

        #region GetPool
        public EcsPool<T> GetPool<T>()
        {
            Type type = typeof(T);
            if (_pools.TryGetValue(type, out IEcsPool pool))
            {
                return (EcsPool<T>)pool;
            }

            //pool = new EcsPool<T>();
            _pools.Add(type, pool);
            return (EcsPool<T>)pool;
        }
        #endregion

        #region NewEntity
        public ent NewEntity()
        {
            int entityID = _entities.GetFree();
            _entities.Normalize(ref _gens);
            _gens[entityID]++;


            return new ent(entityID, _gens[entityID], _id);
        }
        #endregion

        #region Destroy
        public void Destroy()
        {
            _id = DEAD_WORLD_ID;
        }
        #endregion

        internal void OnEntityFieldAdd(int entityID, EcsType chaangedField)
        {

        }


        internal void OnEntityFieldDel(int entityID, EcsType chaangedField)
        {
            
        }


        public class Mask
        {

        }
    }
}

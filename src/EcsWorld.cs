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
        
        private Dictionary<Type, IEcsFieldPool> _pools;
        private SparseSet _entities = new SparseSet();
        private short[] _gens;
        private byte[] _components;

        //private Dictionary<Type, IEcsEntityTable> _tables;

        #region Properties
        public int ID => _id;
        public bool IsAlive => _id != DEAD_WORLD_ID;
        #endregion

        #region Constructors
        public EcsWorld()
        {
            _pools = new Dictionary<Type, IEcsFieldPool>();
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
        public EcsFieldPool<T> GetPool<T>()
        {
            Type type = typeof(T);
            if (_pools.TryGetValue(type, out IEcsFieldPool pool))
            {
                return (EcsFieldPool<T>)pool;
            }

            //pool = new EcsPool<T>();
            _pools.Add(type, pool);
            return (EcsFieldPool<T>)pool;
        }
        #endregion

        #region NewEntity
        public ent NewEntity()
        {
            int entityID = _entities.GetFree();
            _entities.Normalize(ref _gens);
            _entities.Normalize(ref _components);
            _gens[entityID]++;


            return new ent(entityID, _gens[entityID], _id, _components[entityID]);
        }
        #endregion

        #region Destroy
        public void Destroy()
        {
            _id = DEAD_WORLD_ID;
        }
        #endregion

        private void Resize()
        {

        }
    }
}

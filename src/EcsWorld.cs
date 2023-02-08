using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsWorld
    {
        public const int MAX_WORLDS = byte.MaxValue; //Номер последнего мира 254
        public const int DEAD_WORLD_ID = byte.MaxValue; //Зарезервированный номер мира для мертвых сущьностей

        private byte _id = DEAD_WORLD_ID;
        
        private IEcsPool[] _pools;
        private SparseSet _memToPoolIDSet;


        private SparseSet _entities = new SparseSet();
        private short[] _gens;

        private List<EcsFilter>[] _filtersByIncludedComponents;
        private List<EcsFilter>[] _filtersByExcludedComponents;

        #region Properties
        public int ID => _id;
        public bool IsAlive => _id != DEAD_WORLD_ID;
        public bool IsEmpty => _entities.Count < 0;
        #endregion

        #region Constructors
        public EcsWorld()
        {
            _pools = new IEcsPool[512];
            _entities = new SparseSet();
            _memToPoolIDSet = new SparseSet(512);
        }
        #endregion

        #region ID
        internal void SetId(byte id)
        {
            _id = id;
        }
        #endregion

        #region GetPool
        public EcsPool<T> GetPool<T>(mem<T> member)
            where T : struct
        {
            if(_memToPoolIDSet.Contains(member.uniqueID))

            if (_pools.TryGetValue(type, out IEcsPool pool))
            {
                return (EcsPool<T>)pool;
            }

            pool = new EcsPool<T>(this, member, 512);//TODO сделать чтоб объем можно было указывать через конфиг
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

        internal void OnEntityFieldAdd(int entityID, int changedPool)
        {

        }


        internal void OnEntityFieldDel(int entityID, int changedPool)
        {
            
        }


        public class Mask
        {
            private readonly EcsWorld _world;
            internal int[] include;
            internal int[] exclude;
            internal int includeCount;
            internal int excludeCount;
            internal int hash;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
            bool _built;
#endif

            internal Mask(EcsWorld world)
            {
                _world = world;
                include = new int[8];
                exclude = new int[2];
                Reset();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Reset()
            {
                includeCount = 0;
                excludeCount = 0;
                hash = 0;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
                _built = false;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mask Inc<T>(mem<T> member) where T : struct
            {
                var poolId = _world.GetPool(member).ID;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                if (Array.IndexOf(include, poolId, 0, includeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
                if (Array.IndexOf(exclude, poolId, 0, excludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
#endif
                if (includeCount == include.Length) { Array.Resize(ref include, includeCount << 1); }
                include[includeCount++] = poolId;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mask Exc<T>(mem<T> member) where T : struct
            {
                var poolId = _world.GetPool(member).ID;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                if (Array.IndexOf(include, poolId, 0, includeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
                if (Array.IndexOf(exclude, poolId, 0, excludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
#endif
                if (excludeCount == exclude.Length) { Array.Resize(ref exclude, excludeCount << 1); }
                exclude[excludeCount++] = poolId;
                return this;
            }
        }
    }
}

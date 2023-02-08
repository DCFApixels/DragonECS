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
        public EcsPool<T> GetPool<T>(mem<T> member)
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

        internal void OnEntityFieldAdd(int entityID, mem<T> chaangedField)
        {

        }


        internal void OnEntityFieldDel(int entityID, EcsMember chaangedField)
        {
            
        }


        public class Mask
        {
            private readonly EcsWorld _world;
            internal int[] Include;
            internal int[] Exclude;
            internal int IncludeCount;
            internal int ExcludeCount;
            internal int Hash;
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            bool _built;
#endif

            internal Mask(EcsWorld world)
            {
                _world = world;
                Include = new int[8];
                Exclude = new int[2];
                Reset();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Reset()
            {
                IncludeCount = 0;
                ExcludeCount = 0;
                Hash = 0;
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                _built = false;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mask Inc<T>(EcsMember member) where T : struct
            {
                var poolId = _world.GetPool<T>().GetId();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                if (Array.IndexOf(Include, poolId, 0, IncludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
                if (Array.IndexOf(Exclude, poolId, 0, ExcludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
#endif
                if (IncludeCount == Include.Length) { Array.Resize(ref Include, IncludeCount << 1); }
                Include[IncludeCount++] = poolId;
                return this;
            }

#if UNITY_2020_3_OR_NEWER
            [UnityEngine.Scripting.Preserve]
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mask Exc<T>(EcsMember member) where T : struct
            {
                var poolId = _world.GetPool<T>().GetId();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                if (Array.IndexOf(Include, poolId, 0, IncludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
                if (Array.IndexOf(Exclude, poolId, 0, ExcludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
#endif
                if (ExcludeCount == Exclude.Length) { Array.Resize(ref Exclude, ExcludeCount << 1); }
                Exclude[ExcludeCount++] = poolId;
                return this;
            }
        }
    }
}

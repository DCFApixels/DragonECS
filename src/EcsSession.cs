using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public class EcsWorldMap
    {
        private Dictionary<(Type, string), IEcsWorld> _worlds = new Dictionary<(Type, string), IEcsWorld>(8);
        private bool _built = false;

        public void Add<TArchetype>(EcsWorld<TArchetype> world, string name = "")
            where TArchetype : IWorldArchetype
        {
            if(_built) { throw new Exception($"Cant change built {nameof(EcsWorldMap)}"); }
            _worlds.Add((typeof(TArchetype), name), world);
        }

        public EcsWorld<TArchetype> Get<TArchetype>(string name ="")
            where TArchetype : IWorldArchetype
        {
            return (EcsWorld<TArchetype>)_worlds[(typeof(TArchetype), name)];
        }

        public IEcsWorld Get(Type type, string name = "")
        {
            return _worlds[(type, name)];
        }

        public void Build()
        {
            _built = true;
        }
    }

    public class EcsSession
    {
        private int _id;


        private readonly List<IEcsProcessor> _allSystems;
        private ReadOnlyCollection<IEcsProcessor> _allSystemsSealed;

        private bool _isInit = false;
        private bool _isDestoryed = false;


        private readonly Dictionary<Type, IEcsRunner> _runners;
        private IEcsRunSystem _runRunnerCache;

        private readonly EcsWorldMap _worldMap;

        #region Properties
        public ReadOnlyCollection<IEcsProcessor> AllProcessors => _allSystemsSealed;

        #endregion

        public EcsSession()
        {
            _allSystems = new List<IEcsProcessor>(128);
            _runners = new Dictionary<Type, IEcsRunner>();
            _worldMap = new EcsWorldMap();
        }

        #region React Runners/Messengers
        public T GetRunner<T>() where T : IEcsProcessor
        {
            Type type = typeof(T);
            if (_runners.TryGetValue(type, out IEcsRunner result))
            {
                return (T)result;
            }
            result = (IEcsRunner)EcsRunner<T>.Instantiate(_allSystems);
            _runners.Add(type, result);
            return (T)result;
        }
        #endregion

        #region Configuration
        public EcsSession Add(IEcsProcessor system)
        {
            CheckInitForMethod(nameof(AddWorld));
            _allSystems.Add(system);
            return this;
        }
        public EcsSession AddWorld<TArchetype>(EcsWorld<TArchetype> world, string name = "")
            where TArchetype : IWorldArchetype
        {
            CheckInitForMethod(nameof(AddWorld));
            _worldMap.Add(world, name);
            return this;
        }

        #endregion

        #region LifeCycle
        public EcsSession Init()
        {
            CheckInitForMethod(nameof(Init));
            _worldMap.Build();
            _allSystemsSealed = _allSystems.AsReadOnly();
            _isInit = true;

            GetRunner<IEcsInject<EcsWorldMap>>().Inject(_worldMap);

            GetRunner<IEcsPreInitSystem>().PreInit(this);
            GetRunner<IEcsInitSystem>().Init(this);

            _runRunnerCache = GetRunner<IEcsRunSystem>();

            return this;
        }
        public void Run()
        {
            CheckDestroyForMethod(nameof(Run));

            _runRunnerCache.Run(this);
        }
        public void Destroy()
        {
            CheckDestroyForMethod(nameof(Destroy));
            _isDestoryed = true;

            GetRunner<IEcsDestroySystem>().Destroy(this);
        }
        #endregion

        #region StateChecks
        private void CheckInitForMethod(string methodName)
        {
            if (_isInit)
                throw new MethodAccessException($"Запрещено вызывать метод {methodName}, после инициализации {nameof(EcsSession)}");
        }
        private void CheckDestroyForMethod(string methodName)
        {
            if (_isDestoryed)
                throw new MethodAccessException($"Запрещено вызывать метод {methodName}, после уничтожения {nameof(EcsSession)}");
        }
        #endregion

        #region EntityConvert
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity ToEntity(in ent target)
        {
            throw new NotImplementedException();
           // return new Entity(null, target.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ent ToEnt(in Entity target)
        {
            throw new NotImplementedException();
           // return new ent(target.id, target.world._gens[target.id], -1000);
        }
        #endregion



        #region Utils
        #endregion
    }
}

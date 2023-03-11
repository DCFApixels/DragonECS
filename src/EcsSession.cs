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


        private List<IEcsProcessor> _allProcessors;
        private ReadOnlyCollection<IEcsProcessor> _allProcessorsSealed;

        private bool _isInit = false;
        private bool _isDestoryed = false;


        private Dictionary<Type, IEcsProcessorsRunner> _runners;
        private Dictionary<Type, IEcsProcessorsMessenger> _messengers;
        private EcsProcessorsRunner<_Run> _runRunnerCache;

        private EcsWorldMap _worldMap = new EcsWorldMap();

        #region Properties
        public ReadOnlyCollection<IEcsProcessor> AllProcessors => _allProcessorsSealed;

        #endregion

        #region React Runners/Messengers
        public EcsProcessorsRunner<TDoTag> GetRunner<TDoTag>()
        {
            Type type = typeof(TDoTag);
            if (_runners.TryGetValue(type, out IEcsProcessorsRunner result))
            {
                return (EcsProcessorsRunner<TDoTag>)result;
            }
            result = new EcsProcessorsRunner<TDoTag>(this);
            _runners.Add(type, result);
            return (EcsProcessorsRunner<TDoTag>)result;
        }
        internal void OnRunnerDetroyed<TDoTag>(EcsProcessorsRunner<TDoTag> target)
        {
            _runners.Remove(typeof(TDoTag));
        }

        public EcsProcessorsMessenger<TMessege> GetMessenger<TMessege>()
            where TMessege : IEcsMessage
        {
            Type type = typeof(EcsProcessorsMessenger<TMessege>);
            if (_messengers.TryGetValue(type, out IEcsProcessorsMessenger result))
            {
                return (EcsProcessorsMessenger<TMessege>)result;
            }
            result = new EcsProcessorsMessenger<TMessege>(this);
            _messengers.Add(type, result);
            return (EcsProcessorsMessenger<TMessege>)result;
        }
        internal void OnMessengerDetroyed<TMessege>(IEcsProcessorsMessenger<TMessege> target)
            where TMessege : IEcsMessage
        {
            _messengers.Remove(typeof(TMessege));
        }
        #endregion

        #region Configuration
        public EcsSession Add(IEcsProcessor system)
        {
            CheckInitForMethod(nameof(AddWorld));
            _allProcessors.Add(system);
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
        public void Init()
        {
            CheckInitForMethod(nameof(Init));
            _worldMap.Build();
            _allProcessorsSealed = _allProcessors.AsReadOnly();
            _isInit = true;

            GetMessenger<_OnInject<EcsWorldMap>>().Send(new _OnInject<EcsWorldMap>(_worldMap));

            GetRunner<_PreInit>().Run();
            GetRunner<_Init>().Run();

            _runRunnerCache = GetRunner<_Run>();
        }
        public void Run()
        {
            CheckDestroyForMethod(nameof(Run));


            _runRunnerCache.Run();
        }
        public void Destroy()
        {
            CheckDestroyForMethod(nameof(Run));
            _isDestoryed = true;
            GetRunner<_Destroy>().Run();
            GetRunner<_PostDestroy>().Run();
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
            if (_isInit)
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

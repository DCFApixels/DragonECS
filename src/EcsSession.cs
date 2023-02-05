using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{

    public class EcsSession
    {
        private List<IEcsSystem> _allSystems;
        private ReadOnlyCollection<IEcsSystem> _ecsSystemsSealed;

        private bool _isInit = false;
        private bool _isDestoryed = false;

        private int _worldIdIncrement;
        private Dictionary<string, EcsWorld> _worldsDict = new Dictionary<string, EcsWorld>();
        private List<EcsWorld> _worlds = new List<EcsWorld>();

        private Dictionary<Type, IEcsSystemsRunner> _runners;
        private Dictionary<Type, IEcsSystemsMessenger> _messengers;
        private EcsSystemsRunner<_Run> _runRunnerCache;

        #region Properties
        public ReadOnlyCollection<IEcsSystem> AllSystems => _ecsSystemsSealed;

        #endregion

        #region React Runners/Messengers
        public EcsSystemsRunner<TDoTag> GetRunner<TDoTag>()
            where TDoTag : IEcsDoTag
        {
            Type type = typeof(TDoTag);
            if (_runners.TryGetValue(type, out IEcsSystemsRunner result))
            {
                return (EcsSystemsRunner<TDoTag>)result;
            }
            result = new EcsSystemsRunner<TDoTag>(this);
            _runners.Add(type, result);
            return (EcsSystemsRunner<TDoTag>)result;
        }

        public EcsSystemsMessenger<TMessege> GetMessenger<TMessege>()
            where TMessege : IEcsMessage
        {
            Type type = typeof(TMessege);
            if (_messengers.TryGetValue(type, out IEcsSystemsMessenger result))
            {
                return (EcsSystemsMessenger<TMessege>)result;
            }
            result = new EcsSystemsMessenger<TMessege>(this);
            _messengers.Add(type, result);
            return (EcsSystemsMessenger<TMessege>)result;
        }
        #endregion

        #region Configuration
        public EcsSession Add(IEcsSystem system)
        {
            CheckInitForMethod(nameof(AddWorld));

            _allSystems.Add(system);
            return this;
        }
        public EcsSession AddWorld(string name)
        {
            CheckInitForMethod(nameof(AddWorld));

            //_worlds.Add(new EcsWorld(_worldIdIncrement++));
            return this;
        }

        #endregion

        #region LifeCycle
        public void Init()
        {
            CheckInitForMethod(nameof(Init));
            _ecsSystemsSealed = _allSystems.AsReadOnly();
            _isInit = true;

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
    }
}

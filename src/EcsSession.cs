using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{

    public class EcsSession
    {
        private List<IEcsProcessor> _allProcessors;
        private ReadOnlyCollection<IEcsProcessor> _allProcessorsSealed;

        private bool _isInit = false;
        private bool _isDestoryed = false;

        private int _worldIdIncrement;
        private Dictionary<string, EcsWorld> _worldsDict = new Dictionary<string, EcsWorld>();
        private List<EcsWorld> _worlds = new List<EcsWorld>();

        private Dictionary<Type, IEcsProcessorsRunner> _runners;
        private Dictionary<Type, IEcsProcessorsMessenger> _messengers;
        private EcsProcessorsRunner<_Run> _runRunnerCache;

        #region Properties
        public ReadOnlyCollection<IEcsProcessor> AllProcessors => _allProcessorsSealed;

        #endregion

        #region React Runners/Messengers
        public EcsProcessorsRunner<TDoTag> GetRunner<TDoTag>()
            where TDoTag : IEcsDoTag
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
            where TDoTag : IEcsDoTag
        {
            Type type = typeof(TDoTag);
            if (_runners.ContainsKey(type))
            {
                _runners.Remove(type);
            }
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
        public EcsProcessorsGMessenger<TMessege> GetGMessenger<TMessege>()
            where TMessege : IEcsMessage
        {
            Type type = typeof(EcsProcessorsGMessenger<TMessege>);
            if (_messengers.TryGetValue(type, out IEcsProcessorsMessenger result))
            {
                return (EcsProcessorsGMessenger<TMessege>)result;
            }
            result = new EcsProcessorsMessenger<TMessege>(this);
            _messengers.Add(type, result);
            return (EcsProcessorsGMessenger<TMessege>)result;
        }
        internal void OnMessengerDetroyed<TMessege>(IEcsProcessorsMessenger<TMessege> target)
            where TMessege : IEcsMessage
        {
            Type type = typeof(TMessege);
            if (_messengers.ContainsKey(type))
            {
                _messengers.Remove(type);
            }
        }
        #endregion

        #region Configuration
        public EcsSession Add(IEcsProcessor system)
        {
            CheckInitForMethod(nameof(AddWorld));

            _allProcessors.Add(system);
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
            _allProcessorsSealed = _allProcessors.AsReadOnly();
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
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public sealed class EcsSystems
    {
        private IEcsSystem[] _allSystems;
        private Dictionary<Type, IEcsRunner> _runners;
        private IEcsRunSystem _runRunnerCache;

        private ReadOnlyCollection<IEcsSystem> _allSystemsSealed;
        private ReadOnlyDictionary<Type, IEcsRunner> _allRunnersSealed;

        private bool _isInit;
        private bool _isDestoryed;

        #region Properties
        public ReadOnlyCollection<IEcsSystem> AllSystems => _allSystemsSealed;
        public ReadOnlyDictionary<Type, IEcsRunner> AllRunners => _allRunnersSealed;
        public bool IsDestoryed => _isDestoryed;
        #endregion

        #region Constructors
        private EcsSystems(IEcsSystem[] systems)
        {
            _allSystems = systems;
            _runners = new Dictionary<Type, IEcsRunner>();

            _allSystemsSealed = new ReadOnlyCollection<IEcsSystem>(_allSystems);
            _allRunnersSealed = new ReadOnlyDictionary<Type, IEcsRunner>(_runners);

            _isInit = false;
            _isDestoryed = false;
        }
        #endregion

        #region Runners
        public T GetRunner<T>() where T : IEcsSystem
        {
            Type type = typeof(T);
            if (_runners.TryGetValue(type, out IEcsRunner result))
                return (T)result;
            result = (IEcsRunner)EcsRunner<T>.Instantiate(this);
            _runners.Add(type, result);
            return (T)result;
        }
        #endregion

        #region LifeCycle
        public void Init()
        {
            if(_isInit == true)
            {
                EcsDebug.Print("[Warning]", $"This {nameof(EcsSystems)} has already been initialized");
                return;
            }
            _isInit = true;

            GetRunner<IEcsPreInitSystem>().PreInit(this);
            GetRunner<IEcsInitSystem>().Init(this);

            _runRunnerCache = GetRunner<IEcsRunSystem>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
            CheckBeforeInitForMethod(nameof(Run));
            CheckAfterDestroyForMethod(nameof(Run));
#endif
            _runRunnerCache.Run(this);
        }
        public void Destroy()
        {
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
            CheckBeforeInitForMethod(nameof(Run));
#endif
            if (_isDestoryed == true)
            {
                EcsDebug.Print("[Warning]", $"This {nameof(EcsSystems)} has already been destroyed");
                return;
            }
            _isDestoryed = true;
            GetRunner<IEcsDestroySystem>().Destroy(this);
        }
        #endregion

        #region StateChecks
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
        private void CheckBeforeInitForMethod(string methodName)
        {
            if (!_isInit)
                throw new MethodAccessException($"It is forbidden to call {methodName}, before initialization {nameof(EcsSystems)}");
        }
        private void CheckAfterInitForMethod(string methodName)
        {
            if (_isInit)
                throw new MethodAccessException($"It is forbidden to call {methodName}, after initialization {nameof(EcsSystems)}");
        }
        private void CheckAfterDestroyForMethod(string methodName)
        {
            if (_isDestoryed)
                throw new MethodAccessException($"It is forbidden to call {methodName}, after destroying {nameof(EcsSystems)}");
        }
#endif
        #endregion

        #region Builder
        public static Builder New()
        {
            return new Builder();
        }
        public class Builder
        {
            private const int KEYS_CAPACITY = 4;
            private readonly List<object> _blockExecutionOrder;
            private readonly Dictionary<object, List<IEcsSystem>> _systems;
            private readonly object _basicBlocKey;
            private bool _isBasicBlockDeclared;
            private bool _isOnlyBasicBlock;
            public Builder()
            {
                _basicBlocKey = "Basic";
                _blockExecutionOrder = new List<object>(KEYS_CAPACITY);
                _systems = new Dictionary<object, List<IEcsSystem>>(KEYS_CAPACITY);
                _isBasicBlockDeclared = false;
                _isOnlyBasicBlock = true;
            }

            public Builder Add(IEcsSystem system, object blockKey = null)
            {
                if (blockKey == null) blockKey = _basicBlocKey;
                List<IEcsSystem> list;
                if (!_systems.TryGetValue(blockKey, out list))
                {
                    list = new List<IEcsSystem>();
                    list.Add(new SystemsBlockMarkerSystem(blockKey.ToString()));
                    _systems.Add(blockKey, list);
                }
                list.Add(system);
                return this;
            }

            public Builder Add(IEcsModule module)
            {
                module.ImportSystems(this);
                return this;
            }

            public Builder BasicSystemsBlock()
            {
                _isBasicBlockDeclared = true;
                _blockExecutionOrder.Add(_basicBlocKey);
                return this;
            }
            public Builder SystemsBlock(object blockKey)
            {
                if (blockKey == null)
                    return BasicSystemsBlock();

                _isOnlyBasicBlock = false;
                _blockExecutionOrder.Add(blockKey);
                return this;
            }

            public EcsSystems Build()
            {
                if (_isOnlyBasicBlock)
                {
                    return new EcsSystems(_systems[_basicBlocKey].ToArray());
                }

                if(_isBasicBlockDeclared == false)
                    _blockExecutionOrder.Insert(0, _basicBlocKey);

                List<IEcsSystem> result = new List<IEcsSystem>(32);

                List<IEcsSystem> basicBlockList = _systems[_basicBlocKey];

                foreach (var item in _systems)
                {
                    if (!_blockExecutionOrder.Contains(item.Key))
                    {
                        basicBlockList.AddRange(item.Value);
                    }
                }
                foreach (var item in _blockExecutionOrder)
                {
                    if(_systems.TryGetValue(item, out var list))
                    {
                        result.AddRange(list);
                    }
                }

                return new EcsSystems(result.ToArray());
            }
        }
        #endregion
    }

    public interface IEcsModule
    {
        public void ImportSystems(EcsSystems.Builder builder);
    }

    public static class EcsSystemsExt
    {
        public static bool IsNullOrDestroyed(this EcsSystems self)
        {
            return self == null || self.IsDestoryed;
        }
        public static EcsSystems.Builder Add(this EcsSystems.Builder self, IEnumerable<IEcsSystem> range, object blockKey = null)
        {
            foreach (var item in range)
            {
                self.Add(item, blockKey);
            }
            return self;
        }
        public static EcsSystems BuildAndInit(this EcsSystems.Builder self)
        {
            EcsSystems result = self.Build();
            result.Init();
            return result;
        }

    }
}

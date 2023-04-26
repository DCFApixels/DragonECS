using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public sealed class EcsPipeline
    {
        private static EcsPipeline _empty;
        public static EcsPipeline Empty
        {
            get
            {
                if(_empty == null)
                {
                    _empty = new EcsPipeline(Array.Empty<IEcsSystem>());
                    _empty.Init();
                    _empty._isEmptyDummy = true;
                }
                return _empty;
            }
        }

        private IEcsSystem[] _allSystems;
        private Dictionary<Type, IEcsRunner> _runners;
        private IEcsRunSystem _runRunnerCache;

        private ReadOnlyCollection<IEcsSystem> _allSystemsSealed;
        private ReadOnlyDictionary<Type, IEcsRunner> _allRunnersSealed;

        private bool _isInit;
        private bool _isDestoryed;
        private bool _isEmptyDummy;

        #region Properties
        public ReadOnlyCollection<IEcsSystem> AllSystems => _allSystemsSealed;
        public ReadOnlyDictionary<Type, IEcsRunner> AllRunners => _allRunnersSealed;
        public bool IsInit => _isInit;
        public bool IsDestoryed => _isDestoryed;
        #endregion

        #region Constructors
        private EcsPipeline(IEcsSystem[] systems)
        {
            _allSystems = systems;
            _runners = new Dictionary<Type, IEcsRunner>();

            _allSystemsSealed = new ReadOnlyCollection<IEcsSystem>(_allSystems);
            _allRunnersSealed = new ReadOnlyDictionary<Type, IEcsRunner>(_runners);

            _isInit = false;
            _isEmptyDummy = false;
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

        internal void OnRunnerDestroy(IEcsRunner runner)
        {
            _runners.Remove(runner.Interface);
        }
        #endregion

        #region LifeCycle
        public void Init()
        {
            if (_isEmptyDummy)
                return;

            if (_isInit == true)
            {
                EcsDebug.Print("[Warning]", $"This {nameof(EcsPipeline)} has already been initialized");
                return;
            }
            _isInit = true;

            var ecsPipelineInjectRunner = GetRunner<IEcsInject<EcsPipeline>>();
            ecsPipelineInjectRunner.Inject(this);
            EcsRunner.Destroy(ecsPipelineInjectRunner);
            var preInitRunner = GetRunner<IEcsPreInitSystem>();
            preInitRunner.PreInit(this);
            EcsRunner.Destroy(preInitRunner);
            var initRunner = GetRunner<IEcsInitSystem>();
            initRunner.Init(this);
            EcsRunner.Destroy(initRunner);

            _runRunnerCache = GetRunner<IEcsRunSystem>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run()
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            CheckBeforeInitForMethod(nameof(Run));
            CheckAfterDestroyForMethod(nameof(Run));
#endif
            _runRunnerCache.Run(this);
        }
        public void Destroy()
        {
            if (_isEmptyDummy)
                return;

#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            CheckBeforeInitForMethod(nameof(Run));
#endif
            if (_isDestoryed == true)
            {
                EcsDebug.Print("[Warning]", $"This {nameof(EcsPipeline)} has already been destroyed");
                return;
            }
            _isDestoryed = true;
            GetRunner<IEcsDestroySystem>().Destroy(this);
        }
        #endregion

        #region StateChecks
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
        private void CheckBeforeInitForMethod(string methodName)
        {
            if (!_isInit)
                throw new MethodAccessException($"It is forbidden to call {methodName}, before initialization {nameof(EcsPipeline)}");
        }
        private void CheckAfterInitForMethod(string methodName)
        {
            if (_isInit)
                throw new MethodAccessException($"It is forbidden to call {methodName}, after initialization {nameof(EcsPipeline)}");
        }
        private void CheckAfterDestroyForMethod(string methodName)
        {
            if (_isDestoryed)
                throw new MethodAccessException($"It is forbidden to call {methodName}, after destroying {nameof(EcsPipeline)}");
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
            private HashSet<Type> _uniqueTypes;
            private readonly List<object> _blockExecutionOrder;
            private readonly Dictionary<object, List<IEcsSystem>> _systems;
            private readonly string _basicBlocKey;
            private bool _isBasicBlockDeclared;
            private bool _isOnlyBasicBlock;
            public Builder()
            {
                _basicBlocKey = EcsConsts.BASIC_SYSTEMS_BLOCK;
                _uniqueTypes = new HashSet<Type>();
                _blockExecutionOrder = new List<object>(KEYS_CAPACITY);
                _systems = new Dictionary<object, List<IEcsSystem>>(KEYS_CAPACITY);
                _isBasicBlockDeclared = false;
                _isOnlyBasicBlock = true;

                SystemsBlock(EcsConsts.PRE_BEGIN_SYSTEMS_BLOCK);
                SystemsBlock(EcsConsts.BEGIN_SYSTEMS_BLOCK);
            }

            public Builder Add(IEcsSystem system, object blockKey = null)
            {
                AddInternal(system, blockKey, false);
                return this;
            }
            public Builder AddUnique(IEcsSystem system, object blockKey = null)
            {
                AddInternal(system, blockKey, true);
                return this;
            }

            private void AddInternal(IEcsSystem system, object blockKey, bool isUnique)
            {
                if (blockKey == null) blockKey = _basicBlocKey;
                List<IEcsSystem> list;
                if (!_systems.TryGetValue(blockKey, out list))
                {
                    list = new List<IEcsSystem>();
                    list.Add(new SystemsBlockMarkerSystem(blockKey.ToString()));
                    _systems.Add(blockKey, list);
                }
                if ((_uniqueTypes.Add(system.GetType()) == false && isUnique))
                    return;
                list.Add(system);
            }

            public Builder AddModule(IEcsModule module)
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
            public Builder SystemsBlock(string blockKey)
            {
                if (blockKey == null || blockKey == _basicBlocKey)
                    return BasicSystemsBlock();

                _isOnlyBasicBlock = false;
                _blockExecutionOrder.Add(blockKey);
                return this;
            }
            public Builder InsertSystemsBlock(string blockKey, string beforeBlockKey)
            {
                if (blockKey == null || blockKey == _basicBlocKey)
                {
                    _isBasicBlockDeclared = true;
                    blockKey = _basicBlocKey;
                }

                _isOnlyBasicBlock = false;
                int index = _blockExecutionOrder.IndexOf(beforeBlockKey);
                index = index < 0 ? _blockExecutionOrder.Count - 1 : index;
                _blockExecutionOrder.Insert(index, blockKey);
                return this;
            }

            public EcsPipeline Build()
            {
                if (_isOnlyBasicBlock)
                {
                    return new EcsPipeline(_systems[_basicBlocKey].ToArray());
                }

                if(_isBasicBlockDeclared == false)
                    _blockExecutionOrder.Insert(_blockExecutionOrder.IndexOf(EcsConsts.BEGIN_SYSTEMS_BLOCK) + 1, _basicBlocKey);//вставить после BEGIN_SYSTEMS_BLOCK
                SystemsBlock(EcsConsts.END_SYSTEMS_BLOCK);
                SystemsBlock(EcsConsts.POST_END_SYSTEMS_BLOCK);
                Add(new DeleteEmptyEntitesSsytem(), EcsConsts.POST_END_SYSTEMS_BLOCK);

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

                return new EcsPipeline(result.ToArray());
            }
        }
        #endregion
    }

    public interface IEcsModule
    {
        public void ImportSystems(EcsPipeline.Builder b);
    }

    #region Extensions
    public static class EcsPipelineExtensions
    {
        public static void GetRunner<T>(this EcsPipeline self, out T runner) where T : IEcsSystem
        {
            runner = self.GetRunner<T>();
        }
        public static bool IsNullOrDestroyed(this EcsPipeline self)
        {
            return self == null || self.IsDestoryed;
        }
        public static EcsPipeline.Builder Add(this EcsPipeline.Builder self, IEnumerable<IEcsSystem> range, object blockKey = null)
        {
            foreach (var item in range)
            {
                self.Add(item, blockKey);
            }
            return self;
        }
        public static EcsPipeline.Builder AddUnique(this EcsPipeline.Builder self, IEnumerable<IEcsSystem> range, object blockKey = null)
        {
            foreach (var item in range)
            {
                self.AddUnique(item, blockKey);
            }
            return self;
        }
        public static EcsPipeline BuildAndInit(this EcsPipeline.Builder self)
        {
            EcsPipeline result = self.Build();
            result.Init();
            return result;
        }

    }
    #endregion
}

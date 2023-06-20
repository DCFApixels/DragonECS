using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsWorld
    {
        private const short GEN_BITS = 0x7fff;
        private const short DEATH_GEN_BIT = short.MinValue;
        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 2;

        internal static EcsWorld[] Worlds = new EcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser(0);
        public static EcsWorld GetWorld(int worldID) => Worlds[worldID];

        public readonly short id;

        private Type _worldType;
        private int _worldTypeID;

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущность
        private short[] _componentCounts;
        private EcsGroup _allEntites;

        private int[] _delEntBuffer;
        private int _delEntBufferCount;

        internal IEcsPoolImplementation[] _pools;
        private EcsNullPool _nullPool = EcsNullPool.instance;

        private EcsSubject[] _subjects;
        private EcsQueryExecutor[] _executors;

        private List<WeakReference<EcsGroup>> _groups = new List<WeakReference<EcsGroup>>();
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private List<IEcsWorldEventListener> _listeners = new List<IEcsWorldEventListener>();
        private List<IEcsEntityEventListener> _entityListeners = new List<IEcsEntityEventListener>();

        private object[] _components = new object[2];

        #region Properties
        public int WorldTypeID => _worldTypeID;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        public ReadOnlySpan<IEcsPoolImplementation> AllPools => _pools;// new ReadOnlySpan<IEcsPoolImplementation>(pools, 0, _poolsCount);
        #endregion

        #region Constructors/Destroy
        static EcsWorld()
        {
            Worlds[0] = new EcsNullWorld();
        }
        public EcsWorld() : this(true) { }
        internal EcsWorld(bool isIndexable)
        {
            _entitesCapacity = 512;

            if (isIndexable)
            {
                id = (short)_worldIdDispenser.GetFree();
                if (id >= Worlds.Length)
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                Worlds[id] = this;
            }

            _worldType = this.GetType();
            _worldTypeID = WorldMetaStorage.GetWorldID(_worldType);

            _entityDispenser = new IntDispenser(0);
            _pools = new IEcsPoolImplementation[512];
            ArrayUtility.Fill(_pools, _nullPool);

            _gens = new short[_entitesCapacity];
            _componentCounts = new short[_entitesCapacity];

            ArrayUtility.Fill(_gens, DEATH_GEN_BIT);
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_entitesCapacity >> DEL_ENT_BUFFER_SIZE_OFFSET];

            _allEntites = GetFreeGroup();

            _subjects = new EcsSubject[128];
            _executors = new EcsQueryExecutor[128];
        }
        public void Destroy()
        {
            _entityDispenser = null;
            _gens = null;
            _pools = null;
            _nullPool = null;
            _subjects = null;
            _executors = null;
            Worlds[id] = null;
            _worldIdDispenser.Release(id);
        }
        #endregion

        #region ComponentInfo
        public int GetComponentID<T>() => WorldMetaStorage.GetComponentID<T>(_worldTypeID);
        public int GetComponentID(Type type) => WorldMetaStorage.GetComponentID(type, _worldTypeID);
        public Type GetComponentType(int componentID) => WorldMetaStorage.GetComponentType(_worldTypeID, componentID);
        public bool IsComponentTypeDeclared<T>() => IsComponentTypeDeclared(typeof(T));
        public bool IsComponentTypeDeclared(Type type) => WorldMetaStorage.IsComponentTypeDeclared(_worldTypeID, type);
        #endregion

        #region Getters
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        public TPool GetPool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            int index = WorldMetaStorage.GetPoolID<TPool>(_worldTypeID);
            if (index >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);
            }
            if (_pools[index] == _nullPool)
            {
                var pool = new TPool();
                _pools[index] = pool;
                pool.OnInit(this, index);
            }
            return (TPool)_pools[index];
        }
        public TSubject GetSubject<TSubject>() where TSubject : EcsSubject
        {
            int index = WorldMetaStorage.GetSubjectID<TSubject>(_worldTypeID);
            if (index >= _subjects.Length)
                Array.Resize(ref _subjects, _subjects.Length << 1);
            if (_subjects[index] == null)
                _subjects[index] = EcsSubject.Builder.Build<TSubject>(this);
            return (TSubject)_subjects[index];
        }
        public TExecutor GetExecutor<TExecutor>() where TExecutor : EcsQueryExecutor, new()
        {
            int index = WorldMetaStorage.GetExecutorID<TExecutor>(_worldTypeID);
            if (index >= _executors.Length)
                Array.Resize(ref _executors, _executors.Length << 1);
            var result = _executors[index];
            if (result == null)
            {
                result = new TExecutor();
                _executors[index] = result;
                result.Initialize(this);
            }
            return (TExecutor)result;
        }
        #endregion

        #region WorldComponents
        public EcsWorld SetupComponents(IEnumerable<object> components, params object[] componentsParams)
        {
            foreach (var component in components)
                SetComponent(component);
            foreach (var component in componentsParams)
                SetComponent(component);
            return this;
        }
        public EcsWorld SetupComponents(params object[] components)
        {
            foreach (var component in components)
                SetComponent(component);
            return this;
        }
        public void SetComponent(object component)
        {
            Type componentType = component.GetType();
            if (componentType.IsValueType || componentType.IsPrimitive)
                throw new ArgumentException();
            SetComponentInternal(WorldMetaStorage.GetWorldComponentID(componentType, _worldTypeID), component);
        }
        public void SetComponent<T>(T component) where T : class
        {
            SetComponentInternal(WorldMetaStorage.GetWorldComponentID<T>(_worldTypeID), component);
        }
        private void SetComponentInternal(int index, object component)
        {
            if (index >= _components.Length)
                Array.Resize(ref _components, _components.Length << 1);

            ref var currentComponent = ref _components[index];
            if (currentComponent == component)
                return;
            if (currentComponent != null && currentComponent is IEcsWorldComponent oldComponentInterface)
                oldComponentInterface.OnRemovedFromWorld(this);
            currentComponent = component;
            if (component is IEcsWorldComponent newComponentInterface)
                newComponentInterface.OnAddedToWorld(this);
        }
        public T GetComponent<T>() where T : class
        {
            if (!TryGetComponent(out T result))
                throw new NullReferenceException();
            return result;
        }
        public bool TryGetComponent<T>(out T component) where T : class
        {
            int index = WorldMetaStorage.GetWorldComponentID<T>(_worldTypeID);
            if (index >= _components.Length)
            {
                component = null;
                return false;
            }
            component = (T)_components[index];
            return component != null;
        }
        public bool HasComponent<T>()
        {
            int index = WorldMetaStorage.GetWorldComponentID<T>(_worldTypeID);
            if (index >= _components.Length)
                return false;
            return _components[index] != null;
        }
        public void RemoveComponent<T>()
        {
            int index = WorldMetaStorage.GetWorldComponentID<T>(_worldTypeID);
            ref var currentComponent = ref _components[index];
            if (currentComponent is IEcsWorldComponent componentInterface)
                componentInterface.OnRemovedFromWorld(this);
            currentComponent = null;
        }
        #endregion

        #region Where Query
        public EcsReadonlyGroup WhereFor<TSubject>(EcsReadonlyGroup sourceGroup, out TSubject subject) where TSubject : EcsSubject
        {
            var executor = GetExecutor<EcsWhereExecutor<TSubject>>();
            subject = executor.Subject;
            return executor.ExecuteFor(sourceGroup);
        }
        public EcsReadonlyGroup WhereFor<TSubject>(EcsReadonlyGroup sourceGroup) where TSubject : EcsSubject
        {
            return GetExecutor<EcsWhereExecutor<TSubject>>().ExecuteFor(sourceGroup);
        }
        public EcsReadonlyGroup Where<TSubject>(out TSubject subject) where TSubject : EcsSubject
        {
            var executor = GetExecutor<EcsWhereExecutor<TSubject>>();
            subject = executor.Subject;
            return executor.Execute();
        }
        public EcsReadonlyGroup Where<TSubject>() where TSubject : EcsSubject
        {
            return GetExecutor<EcsWhereExecutor<TSubject>>().Execute();
        }
        #endregion

        #region Entity
        public int NewEmptyEntity()
        {
            int entityID = _entityDispenser.GetFree();
            _entitiesCount++;

            if (_gens.Length <= entityID)
            {
                Array.Resize(ref _gens, _gens.Length << 1);
                Array.Resize(ref _componentCounts, _gens.Length);
                ArrayUtility.Fill(_gens, DEATH_GEN_BIT, _entitesCapacity);
                _entitesCapacity = _gens.Length;

                for (int i = 0; i < _groups.Count; i++)
                {
                    if (_groups[i].TryGetTarget(out EcsGroup group))
                    {
                        group.OnWorldResize(_gens.Length);
                    }
                    else
                    {
                        int last = _groups.Count - 1;
                        _groups[i--] = _groups[last];
                        _groups.RemoveAt(last);
                    }
                }
                foreach (var item in _pools)
                    item.OnWorldResize(_gens.Length);

                _listeners.InvokeOnWorldResize(_gens.Length);
            }
            _gens[entityID] &= GEN_BITS;
            _allEntites.Add(entityID);
            _entityListeners.InvokeOnNewEntity(entityID);
            return entityID;
        }
        public entlong NewEmptyEntityLong()
        {
            int e = NewEmptyEntity();
            return GetEntityLong(e);
        }
        public void DelEntity(int entityID)
        {
            _allEntites.Remove(entityID);
            _delEntBuffer[_delEntBufferCount++] = entityID;
            _gens[entityID] |= DEATH_GEN_BIT;
            _entitiesCount--;
            _entityListeners.InvokeOnDelEntity(entityID);

            if (_delEntBufferCount >= _delEntBuffer.Length)
                ReleaseDelEntityBuffer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong GetEntityLong(int entityID) => new entlong(entityID, _gens[entityID], id); //TODO придумать получше имя метода
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityID, short gen) => _gens[entityID] == gen;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int entityID) => _gens[entityID] >= 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetGen(int entityID) => _gens[entityID];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetComponentsCount(int entityID) => _componentCounts[entityID];

        public bool IsMatchesMask(EcsMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (mask._worldTypeID != _worldTypeID)
                throw new EcsFrameworkException("The types of the target world of the mask and this world are different.");
#endif
            for (int i = 0, iMax = mask._inc.Length; i < iMax; i++)
            {
                if (!_pools[mask._inc[i]].Has(entityID))
                    return false;
            }
            for (int i = 0, iMax = mask._exc.Length; i < iMax; i++)
            {
                if (_pools[mask._exc[i]].Has(entityID))
                    return false;
            }
            return true;
        }
        public void ReleaseDelEntityBuffer()
        {
            ReadOnlySpan<int> buffser = new ReadOnlySpan<int>(_delEntBuffer, 0, _delEntBufferCount);
            foreach (var pool in _pools)
                pool.OnReleaseDelEntityBuffer(buffser);
            _listeners.InvokeOnReleaseDelEntityBuffer(buffser);
            for (int i = 0; i < _delEntBufferCount; i++)
                _entityDispenser.Release(_delEntBuffer[i]);
            _delEntBufferCount = 0;
        }
        public void DeleteEmptyEntites()
        {
            foreach (var e in _allEntites)
            {
                if (_componentCounts[e] <= 0) DelEntity(e);
            }
        }

        #region Copy/Clone
        public void CopyEntity(int fromEntityID, int toEntityID)
        {
            foreach (var pool in _pools)
            {
                if (pool.Has(fromEntityID)) pool.Copy(fromEntityID, toEntityID);
            }
        }
        public int CloneEntity(int fromEntityID)
        {
            int newEntity = NewEmptyEntity();
            CopyEntity(fromEntityID, newEntity);
            return newEntity;
        }
        public void CloneEntity(int fromEntityID, int toEntityID)
        {
            CopyEntity(fromEntityID, toEntityID);
            foreach (var pool in _pools)
            {
                if (!pool.Has(fromEntityID) && pool.Has(toEntityID))
                    pool.Del(toEntityID);
            }
        }
        #endregion

        #region Components Increment
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementEntityComponentCount(int entityID) => _componentCounts[entityID]++;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementEntityComponentCount(int entityID)
        {
            var count = --_componentCounts[entityID];
            if (count == 0 && _allEntites.Has(entityID)) DelEntity(entityID);
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (count < 0) throw new EcsFrameworkException("нарушен баланс инкремента/декремента компонентов");
#endif
        }
        #endregion

        #endregion

        #region Groups Pool
        internal void RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        internal EcsGroup GetFreeGroup()
        {
            EcsGroup result = _groupsPool.Count <= 0 ? new EcsGroup(this) : _groupsPool.Pop();
            result._isReleased = false;
            return result;
        }
        internal void ReleaseGroup(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (group.World != this)
                throw new ArgumentException("groupFilter.WorldIndex != this");
#endif
            group._isReleased = true;
            group.Clear();
            _groupsPool.Push(group);
        }
        #endregion

        #region Listeners
        public void AddListener(IEcsWorldEventListener worldEventListener)
        {
            _listeners.Add(worldEventListener);
        }
        public void RemoveListener(IEcsWorldEventListener worldEventListener)
        {
            _listeners.Remove(worldEventListener);
        }
        public void AddListener(IEcsEntityEventListener entityEventListener)
        {
            _entityListeners.Add(entityEventListener);
        }
        public void RemoveListener(IEcsEntityEventListener entityEventListener)
        {
            _entityListeners.Remove(entityEventListener);
        }
        #endregion

        #region Debug
        public void GetComponents(int entityID, List<object> list)
        {
            list.Clear();
            var itemsCount = GetComponentsCount(entityID);

            for (var i = 0; i < _pools.Length; i++)
            {
                if (_pools[i].Has(entityID))
                    list.Add(_pools[i].GetRaw(entityID));
            }
        }
        #endregion
    }

    internal sealed class EcsNullWorld : EcsWorld { }

    #region Callbacks Interface
    public interface IEcsWorldComponent
    {
        void OnAddedToWorld(EcsWorld world);
        void OnRemovedFromWorld(EcsWorld world);
    }
    public interface IEcsWorldEventListener
    {
        void OnWorldResize(int newSize);
        void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer);
        void OnWorldDestroy();
    }
    public interface IEcsEntityEventListener
    {
        void OnNewEntity(int entityID);
        void OnDelEntity(int entityID);
    }
    internal static class WorldEventListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnWorldResize(this List<IEcsWorldEventListener> self, int newSize)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnWorldResize(newSize);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnReleaseDelEntityBuffer(this List<IEcsWorldEventListener> self, ReadOnlySpan<int> buffer)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnReleaseDelEntityBuffer(buffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnWorldDestroy(this List<IEcsWorldEventListener> self)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnWorldDestroy();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnNewEntity(this List<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnNewEntity(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDelEntity(this List<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnDelEntity(entityID);
        }
    }
    #endregion

    #region Extensions
    public static class IntExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static entlong ToEntityLong(this int self, EcsWorld world) => world.GetEntityLong(self);
    }
    #endregion
}

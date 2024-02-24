using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public interface IEcsInterfacePool<T> : IEcsReadonlyPool where T : class
    {
        T Get(int entityID);
    }
    public interface IEcsInterfaceComponent { }
    public class EcsInterfacePool<T> : IEcsPoolImplementation<T>, IEcsInterfacePool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : class, IEcsInterfaceComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        private EcsWorld.PoolsMediator _mediator;

        #region Properties
        public int ComponentTypeID
        {
            get { return _componentTypeID; }
        }
        public Type ComponentType
        {
            get { return typeof(T); }
        }
        public EcsWorld World
        {
            get { return _source; }
        }
        public int Count
        {
            get { return _itemsCount; }
        }
        public bool IReadOnly
        {
            get { return true; }
        }
        #endregion

        #region Methdos
        public T Get(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            _listeners.InvokeOnGet(entityID);
            return _items[_mapping[entityID]];
        }
        public bool Has(int entityID)
        {
            return _mapping[entityID] > 0;
        }
        #endregion

        #region Other
        object IEcsReadonlyPool.GetRaw(int entityID)
        {
            return Get(entityID);
        }
        public void Copy(int fromEntityID, int toEntityID) { }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID) { }
        void IEcsPool.AddRaw(int entityID, object dataRaw)
        {
            EcsDebug.PrintWarning("Is read only!");
        }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
            EcsDebug.PrintWarning("Is read only!");
        }
        void IEcsPool.Del(int entityID)
        {
            EcsDebug.PrintWarning("Is read only!");
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            throw new NotImplementedException();
        }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            throw new NotImplementedException();
        }
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            throw new NotImplementedException();
        }
        void IEcsPoolImplementation.OnWorldDestroy()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Listeners
        public void AddListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { throw new ArgumentNullException("listener is null"); }
            _listeners.Add(listener);
        }
        public void RemoveListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { throw new ArgumentNullException("listener is null"); }
            _listeners.Remove(listener);
        }
        #endregion

        #region IEnumerator - IntelliSense hack
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        #endregion
    }
}

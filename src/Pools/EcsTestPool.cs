using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    /// <summary>Pool for IEcsComponent components</summary>
    public sealed class EcsTestPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsTestComponent
    {
        private EcsWorld _source;
        private int _componentID;


        public const int MIN_CAPACITY_BITS_OFFSET = 4;
        public const int MIN_CAPACITY = 1 << MIN_CAPACITY_BITS_OFFSET;
        private const int EMPTY = -1;

        private int[] _buckets = Array.Empty<int>();
        private Entry[] _entries = Array.Empty<Entry>();

        private int _count;

        private int _freeList;
        private int _freeCount;

        private int _modBitMask;


        private IEcsComponentReset<T> _componentResetHandler = EcsComponentResetHandler<T>.instance;
        private IEcsComponentCopy<T> _componentCopyHandler = EcsComponentCopyHandler<T>.instance;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        #region Properites
        public int Count => _count;
        public int Capacity => _entries.Length;
        public int ComponentID => _componentID;
        public Type ComponentType => typeof(T);
        public EcsWorld World => _source;
        #endregion

        #region Methods
        public void Copy(int fromEntityID, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID);
#endif
            _componentCopyHandler.Copy(ref Get(fromEntityID), ref TryAddOrGet(toEntityID));
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID);
#endif
            _componentCopyHandler.Copy(ref Get(fromEntityID), ref toWorld.GetPool<T>().TryAddOrGet(toEntityID));
        }
        #endregion


        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, int componentID)
        {
            _source = world;
            _componentID = componentID;

            int minCapacity = 512;
            minCapacity = NormalizeCapacity(minCapacity);
            _buckets = new int[minCapacity];
            for (int i = 0; i < minCapacity; i++)
                _buckets[i] = EMPTY;
            _entries = new Entry[minCapacity];
            _modBitMask = (minCapacity - 1) & 0x7FFFFFFF;
        }
        void IEcsPoolImplementation.OnWorldResize(int newSize) { }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            foreach (var entityID in buffer)
                TryDel(entityID);
        }
        #endregion

        #region Other
        void IEcsPool.AddRaw(int entityID, object dataRaw) => Add(entityID) = (T)dataRaw;
        object IEcsPool.GetRaw(int entityID) => Read(entityID);
        void IEcsPool.SetRaw(int entityID, object dataRaw) => Get(entityID) = (T)dataRaw;
        ref readonly T IEcsStructPool<T>.Read(int entityID) => ref Read(entityID);
        ref T IEcsStructPool<T>.Get(int entityID) => ref Get(entityID);
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
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        #endregion


        #region Find/Insert/Remove
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(long x, long y)
        {
            return FindEntry(x << 32 | y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(long key)
        {
            for (int i = _buckets[unchecked((int)key & _modBitMask)]; i >= 0; i = _entries[i].next)
                if (_entries[i].key == key) return i;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => Has(entityID, entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Has(long x, long y) => Has(x << 32 | y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Has(long key) {
            for (int i = _buckets[unchecked((int)key & _modBitMask)]; i >= 0; i = _entries[i].next)
                if (_entries[i].key == key) return true;
            return false;
        }

            public ref T Add(int entityID) => ref Add(entityID, entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T Add(long x, long y) => ref Add(x << 32 | y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T Add(long key)
        {
            int entityID = unchecked((int)key);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(key: key)) EcsPoolThrowHalper.ThrowAlreadyHasComponent<T>(entityID);
#endif

            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                _freeList = _entries[index].next;
                _freeCount--;
            }
            else
            {
                if (_count == _entries.Length)
                    Resize();
                index = _count++;
            }
            int targetBucket = unchecked((int)key & _modBitMask);

            ref var entry = ref _entries[index];
            entry.next = _buckets[targetBucket];
            entry.key = key;
            entry.value = default;
            _buckets[targetBucket] = index;
            this.IncrementEntityComponentCount(entityID);
            _listeners.InvokeOnAddAndGet(entityID);
            return ref entry.value;
        }
        public ref T TryAddOrGet(int entityID)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID) => ref Get(entityID, entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T Get(long x, long y) => ref Get(x << 32 | y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T Get(long key)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(key: key)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(unchecked((int)key));
#endif
            _listeners.InvokeOnGet(unchecked((int)key));
            return ref _entries[FindEntry(key)].value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID) => ref Read(entityID, entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(long x, long y) => ref Read(x << 32 | y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(long key)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(key: key)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(unchecked((int)key));
#endif
            return ref _entries[FindEntry(key)].value;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => Del(entityID, entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Del(long keyX, long keyY) => Del(keyX + (keyY << 32));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Del(long key)
        {
            int entityID = unchecked((int)key);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(key: key)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            int bucket = unchecked((int)key & _modBitMask);
            int last = -1;
            for (int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].next)
            {
                if (_entries[i].key == key)
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = _entries[i].next;
                    }
                    else
                    {
                        _entries[last].next = _entries[i].next;
                    }
                    _entries[i].next = _freeList;
                    _entries[i].key = -1;
                    //_entries[i].value = default;
                    _componentResetHandler.Reset(ref _entries[i].value);
                    _freeList = i;
                    _freeCount++;
                    this.DecrementEntityComponentCount(entityID);
                    _listeners.InvokeOnDel(entityID);
                    return;
                }
            }
        }
        public void TryDel(int entityID)
        {
            if (Has(entityID)) Del(entityID);
        }
        #endregion

        #region Resize
        private void Resize()
        {
            int newSize = _buckets.Length << 1;
            _modBitMask = (newSize - 1) & 0x7FFFFFFF;

            Contract.Assert(newSize >= _entries.Length);
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++)
                newBuckets[i] = EMPTY;

            Entry[] newEntries = new Entry[newSize];
            Array.Copy(_entries, 0, newEntries, 0, _count);
            for (int i = 0; i < _count; i++)
            {
                if (newEntries[i].key >= 0)
                {
                    int bucket = unchecked((int)newEntries[i].key & _modBitMask);
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            _buckets = newBuckets;
            _entries = newEntries;
        }

        private int NormalizeCapacity(int capacity)
        {
            int result = MIN_CAPACITY;
            while (result < capacity) result <<= 1;
            return result;
        }
        #endregion

        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Entry
        {
            public int next;        // Index of next entry, -1 if last
            public long key;
            public T value;
        }
        #endregion
    }
    /// <summary>Standard component</summary>
    public interface IEcsTestComponent { }
    public static class EcsTestPoolExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTestPool<TComponent> GetPool<TComponent>(this EcsWorld self) where TComponent : struct, IEcsTestComponent
        {
            return self.GetPool<EcsTestPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTestPool<TComponent> GetPoolUnchecked<TComponent>(this EcsWorld self) where TComponent : struct, IEcsTestComponent
        {
            return self.GetPoolUnchecked<EcsTestPool<TComponent>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTestPool<TComponent> Include<TComponent>(this EcsAspectBuilderBase self) where TComponent : struct, IEcsTestComponent
        {
            return self.Include<EcsTestPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTestPool<TComponent> Exclude<TComponent>(this EcsAspectBuilderBase self) where TComponent : struct, IEcsTestComponent
        {
            return self.Exclude<EcsTestPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTestPool<TComponent> Optional<TComponent>(this EcsAspectBuilderBase self) where TComponent : struct, IEcsTestComponent
        {
            return self.Optional<EcsTestPool<TComponent>>();
        }
    }
}

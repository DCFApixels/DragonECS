using System;
using System.Collections;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsWorldConfig
    {
        int Count { get; }
        bool Has(string valueName);
        T Get<T>(string valueName);
        bool TryGet<T>(string valueName, out T value);
        IEnumerable<KeyValuePair<string, object>> GetAllConfigs();
    }
    public interface IEcsWorldConfigWriter
    {
        int Count { get; }
        void Set<T>(string valueName, T value);
        bool Has(string valueName);
        T Get<T>(string valueName);
        bool TryGet<T>(string valueName, out T value);
        void Remove(string valueName);
        IEnumerable<KeyValuePair<string, object>> GetAllConfigs();
    }
    [Serializable]
    public class EcsWorldConfig : IEcsWorldConfigWriter, IEcsWorldConfig, IEnumerable<KeyValuePair<string, object>>
    {
        public static readonly IEcsWorldConfig Empty = new EmptyConfig();

        private Dictionary<string, object> _storage = new Dictionary<string, object>();

        public EcsWorldConfig() { }
        public EcsWorldConfig(IEnumerable<KeyValuePair<string, object>> range)
        {
            _storage = new Dictionary<string, object>(range);
        }
        public EcsWorldConfig(params KeyValuePair<string, object>[] range)
        {
            _storage = new Dictionary<string, object>(range);
        }

        public int Count
        {
            get { return _storage.Count; }
        }
        public T Get<T>(string valueName)
        {
            return (T)_storage[valueName];
        }
        public bool Has(string valueName)
        {
            return _storage.ContainsKey(valueName);
        }
        public void Remove(string valueName)
        {
            _storage.Remove(valueName);
        }
        public void Set<T>(string valueName, T value)
        {
            _storage[valueName] = value;
        }
        public void Add(string key, object value)
        {
            _storage.Add(key, value);
        }
        public void Add(KeyValuePair<string, object> pair)
        {
            _storage.Add(pair.Key, pair.Value);
        }
        public bool TryGet<T>(string valueName, out T value)
        {
            bool result = _storage.TryGetValue(valueName, out object rawValue);
            value = rawValue == null ? default : (T)rawValue;
            return result;
        }
        public IEnumerable<KeyValuePair<string, object>> GetAllConfigs()
        {
            return _storage;
        }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return GetAllConfigs().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAllConfigs().GetEnumerator();
        }

        private class EmptyConfig : IEcsWorldConfig
        {
            public int Count { get { return 0; } }
            public T Get<T>(string valueName) { return default; }
            public IEnumerable<KeyValuePair<string, object>> GetAllConfigs() { return Array.Empty<KeyValuePair<string, object>>(); }
            public bool Has(string valueName) { return false; }
            public bool TryGet<T>(string valueName, out T value) { value = default; return false; }
        }
    }
    public static class EcsWorldConfigExtensions
    {
        public static T GetOrDefault<T>(this IEcsWorldConfig self, string valueName, T defaultValue)
        {
            if (self.TryGet(valueName, out T value))
            {
                return value;
            }
            return defaultValue;
        }

        private const string ENTITIES_CAPACITY = nameof(ENTITIES_CAPACITY);
        private const int ENTITIES_CAPACITY_DEFAULT = 512;
        public static TConfig Set_EntitiesCapacity<TConfig>(this TConfig self, int value)
            where TConfig : IEcsWorldConfigWriter
        {
            self.Set(ENTITIES_CAPACITY, value);
            return self;
        }
        public static int Get_EntitiesCapacity(this IEcsWorldConfig self)
        {
            return self.GetOrDefault(ENTITIES_CAPACITY, ENTITIES_CAPACITY_DEFAULT);
        }

        //private const string RECYCLED_ENTITIES_CAPACITY = nameof(RECYCLED_ENTITIES_CAPACITY);
        //public static void Set_RecycledEntitiesCapacity(this IEcsWorldConfig self, int value)
        //{
        //    self.Set(RECYCLED_ENTITIES_CAPACITY, value);
        //}
        //public static int Get_RecycledEntitiesCapacity(this IEcsWorldConfig self)
        //{
        //    return self.GetOrDefault(RECYCLED_ENTITIES_CAPACITY, self.Get_EntitiesCapacity() / 2);
        //}

        private const string POOLS_CAPACITY = nameof(POOLS_CAPACITY);
        private const int POOLS_CAPACITY_DEFAULT = 512;
        public static TConfig Set_PoolsCapacity<TConfig>(this TConfig self, int value)
            where TConfig : IEcsWorldConfigWriter
        {
            self.Set(POOLS_CAPACITY, value);
            return self;
        }
        public static int Get_PoolsCapacity(this IEcsWorldConfig self)
        {
            return self.GetOrDefault(POOLS_CAPACITY, POOLS_CAPACITY_DEFAULT);
        }

        private const string COMPONENT_POOL_CAPACITY = nameof(COMPONENT_POOL_CAPACITY);
        private const int COMPONENT_POOL_CAPACITY_DEFAULT = 512;
        public static TConfig Set_PoolComponentsCapacity<TConfig>(this TConfig self, int value)
            where TConfig : IEcsWorldConfigWriter
        {
            self.Set(COMPONENT_POOL_CAPACITY, value);
            return self;
        }
        public static int Get_PoolComponentsCapacity(this IEcsWorldConfig self)
        {
            return self.GetOrDefault(COMPONENT_POOL_CAPACITY, COMPONENT_POOL_CAPACITY_DEFAULT);
        }

        private const string POOL_RECYCLED_COMPONENTS_CAPACITY = nameof(POOL_RECYCLED_COMPONENTS_CAPACITY);
        public static TConfig Set_PoolRecycledComponentsCapacity<TConfig>(this TConfig self, int value)
            where TConfig : IEcsWorldConfigWriter
        {
            self.Set(POOL_RECYCLED_COMPONENTS_CAPACITY, value);
            return self;
        }
        public static int Get_PoolRecycledComponentsCapacity(this IEcsWorldConfig self)
        {
            return self.GetOrDefault(POOL_RECYCLED_COMPONENTS_CAPACITY, self.Get_PoolComponentsCapacity() / 2);
        }
    }
}

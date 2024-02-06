using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsWorldConfig
    {
        bool IsLocked { get; }
        void Lock();
        void Set<T>(string valueName, T value);
        bool Has(string valueName);
        T Get<T>(string valueName);
        bool TryGet<T>(string valueName, out T value);
        void Remove(string valueName);
    }
    public class EcsWorldConfig : IEcsWorldConfig
    {
        private Dictionary<string, object> _storage = new Dictionary<string, object>();
        private bool _isLocked = false;
        public bool IsLocked { get { return _isLocked; } }
        public void Lock()
        {
            _isLocked = true;
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
            if (_isLocked)
            {
                throw new InvalidOperationException();
            }
            _storage.Remove(valueName);
        }
        public void Set<T>(string valueName, T value)
        {
            if (_isLocked)
            {
                throw new InvalidOperationException();
            }
            _storage[valueName] = value;
        }
        public bool TryGet<T>(string valueName, out T value)
        {
            bool result = _storage.TryGetValue(valueName, out object rawValue);
            value = rawValue == null ? default : (T)rawValue;
            return result;
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
            where TConfig : IEcsWorldConfig
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
            where TConfig : IEcsWorldConfig
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
            where TConfig : IEcsWorldConfig
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
            where TConfig : IEcsWorldConfig
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

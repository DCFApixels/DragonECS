#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IConfigContainer
    {
        int Count { get; }
        bool Has<T>();
        T Get<T>();
        bool TryGet<T>(out T value);
        IEnumerable<KeyValuePair<Type, object>> GetAllConfigs();
    }
    public interface IConfigContainerWriter
    {
        int Count { get; }
        void Set<T>(T value);
        void Set(Type type, object value);
        bool Has<T>();
        T Get<T>();
        bool TryGet<T>(out T value);
        void Remove<T>();
        IEnumerable<KeyValuePair<Type, object>> GetAllConfigs();
        IConfigContainer GetContainer();
    }
    public sealed class ConfigContainer : IConfigContainer, IConfigContainerWriter, IEnumerable<KeyValuePair<Type, object>>
    {
        public static readonly ConfigContainer Empty = new ConfigContainer();

        private Dictionary<Type, object> _storage = new Dictionary<Type, object>();

        public ConfigContainer() { }
        public ConfigContainer(IEnumerable<object> range)
        {
            foreach (var item in range)
            {
                _storage.Add(item.GetType(), item);
            }
        }
        public ConfigContainer(params object[] range)
        {
            foreach (var item in range)
            {
                _storage.Add(item.GetType(), item);
            }
        }

        public int Count
        {
            get { return _storage.Count; }
        }
        public T Get<T>()
        {
            return (T)_storage[typeof(T)];
        }
        public bool Has<T>()
        {
            return _storage.ContainsKey(typeof(T));
        }
        public void Remove<T>()
        {
            _storage.Remove(typeof(T));
        }
        public ConfigContainer Set<T>(T value)
        {
            _storage[typeof(T)] = value;
            return this;
        }
        public ConfigContainer Set(Type type, object value)
        {
            _storage[type] = value;
            return this;
        }
        void IConfigContainerWriter.Set(Type type, object value)
        {
            Set(type, value);
        }
        void IConfigContainerWriter.Set<T>(T value)
        {
            Set(value);
        }
        public bool TryGet<T>(out T value)
        {
            bool result = _storage.TryGetValue(typeof(T), out object rawValue);
            value = rawValue == null ? default : (T)rawValue;
            return result;
        }
        public IConfigContainer GetContainer() { return this; }
        public IEnumerable<KeyValuePair<Type, object>> GetAllConfigs()
        {
            return _storage;
        }
        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAllConfigs().GetEnumerator();
        }
    }
    public static class ConfigContainerExtensions
    {
        public static T GetOrDefault<T>(this IConfigContainer self, T defaultValue)
        {
            if (self.TryGet(out T value))
            {
                return value;
            }
            return defaultValue;
        }
        public static EcsWorldConfig GetWorldConfigOrDefault(this IConfigContainer self)
        {
            return self.GetOrDefault(EcsWorldConfig.Default);
        }
    }
}

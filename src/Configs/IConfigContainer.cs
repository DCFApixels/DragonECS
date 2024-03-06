using System.Collections;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IConfigContainer
    {
        int Count { get; }
        bool Has<T>();
        T Get<T>();
        bool TryGet<T>(out T value);
        IEnumerable<object> GetAllConfigs();
    }
    public interface IConfigContainerWriter
    {
        int Count { get; }
        void Set<T>(T value);
        bool Has<T>();
        T Get<T>();
        bool TryGet<T>(out T value);
        void Remove<T>();
        IEnumerable<object> GetAllConfigs();
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
    }

    public class DefaultConfigContainerBase : IConfigContainer, IConfigContainerWriter, IEnumerable<object>
    {
        private Dictionary<Type, object> _storage = new Dictionary<Type, object>();

        public DefaultConfigContainerBase() { }
        public DefaultConfigContainerBase(IEnumerable<object> range)
        {
            foreach (var item in range)
            {
                _storage.Add(item.GetType(), item);
            }
        }
        public DefaultConfigContainerBase(params object[] range)
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
        protected void SetInternal<T>(T value)
        {
            _storage[typeof(T)] = value;
        }
        void IConfigContainerWriter.Set<T>(T value)
        {
            SetInternal(value);
        }
        public bool TryGet<T>(out T value)
        {
            bool result = _storage.TryGetValue(typeof(T), out object rawValue);
            value = rawValue == null ? default : (T)rawValue;
            return result;
        }
        public IEnumerable<object> GetAllConfigs()
        {
            return _storage.Values;
        }
        public IEnumerator<object> GetEnumerator()
        {
            return _storage.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAllConfigs().GetEnumerator();
        }
    }

    public static class DefaultConfigContainerBaseExtensions
    {
        public static TContainer Set<TContainer, T>(this TContainer self, T value)
            where TContainer : IConfigContainerWriter
        {
            self.Set(value);
            return self;
        }
    }
}

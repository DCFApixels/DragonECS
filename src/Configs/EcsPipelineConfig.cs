using System;
using System.Collections;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsPipelineConfig : IConfig { }
    public interface IEcsPipelineConfigWriter : IConfigWriter
    {
        IEcsPipelineConfig GetPipelineConfig();
    }
    [Serializable]
    public class EcsPipelineConfig : IEcsPipelineConfigWriter, IEcsPipelineConfig, IEnumerable<KeyValuePair<string, object>>
    {
        public static readonly IEcsWorldConfig Empty = new EmptyConfig();

        private Dictionary<string, object> _storage = new Dictionary<string, object>();

        public EcsPipelineConfig() { }
        public EcsPipelineConfig(IEnumerable<KeyValuePair<string, object>> range)
        {
            _storage = new Dictionary<string, object>(range);
        }
        public EcsPipelineConfig(params KeyValuePair<string, object>[] range)
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

        public IEcsPipelineConfig GetPipelineConfig()
        {
            return this;
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
}

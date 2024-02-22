using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IConfig
    {
        int Count { get; }
        bool Has(string valueName);
        T Get<T>(string valueName);
        bool TryGet<T>(string valueName, out T value);
        IEnumerable<KeyValuePair<string, object>> GetAllConfigs();
    }
    public interface IConfigWriter
    {
        int Count { get; }
        void Set<T>(string valueName, T value);
        bool Has(string valueName);
        T Get<T>(string valueName);
        bool TryGet<T>(string valueName, out T value);
        void Remove(string valueName);
        IEnumerable<KeyValuePair<string, object>> GetAllConfigs();
    }
    public static class ConfigExtensions
    {
        public static T GetOrDefault<T>(this IConfig self, string valueName, T defaultValue)
        {
            if (self.TryGet(valueName, out T value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}

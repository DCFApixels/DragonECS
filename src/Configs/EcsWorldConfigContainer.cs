using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public class EcsWorldConfig
    {
        public readonly int EntitiesCapacity;
        public readonly int GroupCapacity;
        public readonly int PoolsCapacity;
        public readonly int PoolComponentsCapacity;
        public readonly int PoolRecycledComponentsCapacity;
        public EcsWorldConfig(int entitiesCapacity = 512, int groupCapacity = 512, int poolsCapacity = 512, int poolComponentsCapacity = 512, int poolRecycledComponentsCapacity = 512 / 2)
        {
            EntitiesCapacity = entitiesCapacity;
            GroupCapacity = groupCapacity;
            PoolsCapacity = poolsCapacity;
            PoolComponentsCapacity = poolComponentsCapacity;
            PoolRecycledComponentsCapacity = poolRecycledComponentsCapacity;
        }
    }
    public interface IEcsWorldConfigContainer : IConfigContainer { }
    public interface IEcsWorldConfigContainerWriter : IConfigContainerWriter
    {
        IEcsWorldConfigContainer GetWorldConfigs();
    }
    [Serializable]
    public class EcsWorldConfigContainer : DefaultConfigContainerBase, IEcsWorldConfigContainerWriter, IEcsWorldConfigContainer, IEnumerable<object>
    {
        public static readonly IEcsWorldConfigContainer Defaut;
        static EcsWorldConfigContainer()
        {
            var container = new EcsWorldConfigContainer();
            container.Set(new EcsWorldConfig());
            Defaut = container;
        }
        public EcsWorldConfigContainer Set<T>(T value)
        {
            SetInternal(value);
            return this;
        }
        public IEcsWorldConfigContainer GetWorldConfigs()
        {
            return this;
        }
    }
    public static class EcsWorldConfigExtensions
    {
        public static T GetOrDefault<T>(this IEcsWorldConfigContainer self, T defaultValue)
        {
            if (self.TryGet(out T value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}

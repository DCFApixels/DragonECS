using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsPipelineConfigContainer : IConfigContainer { }
    public interface IEcsPipelineConfigContainerWriter : IConfigContainerWriter
    {
        IEcsPipelineConfigContainer GetPipelineConfigs();
    }
    [Serializable]
    public class EcsPipelineConfigContainer : DefaultConfigContainerBase, IEcsPipelineConfigContainerWriter, IEcsPipelineConfigContainer, IEnumerable<object>
    {
        public static readonly IEcsPipelineConfigContainer Defaut;
        static EcsPipelineConfigContainer()
        {
            var container = new EcsPipelineConfigContainer();
            Defaut = container;
        }
        public EcsPipelineConfigContainer Set<T>(T value)
        {
            SetInternal(value);
            return this;
        }
        public IEcsPipelineConfigContainer GetPipelineConfigs()
        {
            return this;
        }
    }
}

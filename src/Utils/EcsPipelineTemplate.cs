using System;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    [Serializable]
    [DataContract]
    public class EcsPipelineTemplate : IEcsModule
    {
        [DataMember] public string[] layers;
        [DataMember] public AddCommand[] systems;
        void IEcsModule.Import(EcsPipeline.Builder b)
        {
            b.Layers.MergeWith(layers);
            foreach (var s in systems)
            {
                if (s.target == null) { continue; }

                b.Add(s.target, s.parameters);
            }
        }
        [Serializable]
        [DataContract]
        public struct AddCommand
        {
            [DataMember] public object target;
            [DataMember] public AddParams parameters;
            public AddCommand(object target, AddParams parameters)
            {
                this.target = target;
                this.parameters = parameters;
            }
        }
    }
}

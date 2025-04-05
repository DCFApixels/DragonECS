#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    using static EcsConsts;

    [Serializable]
    [DataContract]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaDescription(AUTHOR, "...")]
    [MetaGroup(PACK_GROUP, OTHER_GROUP)]
    [MetaID("DragonECS_128D547C9201EEAC49B05F89E4A253DF")]
    [MetaColor(MetaColor.DragonRose)]
    public class EcsPipelineTemplate : IEcsModule
    {
        [DataMember] public string[] layers;
        [DataMember] public Record[] records;
        void IEcsModule.Import(EcsPipeline.Builder b)
        {
            b.Layers.MergeWith(layers);
            foreach (var s in records)
            {
                if (s.target == null) { continue; }

                b.Add(s.target, s.parameters);
            }
        }
        [Serializable]
        [DataContract]
        public struct Record
        {
            [DataMember] public object target;
            [DataMember] public AddParams parameters;
            public Record(object target, AddParams parameters)
            {
                this.target = target;
                this.parameters = parameters;
            }
        }
    }
}

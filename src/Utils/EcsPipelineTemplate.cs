using System;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    [Serializable]
    [DataContract]
    public class EcsPipelineTemplate : IEcsModule
    {
        [DataMember] public string[] layers;
        [DataMember] public SystemRecord[] systems;
        void IEcsModule.Import(EcsPipeline.Builder b)
        {
            b.Layers.MergeWith(layers);
            foreach (var s in systems)
            {
                if (s.system == null) { continue; }

                int? sortOrder = s.isCustomSortOrder ? s.sortOrder : default(int?);
                if (s.isUnique)
                {
                    b.AddUnique(s.system, s.layer, sortOrder);
                }
                else
                {
                    b.Add(s.system, s.layer, sortOrder);
                }
            }
        }

        [Serializable]
        [DataContract]
        public struct SystemRecord
        {
            [DataMember] public IEcsProcess system;
            [DataMember] public string layer;
            [DataMember] public int sortOrder;
            [DataMember] public bool isCustomSortOrder;
            [DataMember] public bool isUnique;
            public int? NullableSortOrder { get { return isCustomSortOrder ? sortOrder : default(int?); } }
            public SystemRecord(IEcsProcess system, string layer, int? sortOrder, bool isUnique)
            {
                this.system = system;
                this.layer = layer;
                this.sortOrder = sortOrder.HasValue ? sortOrder.Value : 0;
                isCustomSortOrder = sortOrder.HasValue;
                this.isUnique = isUnique;
            }
        }
    }
}

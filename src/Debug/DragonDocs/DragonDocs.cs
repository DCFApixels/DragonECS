using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS.Docs
{
    [Serializable]
    [DataContract]
    public class DragonDocs
    {
        [DataMember]
        private readonly DragonDocsMeta[] _metas;
        public ReadOnlySpan<DragonDocsMeta> Meta
        {
            get { return new ReadOnlySpan<DragonDocsMeta>(_metas); }
        }

        private DragonDocs(DragonDocsMeta[] metas)
        {
            _metas = metas;
        }

        public static DragonDocs Generate()
        {
            List<DragonDocsMeta> metas = new List<DragonDocsMeta>(256);
            foreach (var type in GetTypes())
            {
                metas.Add(new DragonDocsMeta(type.ToMeta()));
            }
            DragonDocsMeta[] array = metas.ToArray();
            Array.Sort(array);
            return new DragonDocs(array);
        }
        private static List<Type> GetTypes()
        {
            Type metaAttributeType = typeof(EcsMetaAttribute);
            Type memberType = typeof(IEcsMember);
            List<Type> result = new List<Type>(512);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (memberType.IsAssignableFrom(type) || Attribute.GetCustomAttributes(type, metaAttributeType, false).Length > 1)
                    {
                        result.Add(type);
                    }
                }
            }
            return result;
        }
    }
}
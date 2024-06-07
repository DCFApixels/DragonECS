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
        [NonSerialized]
        private DragonDocsMetaGroup[] _mapping = null;

        public ReadOnlySpan<DragonDocsMeta> Meta
        {
            get { return new ReadOnlySpan<DragonDocsMeta>(_metas); }
        }
        public ReadOnlySpan<DragonDocsMetaGroup> Mapping
        {
            get
            {
                Init();
                return new ReadOnlySpan<DragonDocsMetaGroup>(_mapping); ;
            }
        }
        private bool _isInit = false;
        private void Init()
        {
            if (_isInit) { return; }

            if (_metas.Length < 0)
            {
                _mapping = Array.Empty<DragonDocsMetaGroup>();
            }
            List<DragonDocsMetaGroup> groups = new List<DragonDocsMetaGroup>();
            string name = _metas[0].Name;
            int startIndex = 0;
            for (int i = 1; i < _metas.Length; i++)
            {
                var meta = _metas[i];
                if (name != meta.Name)
                {
                    groups.Add(new DragonDocsMetaGroup(name, startIndex, i - startIndex));
                    name = meta.Name;
                    startIndex = i;
                }
            }
            groups.Add(new DragonDocsMetaGroup(name, startIndex, _metas.Length - startIndex));
            _mapping = groups.ToArray();
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
    public struct DragonDocsMetaGroup
    {
        public readonly string Name;
        public readonly int StartIndex;
        public readonly int Length;
        public DragonDocsMetaGroup(string name, int startIndex, int length)
        {
            Name = name;
            StartIndex = startIndex;
            Length = length;
        }
    }

}
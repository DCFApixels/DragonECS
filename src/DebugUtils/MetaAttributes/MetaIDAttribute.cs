using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaIDAttribute : EcsMetaAttribute
    {
        private static HashSet<string> _ids;

        public readonly string ID;
        public MetaIDAttribute(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Throw.ArgumentNull(nameof(id));
            }
            if (_ids.Add(id))
            {
                //TODO перевести ексепшен
                Throw.ArgumentException($"Дублирование MetaID: {ID}");
            }
            ID = id;
        }
    }
}

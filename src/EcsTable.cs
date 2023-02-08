using System;
using System.Collections.Generic;
using System.Linq;

namespace DCFApixels.DragonECS
{
    public abstract class EcsTable
    {
        internal EcsFilter _filter;

        public EcsTable(ref TableBuilder builder) { }

        public EcsFilter Filter
        {
            get => _filter;
        }

    }
}


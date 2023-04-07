using DCFApixels.DragonECS;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsRealationTable
    {
    }
    internal class EcsRelationWorld<TRelationTableArhetype> : EcsWorld<EcsRelationWorld<TRelationTableArhetype>>
         where TRelationTableArhetype : EcsRelationTableArchetypeBase { }
    public sealed class EcsRelationTable<TTableArhetype> : IEcsRealationTable
         where TTableArhetype : EcsRelationTableArchetypeBase
    {
        public readonly IEcsWorld leftWorld;
        public readonly IEcsWorld rightWorld;

        private int[] _relations; //dense
        private int[] _leftMapping;
        private int[] _rgihtMapping;

        private EcsRelationWorld<TTableArhetype> _relationWorld;
    }
}

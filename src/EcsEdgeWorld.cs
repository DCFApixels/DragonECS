using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsEdgeWorld<TWorldArchetype> : EcsWorld<TWorldArchetype> where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        private EcsWorld _firstTarget;
        private EcsWorld _secondTarget;
        public EcsEdgeWorld(EcsWorld firstTarget, EcsWorld secondTarget, EcsPipeline pipeline) : base(pipeline)
        {
            _firstTarget = firstTarget;
            _secondTarget = secondTarget;
        }
        public EcsEdgeWorld(EcsWorld firstTarget, EcsPipeline pipeline) : base(pipeline)
        {
            _firstTarget = firstTarget;
        }
    }
}

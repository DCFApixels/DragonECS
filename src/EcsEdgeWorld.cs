using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsEdgeWorld<TWorldArchetype> : EcsWorld<TWorldArchetype> where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        private IEcsWorld _firstTarget;
        private IEcsWorld _secondTarget;
        public EcsEdgeWorld(IEcsWorld firstTarget, IEcsWorld secondTarget, EcsPipeline pipeline) : base(pipeline)
        {
            _firstTarget = firstTarget;
            _secondTarget = secondTarget;
        }
        public EcsEdgeWorld(IEcsWorld firstTarget, EcsPipeline pipeline) : base(pipeline)
        {
            _firstTarget = firstTarget;
        }
    }
}

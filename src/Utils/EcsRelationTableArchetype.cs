using System;

namespace DCFApixels.DragonECS.Internal
{
    public abstract class EcsRelationTableArchetypeBase
    {
        public EcsRelationTableArchetypeBase()
        {
            throw new TypeAccessException("Сreating instances of EcsRelationTableArchetype class is not available.");
        }
    }
    public sealed class EcsRelationTableArchetype<TLeftWorld, TRightWorld> : EcsRelationTableArchetypeBase
        where TLeftWorld : EcsWorld<TLeftWorld>
        where TRightWorld : EcsWorld<TRightWorld>
    { }
}

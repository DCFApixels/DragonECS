using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [DebugColor(DebugColor.White)]
    public struct Parent : IEcsAttachComponent
    {
        public entlong entity;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public entlong Target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => entity = value;
        }
    }

    public static class ParentUtility
    {
       // public static int GetRootOrSelf(this HierarchySubject s, int entityID) => s.parents.GetRootOrSelf(entityID);
        public static int GetRootOrSelf(this EcsAttachPool<Parent> parents, int entityID)
        {
            while (parents.Has(entityID) && parents.Read(entityID).entity.TryGetID(out int child))
                entityID = child;
            return entityID;
        }
       // public static bool IsRoot(this HierarchySubject s, int entityID) => s.parents.IsRoot(entityID);
        public static bool IsRoot(this EcsAttachPool<Parent> parents, int entityID)
        {
            return !(parents.Has(entityID) && parents.Read(entityID).entity.IsAlive);
        }

       // public static bool TryGetRoot(this HierarchySubject s, int entityID, out int rootEntityID) => TryGetRoot(s.parents, entityID, out rootEntityID);
        public static bool TryGetRoot(this EcsAttachPool<Parent> parents, EcsSubject conditionSubject, int entityID, out int rootEntityID)
        {
            rootEntityID = entityID;
            while (parents.Has(rootEntityID) && parents.Read(rootEntityID).entity.TryGetID(out int child) && !conditionSubject.IsMatches(child))
                rootEntityID = child;
            return rootEntityID != entityID;
        }
        public static bool TryGetRoot(this EcsAttachPool<Parent> parents, int entityID, out int rootEntityID)
        {
            rootEntityID = entityID;
            while (parents.Has(rootEntityID) && parents.Read(rootEntityID).entity.TryGetID(out int child))
                rootEntityID = child;
            return rootEntityID != entityID;
        }

        public static bool TryFindParentWithSubject(this EcsAttachPool<Parent> parents, EcsSubject conditionSubject, int entityID, out int resultEntityID)
        {
            resultEntityID = entityID;
            while (parents.Has(resultEntityID) && parents.Read(resultEntityID).entity.TryGetID(out int child) && !conditionSubject.IsMatches(resultEntityID))
                resultEntityID = child;
            return conditionSubject.IsMatches(resultEntityID);
        }
        public static bool TryFindParentWith<TComponent>(this EcsAttachPool<Parent> parents, int entityID, out int resultEntityID) where TComponent : struct
        {
            var pool = parents.World.AllPools[parents.World.GetComponentID<TComponent>()];
            resultEntityID = entityID;
            while (!pool.Has(resultEntityID) &&
                parents.Has(resultEntityID) &&
                parents.Read(resultEntityID).entity.TryGetID(out int child))
            {
                resultEntityID = child;
            }
            return pool.Has(resultEntityID);
        }
    }
}

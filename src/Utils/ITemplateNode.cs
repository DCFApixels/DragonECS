#if DISABLE_DEBUG
#undef DEBUG
#endif

using DCFApixels.DragonECS.Core.Unchecked;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface ITemplateNode
    {
        void Apply(short worldID, int entityID);
    }
    public static class ITemplateNodeExtensions
    {
        public static int ApplyAndReturn(this ITemplateNode self, short worldID, int entityID)
        {
            self.Apply(worldID, entityID);
            return entityID;
        }
        public static entlong ApplyAndReturnLong(this ITemplateNode self, short worldID, int entityID)
        {
            self.Apply(worldID, entityID);
            return (EcsWorld.GetWorld(worldID), entityID);
        }
        public static void Apply(this ITemplateNode self, EcsWorld world, int entityID)
        {
            self.Apply(world.ID, entityID);
        }
        public static int ApplyAndReturn(this ITemplateNode self, EcsWorld world, int entityID)
        {
            self.Apply(world.ID, entityID);
            return entityID;
        }
        public static entlong ApplyAndReturnLong(this ITemplateNode self, EcsWorld world, int entityID)
        {
            self.Apply(world.ID, entityID);
            return (world, entityID);
        }


        #region new entity
        public static int NewEntity(this EcsWorld world, ITemplateNode template)
        {
            int e = world.NewEntity();
            template.Apply(world.ID, e);
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, ITemplateNode template)
        {
            entlong e = world.NewEntityLong();
            template.Apply(world.ID, e.ID);
            return e;
        }
        public static int NewEntity(this EcsWorld world, ReadOnlySpan<ITemplateNode> templates)
        {
            int e = world.NewEntity();
            foreach (var template in templates)
            {
                template.Apply(world.ID, e);
            }
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, ReadOnlySpan<ITemplateNode> templates)
        {
            entlong e = world.NewEntityLong();
            var info = (RawEntLong)e;
            foreach (var template in templates)
            {
                template.Apply(world.ID, info.id);
            }
            return e;
        }
        public static int NewEntity(this EcsWorld world, IEnumerable<ITemplateNode> templates, bool _ = false)
        {
            int e = world.NewEntity();
            foreach (var template in templates)
            {
                template.Apply(world.ID, e);
            }
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, IEnumerable<ITemplateNode> templates, bool _ = false)
        {
            entlong e = world.NewEntityLong();
            var info = (RawEntLong)e;
            foreach (var template in templates)
            {
                template.Apply(world.ID, info.id);
            }
            return e;
        }
        #endregion


        #region new entity concrete
        public static int NewEntity(this EcsWorld world, int entityID, ITemplateNode template)
        {
            int e = world.NewEntity(entityID);
            template.Apply(world.ID, e);
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, int entityID, ITemplateNode template)
        {
            entlong e = world.NewEntityLong(entityID);
            template.Apply(world.ID, e.ID);
            return e;
        }
        public static int NewEntity(this EcsWorld world, int entityID, ReadOnlySpan<ITemplateNode> templates)
        {
            int e = world.NewEntity(entityID);
            foreach (var template in templates)
            {
                template.Apply(world.ID, e);
            }
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, int entityID, ReadOnlySpan<ITemplateNode> templates)
        {
            entlong e = world.NewEntityLong(entityID);
            var info = (RawEntLong)e;
            foreach (var template in templates)
            {
                template.Apply(world.ID, info.id);
            }
            return e;
        }
        public static int NewEntity(this EcsWorld world, int entityID, IEnumerable<ITemplateNode> templates, bool _ = false)
        {
            int e = world.NewEntity(entityID);
            foreach (var template in templates)
            {
                template.Apply(world.ID, e);
            }
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, int entityID, IEnumerable<ITemplateNode> templates, bool _ = false)
        {
            entlong e = world.NewEntityLong(entityID);
            var info = (RawEntLong)e;
            foreach (var template in templates)
            {
                template.Apply(world.ID, info.id);
            }
            return e;
        }
        #endregion
    }
}
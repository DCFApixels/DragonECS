using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [DebugHide, DebugColor(DebugColor.Black)]
    public class SystemsBlockMarkerSystem : IEcsSystem
    {
        public readonly string name;
        public SystemsBlockMarkerSystem(string name) { this.name = name; }
    }

    [DebugHide, DebugColor(DebugColor.Grey)]
    public class DeleteEmptyEntitesSystem : IEcsRunProcess, IEcsPreInject
    {
        private List<EcsWorld> _worlds = new List<EcsWorld>();
        public void PreInject(object obj)
        {
            if (obj is EcsWorld world)
                _worlds.Add(world);
        }
        public void Run(EcsPipeline pipeline)
        {
            foreach (var world in _worlds)
                world.DeleteEmptyEntites();
        }
    }

    [DebugHide, DebugColor(DebugColor.Grey)]
    public class DeleteOneFrameComponentSystem<TWorld, TComponent> : IEcsRunProcess, IEcsInject<TWorld>
        where TWorld : EcsWorld<TWorld>
        where TComponent : struct, IEcsComponent
    {
        private TWorld _world;
        public void Inject(TWorld obj) => _world = obj;
        private sealed class Subject : EcsSubject
        {
            public EcsPool<TComponent> pool;
            public Subject(Builder b)
            {
                pool = b.Include<TComponent>();
            }
        }
        public void Run(EcsPipeline pipeline)
        {
            foreach (var e in _world.Where(out Subject s))
                s.pool.Del(e);
        }
    }

    public static class DeleteOneFrameComponentSystemExt
    {
        private const string AUTO_DEL_LAYER = nameof(AUTO_DEL_LAYER);
        public static EcsPipeline.Builder AutoDel<TWorld, TComponent>(this EcsPipeline.Builder b)
            where TWorld : EcsWorld<TWorld>
            where TComponent : struct, IEcsComponent
        {
            b.Layers.Insert(EcsConsts.POST_END_LAYER, AUTO_DEL_LAYER);
            b.AddUnique(new DeleteOneFrameComponentSystem<TWorld, TComponent>(), AUTO_DEL_LAYER);
            return b;
        }
        /// <summary>for EcsDefaultWorld</summary>
        public static EcsPipeline.Builder AutoDel<TComponent>(this EcsPipeline.Builder b)
            where TComponent : struct, IEcsComponent
        {
            b.Layers.Insert(EcsConsts.POST_END_LAYER, AUTO_DEL_LAYER);
            b.AddUnique(new DeleteOneFrameComponentSystem<EcsDefaultWorld, TComponent>(), AUTO_DEL_LAYER);
            return b;
        }
    }
}

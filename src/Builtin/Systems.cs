using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [DebugColor(DebugColor.Black)]
    public class SystemsBlockMarkerSystem : IEcsSystem
    {
        public readonly string name;

        public SystemsBlockMarkerSystem(string name)
        {
            this.name = name;
        }
    }
    [DebugHide, DebugColor(DebugColor.Grey)]
    public class DeleteEmptyEntitesSsytem : IEcsRunSystem, IEcsPreInject
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

    public class DeleteOneFrameComponentSystem<TWorld, TComponent> : IEcsRunSystem, IEcsInject<TWorld>
        where TWorld : EcsWorld<TWorld>
        where TComponent : struct, IEcsComponent
    {
        private TWorld _world;
        public void Inject(TWorld obj) => _world = obj;

        private sealed class Query : EcsQuery
        {
            public EcsPool<TComponent> pool;
            public Query(Builder b)
            {
                pool = b.Include<TComponent>();
            }
        }
        public void Run(EcsPipeline pipeline)
        {
            foreach (var e in _world.Where(out Query q))
                q.pool.Del(e);
        }
    }

    public static class DeleteOneFrameComponentSystemExt
    {
        public static EcsPipeline.Builder AutoDel<TWorld, TComponent>(this EcsPipeline.Builder b)
            where TWorld : EcsWorld<TWorld>
            where TComponent : struct, IEcsComponent
        {
            b.Add(new DeleteOneFrameComponentSystem<TWorld, TComponent>());
            return b;
        }
        /// <summary> for EcsDefaultWorld </summary>
        public static EcsPipeline.Builder AutoDel<TComponent>(this EcsPipeline.Builder b)
            where TComponent : struct, IEcsComponent
        {
            b.Add(new DeleteOneFrameComponentSystem<EcsDefaultWorld, TComponent>());
            return b;
        }
    }
}

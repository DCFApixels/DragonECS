using DCFApixels.DragonECS.Internal;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    namespace Internal
    {
        [DebugHide, DebugColor(DebugColor.Black)]
        public class SystemsLayerMarkerSystem : IEcsProcess
        {
            public readonly string name;
            public SystemsLayerMarkerSystem(string name) => this.name = name;
        }
        [DebugHide, DebugColor(DebugColor.Grey)]
        public class DeleteEmptyEntitesSystem : IEcsRunProcess, IEcsInject<EcsWorld>
        {
            private List<EcsWorld> _worlds = new List<EcsWorld>();
            public void Inject(EcsWorld obj) => _worlds.Add(obj);
            public void Run(EcsPipeline pipeline)
            {
                foreach (var world in _worlds)
                    world.DeleteEmptyEntites();
            }
        }
        [DebugHide, DebugColor(DebugColor.Grey)]
        public class DeleteOneFrameComponentSystem<TComponent> : IEcsRunProcess, IEcsInject<EcsWorld>
            where TComponent : struct, IEcsComponent
        {
            private sealed class Subject : EcsSubject
            {
                public EcsPool<TComponent> pool;
                public Subject(Builder b) => pool = b.Include<TComponent>();
            }
            List<EcsWorld> _worlds = new List<EcsWorld>();
            public void Inject(EcsWorld obj) => _worlds.Add(obj);
            public void Run(EcsPipeline pipeline)
            {
                for (int i = 0, iMax = _worlds.Count; i < iMax; i++)
                {
                    EcsWorld world = _worlds[i];
                    if (world.IsComponentTypeDeclared<TComponent>())
                    {
                        foreach (var e in world.Where(out Subject s))
                            s.pool.Del(e);
                    }
                }
            }
        }
    }
    public static class DeleteOneFrameComponentSystemExtensions
    {
        private const string AUTO_DEL_LAYER = nameof(AUTO_DEL_LAYER);
        public static EcsPipeline.Builder AutoDel<TComponent>(this EcsPipeline.Builder b, string layerName = AUTO_DEL_LAYER)
            where TComponent : struct, IEcsComponent
        {
            if(AUTO_DEL_LAYER == layerName)
                b.Layers.Insert(EcsConsts.POST_END_LAYER, AUTO_DEL_LAYER);
            b.AddUnique(new DeleteOneFrameComponentSystem<TComponent>(), layerName);
            return b;
        }
    }
}

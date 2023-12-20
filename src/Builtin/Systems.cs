using DCFApixels.DragonECS.Internal;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    namespace Internal
    {
        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Black)]
        public class SystemsLayerMarkerSystem : IEcsProcess
        {
            public readonly string name;
            public SystemsLayerMarkerSystem(string name) => this.name = name;
        }
        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Grey)]
        public class EndFrameSystem : IEcsRunProcess, IEcsInject<EcsWorld>
        {
            private readonly List<EcsWorld> _worlds = new List<EcsWorld>();
            public void Inject(EcsWorld obj) => _worlds.Add(obj);
            public void Run()
            {
                foreach (var world in _worlds)
                {
                    world.DeleteEmptyEntites();
                    world.ReleaseDelEntityBuffer();
                }
            }
        }
        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Grey)]
        public class DeleteOneFrameComponentSystem<TComponent> : IEcsRunProcess, IEcsInject<EcsWorld>
            where TComponent : struct, IEcsComponent
        {
            public EcsPipeline pipeline { get; set; }
            private sealed class Aspect : EcsAspect
            {
                public EcsPool<TComponent> pool;
                public Aspect(Builder b) => pool = b.Include<TComponent>();
            }
            private readonly List<EcsWorld> _worlds = new List<EcsWorld>();
            public void Inject(EcsWorld obj) => _worlds.Add(obj);
            public void Run()
            {
                for (int i = 0, iMax = _worlds.Count; i < iMax; i++)
                {
                    EcsWorld world = _worlds[i];
                    if (world.IsComponentTypeDeclared<TComponent>())
                    {
                        foreach (var e in world.Where(out Aspect a))
                            a.pool.Del(e);
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
            if (AUTO_DEL_LAYER == layerName)
                b.Layers.InsertAfter(EcsConsts.POST_END_LAYER, AUTO_DEL_LAYER);
            b.AddUnique(new DeleteOneFrameComponentSystem<TComponent>(), layerName);
            return b;
        }
    }
}

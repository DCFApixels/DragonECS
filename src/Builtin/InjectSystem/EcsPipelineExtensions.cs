using DCFApixels.DragonECS.DI.Internal;

namespace DCFApixels.DragonECS
{
    public static partial class EcsPipelineExtensions
    {
        public static void Inject<T>(this EcsPipeline self, T data)
        {
            self.GetRunner<IEcsInject<T>>().Inject(data);
        }
    }
    public static partial class EcsPipelineBuilderExtensions
    {
        public static EcsPipeline.Builder AddInjectionGraph(this EcsPipeline.Builder self, InjectionGraph graph)
        {
            self.Config.Set(InjectionGraph.CONFIG_NAME, graph);
            return self;
        }
        public static EcsPipeline.Builder GetInjectionGraph(this EcsPipeline.Builder self, out InjectionGraph graph)
        {
            graph = self.Config.Get<InjectionGraph>(InjectionGraph.CONFIG_NAME);
            return self;
        }
        public static EcsPipeline.Builder Inject<T>(this EcsPipeline.Builder self, T data)
        {
            if (data == null)
            {
                Throw.ArgumentNull();
            }
            self.Add(new InitInjectionSystem<T>(data));
            if (data is IEcsModule module)
            {
                self.AddModule(module);
            }
            return self;
        }
        public static EcsPipeline.Builder Inject<T0, T1>(this EcsPipeline.Builder self, T0 d0, T1 d1)
        {
            return self.Inject(d0).Inject(d1);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2)
        {
            return self.Inject(d0).Inject(d1).Inject(d2);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 f)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(f);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5, T6>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 f, T6 d6)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(f).Inject(d6);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5, T6, T7>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 f, T6 d6, T7 d7)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(f).Inject(d6).Inject(d7);
        }
    }
}
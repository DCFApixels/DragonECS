#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;

namespace DCFApixels.DragonECS
{
    public static partial class EcsPipelineBuilderExtensions
    {
        public static EcsPipeline.Builder Inject<T>(this EcsPipeline.Builder self, T data)
        {
            if (data == null) { Throw.ArgumentNull(); }
            self.Injector.Inject(data);
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
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 d5)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(d5);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5, T6>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 d5, T6 d6)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(d5).Inject(d6);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5, T6, T7>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 d5, T6 d6, T7 d7)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(d5).Inject(d6).Inject(d7);
        }
    }
}
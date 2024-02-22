namespace DCFApixels.DragonECS
{
    public readonly struct BlockInjector
    {
        private readonly Injector _injectionGraph;
        public BlockInjector(Injector injectionGraph)
        {
            _injectionGraph = injectionGraph;
        }
        public void Inject<T>(T obj)
        {
            _injectionGraph.Inject(obj);
        }
//        public void InjectNoBoxing<T>(T data) where T : struct
//        {
//            _injectionGraph.InjectNoBoxing(data);
//        }
//#if !REFLECTION_DISABLED
//        public void InjectRaw(object obj)
//        {
//            _injectionGraph.InjectRaw(obj);
//        }
//#endif
    }
    public interface IInjectionBlock
    {
        void InjectTo(BlockInjector i);
    }
}

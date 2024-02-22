namespace DCFApixels.DragonECS
{
    public readonly struct Injector
    {
        private readonly InjectionGraph _injectionGraph;
        public Injector(InjectionGraph injectionGraph)
        {
            _injectionGraph = injectionGraph;
        }
        public void Inject<T>(T obj)
        {
            _injectionGraph.Inject(obj);
        }
        public void InjectNoBoxing<T>(T data)
        {
            _injectionGraph.InjectNoBoxing(data);
        }
#if !REFLECTION_DISABLED
        public void InjectRaw(object obj)
        {
            _injectionGraph.InjectRaw(obj);
        }
#endif
    }
    public interface IInjectionBlock
    {
        void InjectTo(Injector i);
    }
}

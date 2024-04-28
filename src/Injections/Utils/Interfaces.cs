namespace DCFApixels.DragonECS
{
    public interface IInjectionBlock
    {
        void InjectTo(Injector inj);
    }
    public readonly struct InjectionBranchIniter
    {
        private readonly Injector _injector;
        public InjectionBranchIniter(Injector injector)
        {
            _injector = injector;
        }
        public void AddNode<T>()
        {
            _injector.AddNode<T>();
        }
    }
    public interface IInjectionUnit
    {
        void OnInitInjectionBranch(InjectionBranchIniter initer);
    }
}

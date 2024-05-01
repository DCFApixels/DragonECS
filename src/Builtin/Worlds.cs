using System.Diagnostics;

namespace DCFApixels.DragonECS
{
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_NAME)]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsDefaultWorld : EcsWorld, IInjectionUnit
    {
        public EcsDefaultWorld(EcsWorldConfig config, short worldID = -1) : base(config, worldID) { }
        public EcsDefaultWorld(IConfigContainer configs = null, short worldID = -1) : base(configs, worldID) { }
        void IInjectionUnit.OnInitInjectionBranch(InjectionBranchIniter initer)
        {
            initer.AddNode<EcsDefaultWorld>();
        }
    }
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_NAME)]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsEventWorld : EcsWorld, IInjectionUnit
    {
        public EcsEventWorld(EcsWorldConfig config, short worldID = -1) : base(config, worldID) { }
        public EcsEventWorld(IConfigContainer configs = null, short worldID = -1) : base(configs, worldID) { }
        void IInjectionUnit.OnInitInjectionBranch(InjectionBranchIniter initer)
        {
            initer.AddNode<EcsDefaultWorld>();
        }
    }
}

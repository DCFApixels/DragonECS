using System.Diagnostics;

namespace DCFApixels.DragonECS
{
    /// <summary> EcsWrold for store regular game entities. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.WORLDS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Inherits EcsWorld without extending its functionality and is used for specific injections. Can be used to store regular game entities, can also be used as a single world in the game for all entities.")]
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
    /// <summary> EcsWrold for store event entities. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.WORLDS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Inherits EcsWorld without extending its functionality and is used for specific injections. Can be used to store event entities.")]
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

using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsTableMember
    {
        public int PoolID { get; }
    }
    public interface IEcsMemberCachePool<TSelf, T> : IEcsTableMember
        where TSelf: struct, IEcsTableMember
        where T :struct
    {
        public EcsPool<T> Pool { get; }
        public void Inject(out TSelf self, EcsPool<T> pool);
    }
}

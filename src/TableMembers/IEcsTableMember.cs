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
    {
        public EcsFieldPool<T> Pool { get; }
        public void Inject(out TSelf self, EcsFieldPool<T> pool);
    }
}

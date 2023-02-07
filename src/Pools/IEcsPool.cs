using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public interface IEcsPool
    {
        public EcsWorld World { get; }
        public int ID { get; }
        public EcsType Type { get; }
        public bool Has(int index);
        public void Add(int index);
        public void Del(int index);
    }
}

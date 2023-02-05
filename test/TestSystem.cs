using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class TestSystem : IEcsDo<_Init>, IEcsDo<_Run>, IEcsDo<_Destroy>
    {
        void IEcsDo<_Init>.Do(EcsSession engine)
        {
        }

        void IEcsDo<_Run>.Do(EcsSession engine)
        {
        }

        void IEcsDo<_Destroy>.Do(EcsSession engine)
        {
        }
    }
}

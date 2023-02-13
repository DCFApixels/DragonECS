using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class TestSystem : 
        IReceive<_OnInject<SharedData>>, 
        IDo<_Init>, IDo<_Run>, IDo<_Destroy>
    {
        private SharedData _sharedData;
        void IReceive<_OnInject<SharedData>>.Do(EcsSession session, in _OnInject<SharedData> m) => _sharedData = m.data;


        void IDo<_Init>.Do(EcsSession session)
        {
        }

        void IDo<_Run>.Do(EcsSession session)
        {
            
        }

        void IDo<_Destroy>.Do(EcsSession session)
        {
        }

    }
}

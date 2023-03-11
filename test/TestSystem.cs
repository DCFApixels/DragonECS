using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class TestSystem :
        IEcsInject<SharedData>,
        IEcsSimpleCycleSystem
    {
        private SharedData _sharedData;
        public void Inject(SharedData obj) => _sharedData = obj;



        public void Init(EcsSession session)
        {
        }
        public void Run(EcsSession session)
        {
        }
        public void Destroy(EcsSession session)
        {
        }
    }
}

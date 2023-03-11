using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class TestSystem : IEcsInject<SharedData>, IEcsSimpleCycleSystem
    {
        private SharedData _sharedData;
        public void Inject(SharedData obj) => _sharedData = obj;

        public void Init(EcsSession session)
        {
            Debug.Log("Init");
        }
        public void Run(EcsSession session)
        {
            Debug.Log("Run");
        }
        public void Destroy(EcsSession session)
        {
            Debug.Log("Destroy");
        }
    }
}

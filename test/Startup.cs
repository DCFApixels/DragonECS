using System;
using System.Collections.Generic;
using DCFApixels;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class Startup : MonoBehaviour
    {

        private EcsSession _ecsSession;

        private void Start()
        {
            _ecsSession
                .AddWorld("")
                .Add(new TestSystem())
                .Init();
        }

        private void Update()
        {
            _ecsSession.Run();
        }

        private void OnDestroy()
        {
            _ecsSession.Destroy();
        }
    }
}

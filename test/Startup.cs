using System;
using System.Collections.Generic;
using DCFApixels;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class Startup : MonoBehaviour
    {

        private EcsSession _ecsSession;

        [SerializeField]
        public SharedData _data = new SharedData();

        private void Start()
        {
            _ecsSession = new EcsSession()
                .Inject(_data)
                .AddWorld(new EcsWorld<DefaultWorld>())
                .Add(new TestSystem())
                .Add(new VelocitySystem())
                .Add(new ViewSystem())
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

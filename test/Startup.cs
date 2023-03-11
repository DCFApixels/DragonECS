﻿using System;
using System.Collections.Generic;
using DCFApixels;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class Startup : MonoBehaviour
    {

        private EcsSession _ecsSession;
        public SharedData _data = new SharedData();

        private void Start()
        {
            _ecsSession
                .Add(new TestSystem())
                .Inject(_data)
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

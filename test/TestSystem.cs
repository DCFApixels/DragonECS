using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class TestSystem : IEcsInject<SharedData>, IEcsInject<EcsWorldMap>, IEcsInitSystem
    {
        private SharedData _sharedData;
        private EcsWorld<DefaultWorld> _world;
        public void Inject(SharedData obj) => _sharedData = obj;
        public void Inject(EcsWorldMap obj) { _world = obj.Get<DefaultWorld>(); }

        public void Init(EcsSession session)
        {
            var x1 = _world.GetFilter<Inc<TransfromCom, Velocity>>();
            var x2 = _world.GetFilter<Inc<TransfromCom, View>>();
            var x3 = _world.GetFilter<Inc<TransfromCom, Velocity>>();
            var x4 = _world.GetFilter<Inc<TransfromCom, Velocity>>();
            var x5 = _world.GetFilter<Inc<Velocity, TransfromCom>>();

            int has1 = x1.GetHashCode();
            int has2 = x2.GetHashCode();
            int has3 = x3.GetHashCode();
            int has4 = x4.GetHashCode();
            int has5 = x5.GetHashCode();

            var e = _world.NewEntity();
            e.Write<TransfromCom>().position = Vector3.zero;
            e.Write<View>().Ref = _sharedData.view1;
            e.Write<EnemyTag>();

            e = _world.NewEntity();
            e.Write<TransfromCom>().position = Vector3.zero;
            e.Write<Velocity>().value = Vector3.one;
            e.Write<View>().Ref = _sharedData.view2;
            e.Write<PlayerTag>();
        }
    }
}

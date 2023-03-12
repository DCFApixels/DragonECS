using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class ViewSystem : IEcsInject<EcsWorldMap>, IEcsRunSystem
    {
        private EcsWorld<DefaultWorld> _world;
        public void Inject(EcsWorldMap obj) { _world = obj.Get<DefaultWorld>(); }

        public void Run(EcsSession session)
        {
            foreach (var item in _world.GetFilter<Inc<TransfromCom, View>>().Entities)
            {
                item.Write<View>().Ref.position = item.Read<TransfromCom>().position;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public static class Mems
    {
        public static readonly mem<float> health = "health"; 
        public static readonly mem<float> regeneration = "regeneration";
        public static readonly mem<Vector3> position = "position";
        public static readonly mem<Quaternion> rotation = "rotation";
        public static readonly mem<Vector3> scale = "scale";
    }
}

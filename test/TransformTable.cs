using System;
using System.Collections.Generic;
using DCFApixels;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public class TransformTable : EcsTable
    {
        public readonly EcsPool<Vector3> position;
        public readonly EcsPool<Quaternion> rotation;
        public readonly EcsPool<Vector3> scale;

        public TransformTable(ref TableBuilder tableBuilder) : base(ref tableBuilder)
        {
            position = tableBuilder.Inc(Mems.position);
            rotation = tableBuilder.Inc(Mems.rotation);
            scale = tableBuilder.Inc(Mems.scale);
        }
    }

    public class PositionTable : EcsTable
    {
        public readonly EcsPool<Vector3> position;
        public readonly EcsPool<Quaternion> rotation;
        public readonly EcsPool<Vector3> scale;

        public PositionTable(ref TableBuilder tableBuilder) : base(ref tableBuilder)
        {
            position = tableBuilder.Inc(Mems.position);
            rotation = tableBuilder.Cache(Mems.rotation);
            scale = tableBuilder.Cache(Mems.scale);
        }
    }

    public class RotationTable : EcsTable
    {
        public readonly EcsPool<Vector3> position;
        public readonly EcsPool<Quaternion> rotation;
        public readonly EcsPool<Vector3> scale;

        public RotationTable(ref TableBuilder tableBuilder) : base(ref tableBuilder)
        {
            position = tableBuilder.Cache(Mems.position);
            rotation = tableBuilder.Inc(Mems.rotation);
            scale = tableBuilder.Cache(Mems.scale);
        }
    }

    public class ScaleTable : EcsTable
    {
        public readonly EcsPool<Vector3> position;
        public readonly EcsPool<Quaternion> rotation;
        public readonly EcsPool<Vector3> scale;

        public ScaleTable(ref TableBuilder tableBuilder) : base(ref tableBuilder)
        {
            position = tableBuilder.Cache(Mems.position);
            rotation = tableBuilder.Cache(Mems.rotation);
            scale = tableBuilder.Inc(Mems.scale);
        }
    }
}

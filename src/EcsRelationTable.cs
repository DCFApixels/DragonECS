using DCFApixels.DragonECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditorInternal;

namespace DCFApixels.Assets.DragonECS.src
{
    public class EcsRelationTable
    {
        public readonly IEcsWorld leftWorld;
        public readonly IEcsWorld rightWorld;


        private int[] _relationEntities; //dense
        private int[] _leftMapping;
        private int[] _rgihtMapping;

        private int _relationsCount;


        #region Properties
        public int RelationsCount
        {
            get => _relationsCount;
        }
        #endregion


        internal EcsRelationTable(IEcsWorld leftWorld, IEcsWorld rightWorld)
        {
            this.leftWorld = leftWorld;
            this.rightWorld = rightWorld;

            _relationEntities = new int[512];
            _leftMapping = new int[512];
            _rgihtMapping = new int[512];

            _relationsCount = 0;
        }

        public void AddRelation(int leftEnttiyID, int rightEntityID)
        {

        }
        public void RemoveRelationLeft(int entityID)
        {

        }
        public void RemoveRelationRight(int entityID)
        {

        }
    }
}

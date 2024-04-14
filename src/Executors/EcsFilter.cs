using DCFApixels.DragonECS.Internal;
using System;
using System.Reflection.Emit;

namespace DCFApixels.DragonECS
{
    public abstract class EcsFilter : EcsQueryExecutor
    {
        public abstract EcsAspect AspectRaw { get; }
    }
    public class EcsFilter<TAspect> : EcsFilter
        where TAspect : EcsAspect
    {
        private long _version;

        private EcsGroup _filterGroup;

        private TAspect _aspect;

        public override long Version { get { return _version; } }
        public TAspect Aspect { get { return _aspect; } }
        public override EcsAspect AspectRaw { get { return _aspect; } }

        private bool IsMatches(int entityID)
        {
            //int _entityComponentMaskBitShift = BitsUtility.GetHighBitNumber(World._entityComponentMaskLength);
            //
            //int[] _entityComponentMasks = World._entityComponentMasks;
            //int chunck = entityID << _entityComponentMaskBitShift;
            //for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
            //{
            //    var bit = _sortIncChunckBuffer.ptr[i];
            //    if ((_entityComponentMasks[chunck + bit.chankIndex] & bit.mask) != bit.mask)
            //    {
            //        goto skip;
            //    }
            //}
            //for (int i = 0; i < _sortExcChunckBuffer.Length; i++)
            //{
            //    var bit = _sortExcChunckBuffer.ptr[i];
            //    if ((_entityComponentMasks[chunck + bit.chankIndex] & bit.mask) != 0)
            //    {
            //        goto skip;
            //    }
            //}
            //return true;
            //skip: continue;
            throw new System.NotImplementedException();
        }
        protected override void OnInitialize()
        {
            _aspect = World.GetAspect<TAspect>();
        }
        protected override void OnDestroy()
        {

        }
    }
}

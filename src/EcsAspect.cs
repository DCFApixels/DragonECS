using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsAspect
    {
        internal EcsWorld _source;
        internal EcsMask _mask;
        private bool _isInit = false;

        private UnsafeArray<int> _sortIncBuffer;
        private UnsafeArray<int> _sortExcBuffer;
        private UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;
        private UnsafeArray<EcsMaskChunck> _sortExcChunckBuffer;

        #region Properties
        public EcsMask Mask
        {
            get { return _mask; }
        }
        public EcsWorld World
        {
            get { return _source; }
        }
        public bool IsInit
        {
            get { return _isInit; }
        }
        #endregion

        #region Methods
        public bool IsMatches(int entityID)
        {
            return _source.IsMatchesMask(_mask, entityID);
        }
        #endregion

        #region Builder
        protected virtual void Init(Builder b) { }
        public sealed class Builder
        {
            private EcsWorld _world;
            private EcsMask.Builder _maskBuilder;

            public EcsWorld World => _world;

            private Builder(EcsWorld world)
            {
                _world = world;
                _maskBuilder = EcsMask.New(world);
            }
            internal static unsafe TAspect New<TAspect>(EcsWorld world) where TAspect : EcsAspect
            {
                Builder builder = new Builder(world);
                Type aspectType = typeof(TAspect);
                EcsAspect newAspect;
                //TODO добавить оповещение что инициализация через конструктор не работает
#if !REFLECTION_DISABLED
                ConstructorInfo constructorInfo = aspectType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                if (constructorInfo != null)
                {
                    newAspect = (EcsAspect)constructorInfo.Invoke(new object[] { builder });
                }
                else
#endif
                {
#pragma warning disable IL2091 // Target generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in target method or type. The generic parameter of the source method or type does not have matching annotations.
                    newAspect = Activator.CreateInstance<TAspect>();
#pragma warning restore IL2091
                    newAspect.Init(builder);
                }
                newAspect._source = world;
                builder.Build(out newAspect._mask);
                newAspect._isInit = true;

                newAspect._sortIncBuffer = new UnsafeArray<int>(newAspect._mask.inc.Length, true);
                newAspect._sortExcBuffer = new UnsafeArray<int>(newAspect._mask.exc.Length, true);
                newAspect._sortIncChunckBuffer = new UnsafeArray<EcsMaskChunck>(newAspect._mask.incChunckMasks.Length, true);
                newAspect._sortExcChunckBuffer = new UnsafeArray<EcsMaskChunck>(newAspect._mask.excChunckMasks.Length, true);

                for (int i = 0; i < newAspect._sortIncBuffer.Length; i++)
                {
                    newAspect._sortIncBuffer.ptr[i] = newAspect._mask.inc[i];
                }
                for (int i = 0; i < newAspect._sortExcBuffer.Length; i++)
                {
                    newAspect._sortExcBuffer.ptr[i] = newAspect._mask.exc[i];
                }

                for (int i = 0; i < newAspect._sortIncChunckBuffer.Length; i++)
                {
                    newAspect._sortIncChunckBuffer.ptr[i] = newAspect._mask.incChunckMasks[i];
                }
                for (int i = 0; i < newAspect._sortExcChunckBuffer.Length; i++)
                {
                    newAspect._sortExcChunckBuffer.ptr[i] = newAspect._mask.excChunckMasks[i];
                }

                return (TAspect)newAspect;
            }

            #region Include/Exclude/Optional/Combine
            public TPool Include<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPool<TPool>();
                IncludeImplicit(pool.ComponentType);
                return pool;
            }
            public TPool Exclude<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPool<TPool>();
                ExcludeImplicit(pool.ComponentType);
                return pool;
            }
            public TPool Optional<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return _world.GetPool<TPool>();
            }
            private void IncludeImplicit(Type type)
            {
                _maskBuilder.Include(type);
            }
            private void ExcludeImplicit(Type type)
            {
                _maskBuilder.Exclude(type);
            }
            public TOtherAspect Combine<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect
            {
                var result = _world.GetAspect<TOtherAspect>();
                _maskBuilder.Combine(result.Mask);
                return result;
            }
            #endregion

            private void Build(out EcsMask mask)
            {
                mask = _maskBuilder.Build();
            }

            #region SupportReflectionHack
#if UNITY_2020_3_OR_NEWER
            [UnityEngine.Scripting.Preserve]
#endif
            private void SupportReflectionHack<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                Include<TPool>();
                Exclude<TPool>();
                Optional<TPool>();
                IncludeImplicit(null);
                ExcludeImplicit(null);
            }
            #endregion
        }
        #endregion

        #region Finalizator
        unsafe ~EcsAspect()
        {
            _sortIncBuffer.Dispose();
            _sortExcBuffer.Dispose();
            _sortIncChunckBuffer.Dispose();
            _sortExcChunckBuffer.Dispose();
        }
        #endregion

        #region Iterator
        public Iterator GetIterator()
        {
            return new Iterator(this, _source.Entities);
        }
        public Iterator GetIteratorFor(EcsSpan span)
        {
            return new Iterator(this, span);
        }
        #endregion

        #region Combined
        private readonly struct Combined
        {
            public readonly EcsAspect aspect;
            public readonly int order;
            public Combined(EcsAspect aspect, int order)
            {
                this.aspect = aspect;
                this.order = order;
            }
        }
        #endregion

        #region Iterator
        public ref struct Iterator
        {
            public readonly int worldID;
            public readonly EcsAspect aspect;
            private EcsSpan _span;

            public Iterator(EcsAspect aspect, EcsSpan span)
            {
                worldID = aspect.World.id;
                _span = span;
                this.aspect = aspect;
            }
            public void CopyTo(EcsGroup group)
            {
                group.Clear();
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    group.Add_Internal(enumerator.Current);
                }
            }
            public int CopyTo(ref int[] array)
            {
                int count = 0;
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (array.Length <= count)
                    {
                        Array.Resize(ref array, array.Length << 1);
                    }
                    array[count++] = enumerator.Current;
                }
                return count;
            }
            public EcsSpan CopyToSpan(ref int[] array)
            {
                int count = CopyTo(ref array);
                return new EcsSpan(worldID, array, count);
            }

            #region Other
            public override string ToString()
            {
                List<int> ints = new List<int>();
                foreach (var e in this)
                {
                    ints.Add(e);
                }
                return CollectionUtility.EntitiesToString(ints, "it");
            }
            #endregion

            #region Enumerator
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() { return new Enumerator(_span, aspect); }

            public unsafe ref struct Enumerator
            {
                #region CountComparers
                private readonly struct IncCountComparer : IComparerX<int>
                {
                    public readonly int[] counts;
                    public IncCountComparer(int[] counts)
                    {
                        this.counts = counts;
                    }
                    public int Compare(int a, int b)
                    {
                        return counts[a] - counts[b];
                    }
                }
                private readonly struct ExcCountComparer : IComparerX<int>
                {
                    public readonly int[] counts;
                    public ExcCountComparer(int[] counts)
                    {
                        this.counts = counts;
                    }
                    public int Compare(int a, int b)
                    {
                        return counts[b] - counts[a];
                    }
                }
                #endregion

                private ReadOnlySpan<int>.Enumerator _span;
                private readonly int[] _entityComponentMasks;

                private static EcsMaskChunck* _preSortedIncBuffer;
                private static EcsMaskChunck* _preSortedExcBuffer;

                private UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;
                private UnsafeArray<EcsMaskChunck> _sortExcChunckBuffer;

                private readonly int _entityComponentMaskLength;

                public unsafe Enumerator(EcsSpan span, EcsAspect aspect)
                {
                    _span = span.GetEnumerator();
                    _entityComponentMasks = aspect.World._entityComponentMasks;
                    _sortIncChunckBuffer = aspect._sortIncChunckBuffer;
                    _sortExcChunckBuffer = aspect._sortExcChunckBuffer;

                    _entityComponentMaskLength = aspect.World._entityComponentMaskLength;

                    #region Sort
                    UnsafeArray<int> _sortIncBuffer = aspect._sortIncBuffer;
                    UnsafeArray<int> _sortExcBuffer = aspect._sortExcBuffer;
                    int[] counts = aspect.World._poolComponentCounts;

                    if (_preSortedIncBuffer == null)
                    {
                        _preSortedIncBuffer = UnmanagedArrayUtility.New<EcsMaskChunck>(256);
                        _preSortedExcBuffer = UnmanagedArrayUtility.New<EcsMaskChunck>(256);
                    }

                    if (_sortIncChunckBuffer.Length > 1)
                    {
                        IncCountComparer incComparer = new IncCountComparer(counts);
                        UnsafeArraySortHalperX<int>.InsertionSort(_sortIncBuffer.ptr, _sortIncBuffer.Length, ref incComparer);
                        for (int i = 0; i < _sortIncBuffer.Length; i++)
                        {
                            _preSortedIncBuffer[i] = EcsMaskChunck.FromID(_sortIncBuffer.ptr[i]);
                        }
                        for (int i = 0, ii = 0; ii < _sortIncChunckBuffer.Length; ii++)
                        {
                            EcsMaskChunck bas = _preSortedIncBuffer[i];
                            int chankIndexX = bas.chankIndex;
                            int maskX = bas.mask;

                            for (int j = i + 1; j < _sortIncBuffer.Length; j++)
                            {
                                if (_preSortedIncBuffer[j].chankIndex == chankIndexX)
                                {
                                    maskX |= _preSortedIncBuffer[j].mask;
                                }
                            }
                            _sortIncChunckBuffer.ptr[ii] = new EcsMaskChunck(chankIndexX, maskX);
                            while (++i < _sortIncBuffer.Length && _preSortedIncBuffer[i].chankIndex == chankIndexX)
                            {
                                // skip
                            }
                        }
                    }

                    if (_sortExcChunckBuffer.Length > 1)
                    {
                        ExcCountComparer excComparer = new ExcCountComparer(counts);
                        UnsafeArraySortHalperX<int>.InsertionSort(_sortExcBuffer.ptr, _sortExcBuffer.Length, ref excComparer);
                        for (int i = 0; i < _sortExcBuffer.Length; i++)
                        {
                            _preSortedExcBuffer[i] = EcsMaskChunck.FromID(_sortExcBuffer.ptr[i]);
                        }

                        for (int i = 0, ii = 0; ii < _sortExcChunckBuffer.Length; ii++)
                        {
                            EcsMaskChunck bas = _preSortedExcBuffer[i];
                            int chankIndexX = bas.chankIndex;
                            int maskX = bas.mask;

                            for (int j = i + 1; j < _sortExcBuffer.Length; j++)
                            {
                                if (_preSortedExcBuffer[j].chankIndex == chankIndexX)
                                {
                                    maskX |= _preSortedExcBuffer[j].mask;
                                }
                            }
                            _sortExcChunckBuffer.ptr[ii] = new EcsMaskChunck(chankIndexX, maskX);
                            while (++i < _sortExcBuffer.Length && _preSortedExcBuffer[i].chankIndex == chankIndexX)
                            {
                                // skip
                            }
                        }
                    }
                    #endregion
                }
                public int Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get { return _span.Current; }
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    while (_span.MoveNext())
                    {
                        int e = _span.Current;
                        for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
                        {
                            var bit = _sortIncChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[e * _entityComponentMaskLength + bit.chankIndex] & bit.mask) != bit.mask)
                            {
                                goto skip;
                            }
                        }
                        for (int i = 0; i < _sortExcChunckBuffer.Length; i++)
                        {
                            var bit = _sortExcChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[e * _entityComponentMaskLength + bit.chankIndex] & bit.mask) > 0)
                            {
                                goto skip;
                            }
                        }
                        return true;
                        skip: continue;
                    }
                    return false;
                }
            }
            #endregion
        }
        #endregion
    }
}

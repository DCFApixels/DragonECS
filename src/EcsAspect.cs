using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsAspect : ITemplateNode
    {
        internal EcsWorld _source;
        internal EcsMask _mask;
        private bool _isBuilt = false;

        [ThreadStatic]
        private static Stack<Builder> _constructorBuildersStack = null;
        private static Stack<Builder> GetBuildersStack()
        {
            if (_constructorBuildersStack == null)
            {
                _constructorBuildersStack = new Stack<Builder>();
            }
            return _constructorBuildersStack;
        }

        protected static IncludeMarker Inc
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(Inc));
                }
                return buildersStack.Peek().Inc;
            }
        }
        protected static ExcludeMarker Exc
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(Exc));
                }
                return buildersStack.Peek().Exc;
            }
        }
        protected static OptionalMarker Opt
        {
            get
            {
                var buildersStack = GetBuildersStack();
                if (buildersStack.Count <= 0)
                {
                    Throw.Aspect_CanOnlyBeUsedDuringInitialization(nameof(Opt));
                }
                return buildersStack.Peek().Opt;
            }
        }

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
            get { return _isBuilt; }
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
            public IncludeMarker Inc
            {
                get { return new IncludeMarker(this); }
            }
            public ExcludeMarker Exc
            {
                get { return new ExcludeMarker(this); }
            }
            public OptionalMarker Opt
            {
                get { return new OptionalMarker(this); }
            }

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

                var buildersStack = GetBuildersStack();
                buildersStack.Push(builder);

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
                }
                newAspect.Init(builder);
                buildersStack.Pop();
                newAspect._source = world;
                builder.Build(out newAspect._mask);
                newAspect._isBuilt = true;

                newAspect._sortIncBuffer = new UnsafeArray<int>(newAspect._mask._inc.Length, true);
                newAspect._sortExcBuffer = new UnsafeArray<int>(newAspect._mask._exc.Length, true);
                newAspect._sortIncChunckBuffer = new UnsafeArray<EcsMaskChunck>(newAspect._mask._incChunckMasks.Length, true);
                newAspect._sortExcChunckBuffer = new UnsafeArray<EcsMaskChunck>(newAspect._mask._excChunckMasks.Length, true);

                for (int i = 0; i < newAspect._sortIncBuffer.Length; i++)
                {
                    newAspect._sortIncBuffer.ptr[i] = newAspect._mask._inc[i];
                }
                for (int i = 0; i < newAspect._sortExcBuffer.Length; i++)
                {
                    newAspect._sortExcBuffer.ptr[i] = newAspect._mask._exc[i];
                }

                for (int i = 0; i < newAspect._sortIncChunckBuffer.Length; i++)
                {
                    newAspect._sortIncChunckBuffer.ptr[i] = newAspect._mask._incChunckMasks[i];
                }
                for (int i = 0; i < newAspect._sortExcChunckBuffer.Length; i++)
                {
                    newAspect._sortExcChunckBuffer.ptr[i] = newAspect._mask._excChunckMasks[i];
                }

                return (TAspect)newAspect;
            }

            #region Include/Exclude/Optional/Combine/Except
            public TPool IncludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPoolInstance<TPool>();
                IncludeImplicit(pool.ComponentType);
                return pool;
            }
            public TPool ExcludePool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                var pool = _world.GetPoolInstance<TPool>();
                ExcludeImplicit(pool.ComponentType);
                return pool;
            }
            public TPool OptionalPool<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return _world.GetPoolInstance<TPool>();
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
            public TOtherAspect Except<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect
            {
                var result = _world.GetAspect<TOtherAspect>();
                _maskBuilder.Except(result.Mask);
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
                IncludePool<TPool>();
                ExcludePool<TPool>();
                OptionalPool<TPool>();
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
            public readonly short worldID;
            public readonly EcsAspect aspect;
            private EcsSpan _span;

            public Iterator(EcsAspect aspect, EcsSpan span)
            {
                worldID = aspect.World.id;
                _span = span;
                this.aspect = aspect;
            }

            #region CopyTo
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
            #endregion

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

                private readonly int _entityComponentMaskLengthBitShift;

                public unsafe Enumerator(EcsSpan span, EcsAspect aspect)
                {
                    _entityComponentMasks = aspect.World._entityComponentMasks;
                    _sortIncChunckBuffer = aspect._sortIncChunckBuffer;
                    _sortExcChunckBuffer = aspect._sortExcChunckBuffer;

                    _entityComponentMaskLengthBitShift = aspect.World._entityComponentMaskLengthBitShift;

                    if (aspect.Mask.IsBroken)
                    {
                        _span = span.Slice(0, 0).GetEnumerator();
                        return;
                    }

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
                            EcsMaskChunck chunkX = _preSortedIncBuffer[i];
                            int chankIndexX = chunkX.chankIndex;
                            int maskX = chunkX.mask;

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
                    if (_sortIncChunckBuffer.Length > 0 && counts[_sortIncBuffer.ptr[0]] <= 0)
                    {
                        _span = span.Slice(0, 0).GetEnumerator();
                        return;
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

                    _span = span.GetEnumerator();
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
                        int chunck = _span.Current << _entityComponentMaskLengthBitShift;
                        for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
                        {
                            var bit = _sortIncChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[chunck + bit.chankIndex] & bit.mask) != bit.mask)
                            {
                                goto skip;
                            }
                        }
                        for (int i = 0; i < _sortExcChunckBuffer.Length; i++)
                        {
                            var bit = _sortExcChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[chunck + bit.chankIndex] & bit.mask) != 0)
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

        #region Template
        public virtual void Apply(short worldID, int entityID)
        {
            EcsWorld world = EcsWorld.GetWorld(worldID);
            foreach (var incTypeID in _mask._inc)
            {
                var pool = world.GetPoolInstance(incTypeID);
                if (pool != null)
                {
                    if (pool.Has(entityID) == false)
                    {
                        pool.AddRaw(entityID, null);
                    }
                }
#if DEBUG
                else
                {
                    EcsDebug.PrintWarning("Component has not been added because the pool has not been initialized yet.");
                }
#endif
            }
            foreach (var excTypeID in _mask._exc)
            {
                var pool = world.GetPoolInstance(excTypeID);
                if (pool != null && pool.Has(entityID))
                {
                    pool.Del(entityID);
                }
            }
        }
        #endregion
    }
    public readonly ref struct IncludeMarker
    {
        private readonly EcsAspect.Builder _builder;
        public IncludeMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }
        public T GetInstance<T>()
            where T : IEcsPoolImplementation, new()
        {
            return _builder.IncludePool<T>();
        }
    }
    public readonly ref struct ExcludeMarker
    {
        private readonly EcsAspect.Builder _builder;
        public ExcludeMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }
        public T GetInstance<T>()
            where T : IEcsPoolImplementation, new()
        {
            return _builder.ExcludePool<T>();
        }
    }
    public readonly ref struct OptionalMarker
    {
        private readonly EcsAspect.Builder _builder;
        public OptionalMarker(EcsAspect.Builder builder)
        {
            _builder = builder;
        }
        public T GetInstance<T>()
            where T : IEcsPoolImplementation, new()
        {
            return _builder.OptionalPool<T>();
        }
    }
}

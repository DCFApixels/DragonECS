//#define sort_1
#define lockSwapCheck
// Зеленый свет, тесты показали идентичную скорость или даже прирост

using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DCFApixels.DragonECS
{
    public abstract class EcsAspect
    {
        internal EcsWorld source;
        internal EcsMask mask;
        private bool _isInit;
        #region Properties
        public EcsMask Mask => mask;
        public EcsWorld World => source;
        public bool IsInit => _isInit;
        #endregion

        #region Methods
        public bool IsMatches(int entityID) => source.IsMatchesMask(mask, entityID);
        #endregion

        #region Builder
        protected virtual void Init(Builder b) { }
        public sealed class Builder : EcsAspectBuilderBase
        {
            private EcsWorld _world;
            private HashSet<int> _inc;
            private HashSet<int> _exc;
            private List<Combined> _combined;

            public EcsWorld World => _world;

            private Builder(EcsWorld world)
            {
                _world = world;
                _combined = new List<Combined>();
                _inc = new HashSet<int>();
                _exc = new HashSet<int>();
            }
            internal static TAspect Build<TAspect>(EcsWorld world) where TAspect : EcsAspect
            {
                Builder builder = new Builder(world);
                Type aspectType = typeof(TAspect);
                ConstructorInfo constructorInfo = aspectType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                EcsAspect newAspect;
                if (constructorInfo != null)
                {
                    newAspect = (EcsAspect)constructorInfo.Invoke(new object[] { builder });
                }
                else
                {
                    newAspect = (EcsAspect)Activator.CreateInstance(typeof(TAspect));
                    newAspect.Init(builder);
                }
                newAspect.source = world;
                builder.End(out newAspect.mask);
                newAspect._isInit = true;
                return (TAspect)newAspect;
            }

            #region Include/Exclude/Optional
            public sealed override TPool Include<TPool>()
            {
                IncludeImplicit(typeof(TPool).GetGenericArguments()[0]);
                return _world.GetPool<TPool>();
            }
            public sealed override TPool Exclude<TPool>()
            {
                ExcludeImplicit(typeof(TPool).GetGenericArguments()[0]);
                return _world.GetPool<TPool>();
            }
            public sealed override TPool Optional<TPool>()
            {
                return _world.GetPool<TPool>();
            }
            private void IncludeImplicit(Type type)
            {
                int id = _world.GetComponentID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(type);
#endif
                _inc.Add(id);
            }
            private void ExcludeImplicit(Type type)
            {
                int id = _world.GetComponentID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(type);
#endif
                _exc.Add(id);
            }
            #endregion

            #region Combine
            public TOtherAspect Combine<TOtherAspect>(int order = 0) where TOtherAspect : EcsAspect
            {
                var result = _world.GetAspect<TOtherAspect>();
                _combined.Add(new Combined(result, order));
                return result;
            }
            #endregion

            public EcsWorldCmp<T> GetWorldData<T>() where T : struct
            {
                return new EcsWorldCmp<T>(_world.id);
            }

            private void End(out EcsMask mask)
            {
                HashSet<int> maskInc;
                HashSet<int> maskExc;
                if (_combined.Count > 0)
                {
                    maskInc = new HashSet<int>();
                    maskExc = new HashSet<int>();
                    _combined.Sort((a, b) => a.order - b.order);
                    foreach (var item in _combined)
                    {
                        EcsMask submask = item.aspect.mask;
                        maskInc.ExceptWith(submask.exc);//удаляю конфликтующие ограничения
                        maskExc.ExceptWith(submask.inc);//удаляю конфликтующие ограничения
                        maskInc.UnionWith(submask.inc);
                        maskExc.UnionWith(submask.exc);
                    }
                    maskInc.ExceptWith(_exc);//удаляю конфликтующие ограничения
                    maskExc.ExceptWith(_inc);//удаляю конфликтующие ограничения
                    maskInc.UnionWith(_inc);
                    maskExc.UnionWith(_exc);
                }
                else
                {
                    maskInc = _inc;
                    maskExc = _exc;
                }

                Dictionary<int, int> r = new Dictionary<int, int>();
                foreach (var id in maskInc)
                {
                    var bit = EcsMaskBit.FromID(id);
                    if (!r.TryAdd(bit.chankIndex, bit.mask))
                        r[bit.chankIndex] = r[bit.chankIndex] | bit.mask;
                }
                EcsMaskBit[] incMasks = r.Select(o => new EcsMaskBit(o.Key, o.Value)).ToArray();
                r.Clear();
                foreach (var id in maskExc)
                {
                    var bit = EcsMaskBit.FromID(id);
                    if (!r.TryAdd(bit.chankIndex, bit.mask))
                        r[bit.chankIndex] = r[bit.chankIndex] | bit.mask;
                }
                EcsMaskBit[] excMasks = r.Select(o => new EcsMaskBit(o.Key, o.Value)).ToArray();

                var inc = maskInc.ToArray();
                Array.Sort(inc);
                var exc = maskExc.ToArray();
                Array.Sort(exc);

                mask = new EcsMask(_world.id, inc, exc, incMasks, excMasks);
                _world = null;
                _inc = null;
                _exc = null;
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

        #region Iterator
        public EcsAspectIterator GetIterator()
        {
            return new EcsAspectIterator(this, source.Entities);
        }
        public EcsAspectIterator GetIteratorFor(EcsSpan span)
        {
            return new EcsAspectIterator(this, span);
        }
        #endregion

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
    }

    #region BuilderBase
    public abstract class EcsAspectBuilderBase
    {
        public abstract TPool Include<TPool>() where TPool : IEcsPoolImplementation, new();
        public abstract TPool Exclude<TPool>() where TPool : IEcsPoolImplementation, new();
        public abstract TPool Optional<TPool>() where TPool : IEcsPoolImplementation, new();
    }
    #endregion

    #region Mask
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EcsMaskBit
    {
        private const int BITS = 32;
        private const int DIV_SHIFT = 5;
        private const int MOD_MASK = BITS - 1;

        public readonly int chankIndex;
        public readonly int mask;
        public EcsMaskBit(int chankIndex, int mask)
        {
            this.chankIndex = chankIndex;
            this.mask = mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsMaskBit FromID(int id)
        {
            return new EcsMaskBit(id >> DIV_SHIFT, 1 << (id & MOD_MASK)); //аналогично new EcsMaskBit(id / BITS, 1 << (id % BITS)) но быстрее
        }
        public override string ToString()
        {
            return $"mask({chankIndex}, {mask}, {BitsUtility.CountBits(mask)})";
        }
        internal class DebuggerProxy
        {
            public int chunk;
            public uint mask;
            public int[] values = Array.Empty<int>();
            public string bits;
            public DebuggerProxy(EcsMaskBit maskbits)
            {
                chunk = maskbits.chankIndex;
                mask = (uint)maskbits.mask;
                BitsUtility.GetBitNumbersNoAlloc(mask, ref values);
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] += (chunk) << 5;
                }
                bits = BitsUtility.ToBitsString(mask, '_', 8);
            }
        }
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask
    {
        internal readonly int worldID;
        internal readonly EcsMaskBit[] incChunckMasks;
        internal readonly EcsMaskBit[] excChunckMasks;
        internal readonly int[] inc;
        internal readonly int[] exc;
        public int WorldID => worldID;
        public EcsWorld World => EcsWorld.GetWorld(worldID);
        /// <summary>Including constraints</summary>
        public ReadOnlySpan<int> Inc => inc;
        /// <summary>Excluding constraints</summary>
        public ReadOnlySpan<int> Exc => exc;
        internal EcsMask(int worldID, int[] inc, int[] exc, EcsMaskBit[] incChunckMasks, EcsMaskBit[] excChunckMasks)
        {
#if DEBUG
            CheckConstraints(inc, exc);
#endif
            this.inc = inc;
            this.exc = exc;
            this.worldID = worldID;
            this.incChunckMasks = incChunckMasks;
            this.excChunckMasks = excChunckMasks;
        }

        #region Object
        public override string ToString() => CreateLogString(worldID, inc, exc);
        #endregion

        #region Debug utils
#if DEBUG
        private static HashSet<int> _dummyHashSet = new HashSet<int>();
        private void CheckConstraints(int[] inc, int[] exc)
        {
            lock (_dummyHashSet)
            {
                if (CheckRepeats(inc)) throw new EcsFrameworkException("The values in the Include constraints are repeated.");
                if (CheckRepeats(exc)) throw new EcsFrameworkException("The values in the Exclude constraints are repeated.");
                _dummyHashSet.Clear();
                _dummyHashSet.UnionWith(inc);
                if (_dummyHashSet.Overlaps(exc)) throw new EcsFrameworkException("Conflicting Include and Exclude constraints.");
            }
        }
        private bool CheckRepeats(int[] array)
        {
            _dummyHashSet.Clear();
            foreach (var item in array)
            {
                if (_dummyHashSet.Contains(item)) return true;
                _dummyHashSet.Add(item);
            }
            return false;
        }
#endif
        private static string CreateLogString(int worldID, int[] inc, int[] exc)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string converter(int o) => EcsDebugUtility.GetGenericTypeName(EcsWorld.GetWorld(worldID).AllPools[o].ComponentType, 1);
            return $"Inc({string.Join(", ", inc.Select(converter))}) Exc({string.Join(", ", exc.Select(converter))})";
#else
            return $"Inc({string.Join(", ", inc)}) Exc({string.Join(", ", exc)})"; // Release optimization
#endif
        }
        internal class DebuggerProxy
        {
            public readonly EcsWorld world;
            public readonly int worldID;
            public readonly EcsMaskBit[] includedChunkMasks;
            public readonly EcsMaskBit[] excludedChunkMasks;
            public readonly int[] included;
            public readonly int[] excluded;
            public readonly Type[] includedTypes;
            public readonly Type[] excludedTypes;

            public DebuggerProxy(EcsMask mask)
            {
                world = EcsWorld.GetWorld(mask.worldID);
                worldID = mask.worldID;
                includedChunkMasks = mask.incChunckMasks;
                excludedChunkMasks = mask.excChunckMasks;
                included = mask.inc;
                excluded = mask.exc;
                Type converter(int o) => world.GetComponentType(o);
                includedTypes = included.Select(converter).ToArray();
                excludedTypes = excluded.Select(converter).ToArray();
            }
            public override string ToString() => CreateLogString(worldID, included, excluded);
        }
        #endregion
    }
    #endregion

    #region Iterator
    public ref struct EcsAspectIterator
    {
        public readonly int worldID;
        public readonly EcsMask mask;
        private EcsSpan _span;
        private Enumerator _enumerator;

        public EcsAspectIterator(EcsAspect aspect, EcsSpan span)
        {
            worldID = aspect.World.id;
            mask = aspect.mask; 
            _span = span;
            _enumerator = default;
        }

        public int Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _enumerator.Current;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin() => _enumerator = GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Next() => _enumerator.MoveNext();
        public void CopyTo(EcsGroup group)
        {
            group.Clear();
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
                group.AddInternal(enumerator.Current);
        }
        public int CopyTo(ref int[] array)
        {
            var enumerator = GetEnumerator();
            int count = 0;
            while (enumerator.MoveNext())
            {
                if(array.Length <= count)
                    Array.Resize(ref array, array.Length << 1);
                array[count++] = enumerator.Current;
            }
            return count;
        }
        public EcsSpan CopyToSpan(ref int[] array)
        {
            var enumerator = GetEnumerator();
            int count = 0;
            while (enumerator.MoveNext())
            {
                if (array.Length <= count)
                    Array.Resize(ref array, array.Length << 1);
                array[count++] = enumerator.Current;
            }
            return new EcsSpan(worldID, array, count);
        }

        #region object
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (var e in this)
            {
                result.Append(e);
                result.Append(", ");
            }
            return result.ToString();
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_span, mask);


        public unsafe ref struct Enumerator
        {
            private ReadOnlySpan<int>.Enumerator _span;
#if sort_1
            private readonly EcsMaskBit[] _incChunckMasks;
            private readonly EcsMaskBit[] _excChunckMasks;
#else
            private readonly EcsMaskBit[] _incChunckMasks;
            private readonly EcsMaskBit[] _excChunckMasks;
#endif
            private readonly int[][] _entitiesComponentMasks;



            private static EcsMaskBit* _sortedIncBuffer;
            private static EcsMaskBit* _sortedExcBuffer;
            private static SparseArray<int> _sp;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe Enumerator(EcsSpan span, EcsMask mask)
            {
                _span = span.GetEnumerator();
#if sort_1
                _incChunckMasks = mask.incChunckMasks;
                _excChunckMasks = mask.excChunckMasks;
#endif
                _entitiesComponentMasks = span.World._entitiesComponentMasks;


                #region MyRegion
                #region Trash
#if !sort_1
                int[] inc = mask.inc;
                int[] exc = mask.exc;
                int[] counts = mask.World._poolComponentCounts;
                if (inc.Length > 1)
                {
                    //if (inc.Length == 2)
                    //{
                    //    if (counts[inc[0]] > counts[inc[1]])
                    //    {
                    //        int tmp = inc[0];
                    //        inc[0] = inc[1];
                    //        inc[1] = tmp;
                    //    }
                    //    //...
                    //}
                    //else
                    {
                        //for (int i = 0; i < inc.Length; i++)
                        //{
                        //    int counti = counts[inc[i]];
                        //    if (counti <= 0)
                        //    {
                        //        _span = ReadOnlySpan<int>.Empty.GetEnumerator();
                        //        goto skip1;
                        //    }
                        //    for (int j = inc.Length - 1; j >= i; j--)
                        //    {
                        //        if (counts[inc[j]] < counti)
                        //        {
                        //            int tmp = inc[j];
                        //            inc[j] = inc[i];
                        //            inc[i] = tmp;
                        //        }
                        //    }
                        //}

                        //...

                        for (int i = 0, n = inc.Length; i < n - 1; i++)
                        {
                            //int counti = counts[inc[i]];
                            //if (counti <= 0)
                            //{
                            //    _span = ReadOnlySpan<int>.Empty.GetEnumerator();
                            //    goto skip1;
                            //}
                            bool noSwaped = true; 
                            for (int j = 0; j < n - i - 1; )
                            {
                                ref int j0 = ref inc[j++];
                                if (counts[j0] > counts[inc[j]])
                                {
                                    int tmp = inc[j];
                                    inc[j] = j0;
                                    j0 = tmp;
                                    noSwaped = false;
                                }
                            }
#if !lockSwapCheck
                            if (noSwaped)
                                break;
#endif
                        }
                    }
                }
                skip1:;
                if (exc.Length > 1)
                {
                    //if (exc.Length == 2)
                    //{
                    //    if (counts[exc[0]] < counts[exc[1]])
                    //    {
                    //        int tmp = exc[0];
                    //        exc[0] = inc[1];
                    //        exc[1] = tmp;
                    //    }
                    //    //...
                    //}
                    //else
                    {
                        //for (int i = 0; i < exc.Length; i++)
                        //{
                        //    int counti = counts[inc[i]];
                        //    if (counti <= 0)
                        //    {
                        //        _excChunckMasks = ReadOnlySpan<EcsMaskBit>.Empty;
                        //        goto skip2;
                        //    }
                        //    for (int j = exc.Length - 1; j >= i; j--)
                        //    {
                        //        if (counts[exc[j]] > counti)
                        //        {
                        //            int tmp = exc[j];
                        //            exc[j] = exc[i];
                        //            exc[i] = tmp;
                        //        }
                        //    }
                        //}

                        //...

                        for (int i = 0, n = exc.Length; i < n - 1; i++)
                        {
                            //int counti = counts[inc[i]];
                            //if (counti <= 0)
                            //{
                            //    _excChunckMasks = ReadOnlySpan<EcsMaskBit>.Empty;
                            //    goto skip2;
                            //}
                            bool noSwaped = true;
                            for (int j = 0; j < n - i - 1;)
                            {
                                ref int j0 = ref exc[j++];
                                if (counts[j0] < counts[exc[j]])
                                {
                                    int tmp = exc[j];
                                    exc[j] = j0;
                                    j0 = tmp;
                                    noSwaped = false;
                                }
                            }
#if !lockSwapCheck
                            if (noSwaped)
                                break;
#endif
                        }
                    }
                }
                skip2:;

                if (_sortedIncBuffer == null)
                {
                    _sortedIncBuffer = UnmanagedArrayUtility.New<EcsMaskBit>(256);
                    _sortedExcBuffer = UnmanagedArrayUtility.New<EcsMaskBit>(256);
                    _sp = new SparseArray<int>(32);
                }

                _sp.Clear();
                for (int i = 0, ii = 0; i < inc.Length; i++)
                {
                    //int id = inc[i];
                    //_sortedIncBuffer[i] = new EcsMaskBit(id >> DIV_SHIFT, 1 << (id & MOD_MASK));
                    _sortedIncBuffer[i] = EcsMaskBit.FromID(inc[i]);
                }
                for (int i = 0; i < exc.Length; i++)
                {
                    //int id = inc[i];
                    //_sortedExcBuffer[i] = new EcsMaskBit(id >> DIV_SHIFT, 1 << (id & MOD_MASK));
                    _sortedExcBuffer[i] = EcsMaskBit.FromID(exc[i]);
                }

                EcsMaskBit[] incChunckMasks = mask.incChunckMasks;
                EcsMaskBit[] excChunckMasks = mask.excChunckMasks;

                //_incChunckMasks = new ReadOnlySpan<EcsMaskBit>(_sortedIncBuffer, inc.Length);
                //_excChunckMasks = new ReadOnlySpan<EcsMaskBit>(_sortedExcBuffer, exc.Length);

                
                int _sortedIncBufferLength = inc.Length;
                int _sortedExcBufferLength = exc.Length;

                if (_sortedIncBufferLength > 1)//перенести этот чек в начала сортировки, для _incChunckMasks.Length == 1 сортировка не нужна
                {
                    for (int i = 0, ii = 0; ii < incChunckMasks.Length; ii++)
                    {
                        EcsMaskBit bas = _sortedIncBuffer[i];
                        int chankIndexX = bas.chankIndex;
                        int maskX = bas.mask;

                        for (int j = i + 1; j < _sortedIncBufferLength; j++)
                        {
                            if (_sortedIncBuffer[j].chankIndex == chankIndexX)
                            {
                                maskX |= _sortedIncBuffer[j].mask;
                            }
                        }
                        incChunckMasks[ii] = new EcsMaskBit(chankIndexX, maskX);
                        while (++i < _sortedIncBufferLength && _sortedIncBuffer[i].chankIndex == chankIndexX)
                        {

                        }
                    }
                }

                if (_sortedExcBufferLength > 1)//перенести этот чек в начала сортировки, для _excChunckMasks.Length == 1 сортировка не нужна
                {
                    for (int i = 0, ii = 0; ii < excChunckMasks.Length; ii++)
                    {
                        EcsMaskBit bas = _sortedExcBuffer[i];
                        int chankIndexX = bas.chankIndex;
                        int maskX = bas.mask;

                        for (int j = i + 1; j < _sortedExcBufferLength; j++)
                        {
                            if (_sortedExcBuffer[j].chankIndex == chankIndexX)
                            {
                                maskX |= _sortedExcBuffer[j].mask;
                            }
                        }
                        excChunckMasks[ii] = new EcsMaskBit(chankIndexX, maskX);
                        while (++i < _sortedExcBufferLength && _sortedExcBuffer[i].chankIndex == chankIndexX)
                        {

                        }
                    }
                }

                _incChunckMasks = incChunckMasks;
                _excChunckMasks = excChunckMasks;

#endif
                #endregion

                #endregion

                #region MyRegion
                //_inc = mask.inc;
                //_exc = mask.exc;
                //int[] inc = mask.inc;
                //int[] exc = mask.exc;
                //int[] counts = mask.World._poolComponentCounts;
                //if (_inc.Length > 1)
                //{
                //    for (int i = 0; i < inc.Length; i++)
                //    {
                //        if (counts[inc[i]] <= 0)
                //        {
                //            _span = ReadOnlySpan<int>.Empty.GetEnumerator();
                //            goto skip1;
                //        }
                //        for (int j = 0; j < inc.Length - i - 1; j++)
                //        {
                //            if (counts[inc[i]] > counts[inc[j]])
                //            {
                //                int tmp = inc[j];
                //                inc[j] = inc[i];
                //                inc[i] = tmp;
                //            }
                //        }
                //    }
                //}
                //skip1:;
                //if(exc.Length > 1)
                //{
                //    for (int i = 0; i < exc.Length; i++)
                //    {
                //        if (counts[exc[i]] <= 0)
                //        {
                //            _exc = ReadOnlySpan<int>.Empty;
                //            goto skip2;
                //        }
                //        for (int j = 0; j < exc.Length - i - 1; j++)
                //        {
                //            if (counts[exc[i]] < counts[exc[j]])
                //            {
                //                int tmp = exc[j];
                //                exc[j] = exc[i];
                //                exc[i] = tmp;
                //            }
                //        }
                //    }
                //}
                //skip2:;
                #endregion
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _span.Current;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_span.MoveNext())
                {
                    int e = _span.Current;

                    foreach (var bit in _incChunckMasks)
                    {
                        if ((_entitiesComponentMasks[e][bit.chankIndex] & bit.mask) != bit.mask)
                            goto skip;
                    }
                    foreach (var bit in _excChunckMasks)
                    {
                        if ((_entitiesComponentMasks[e][bit.chankIndex] & bit.mask) > 0)
                            goto skip;
                    }

                    //for (int i = 0, iMax = _incChunckMasks.Length; i < iMax; i++)
                    //{
                    //    var bit = _incChunckMasks[i];
                    //    if ((_entitiesComponentMasks[e][bit.chankIndex] & bit.mask) != bit.mask)
                    //        goto skip;
                    //}
                    //for (int i = 0, iMax = _excChunckMasks.Length; i < iMax; i++)
                    //{
                    //    var bit = _excChunckMasks[i];
                    //    if ((_entitiesComponentMasks[e][bit.chankIndex] & bit.mask) > 0)
                    //        goto skip;
                    //}
                    return true;
                    skip: continue;

                    #region MyRegion
                    //for (int i = 0, iMax = _inc.Length; i < iMax; i++)
                    //{
                    //    bit = EcsMaskBit.FromID(_inc[i]);
                    //    if ((_entitiesComponentMasks[e][bit.chankIndex] & bit.mask) != bit.mask)
                    //        goto skip;
                    //}
                    //for (int i = 0, iMax = _exc.Length; i < iMax; i++)
                    //{
                    //    bit = EcsMaskBit.FromID(_exc[i]);
                    //    if ((_entitiesComponentMasks[e][bit.chankIndex] & bit.mask) > 0)
                    //        goto skip;
                    //}
                    //return true;
                    //skip: continue;
                    #endregion
                }
                return false;
            }
        }
        #endregion
    }
    #endregion
}

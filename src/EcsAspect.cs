using DCFApixels.DragonECS.Internal;
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

                var inc = maskInc.ToArray();
                Array.Sort(inc);
                var exc = maskExc.ToArray();
                Array.Sort(exc);

                mask = new EcsMask(_world.id, inc, exc);
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
        public EcsAspectIterator GetIteratorFor(EcsReadonlyGroup sourceGroup)
        {
            return new EcsAspectIterator(this, sourceGroup);
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
    public sealed class EcsMask
    {
        internal readonly int worldID;
        /// <summary>Including constraints</summary>
        internal readonly int[] inc;
        /// <summary>Excluding constraints</summary>
        internal readonly int[] exc;
        internal EcsMask(int worldID, int[] inc, int[] exc)
        {
#if DEBUG
            CheckConstraints(inc, exc);
#endif
            this.worldID = worldID;
            this.inc = inc;
            this.exc = exc;
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
            public readonly Type worldType;
            public readonly int worldID;
            public readonly int[] included;
            public readonly int[] excluded;
            public readonly Type[] includedTypes;
            public readonly Type[] excludedTypes;
            public DebuggerProxy(EcsMask mask)
            {
                worldType = WorldMetaStorage.GetWorldType(mask.worldID);
                worldID = mask.worldID;
                included = mask.inc;
                excluded = mask.exc;
                Type converter(int o) => WorldMetaStorage.GetComponentType(worldID, o);
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
        public readonly EcsMask mask;
        private EcsReadonlyGroup _sourceGroup;
        private Enumerator _enumerator;

        public EcsAspectIterator(EcsAspect aspect, EcsReadonlyGroup sourceGroup)
        {
            mask = aspect.mask;
            _sourceGroup = sourceGroup;
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
        public Enumerator GetEnumerator() => new Enumerator(_sourceGroup, mask);

        public ref struct Enumerator
        {
            private EcsGroup.Enumerator _sourceGroup;
            private readonly int[] _inc;
            private readonly int[] _exc;
            private readonly IEcsPoolImplementation[] _pools;

            public Enumerator(EcsReadonlyGroup sourceGroup, EcsMask mask)
            {
                _sourceGroup = sourceGroup.GetEnumerator();
                _inc = mask.inc;
                _exc = mask.exc;
                _pools = sourceGroup.World._pools;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _sourceGroup.Current;
            }
            public bool MoveNext()
            {
                while (_sourceGroup.MoveNext())
                {
                    int e = _sourceGroup.Current;
                    for (int i = 0, iMax = _inc.Length; i < iMax; i++)
                        if (!_pools[_inc[i]].Has(e)) goto next;
                    for (int i = 0, iMax = _exc.Length; i < iMax; i++)
                        if (_pools[_exc[i]].Has(e)) goto next;
                    return true;
                    next: continue;
                }
                return false;
            }
        }
        #endregion
    }
    #endregion
}

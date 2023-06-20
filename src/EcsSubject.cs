using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DCFApixels.DragonECS
{
    public abstract class EcsSubject
    {
        [EditorBrowsable(EditorBrowsableState.Always)]
        internal EcsWorld source;
        [EditorBrowsable(EditorBrowsableState.Always)]
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
        public sealed class Builder : EcsSubjectBuilderBase
        {
            private EcsWorld _world;
            private HashSet<int> _inc;
            private HashSet<int> _exc;
            private List<CombinedSubject> _subjects;

            public EcsWorld World => _world;

            private Builder(EcsWorld world)
            {
                _world = world;
                _subjects = new List<CombinedSubject>();
                _inc = new HashSet<int>();
                _exc = new HashSet<int>();
            }
            internal static TSubject Build<TSubject>(EcsWorld world) where TSubject : EcsSubject
            {
                Builder builder = new Builder(world);
                Type subjectType = typeof(TSubject);
                ConstructorInfo constructorInfo = subjectType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                EcsSubject newSubject;
                if (constructorInfo != null)
                {
                    newSubject = (EcsSubject)constructorInfo.Invoke(new object[] { builder });
                }
                else
                {
                    newSubject = (EcsSubject)Activator.CreateInstance(typeof(TSubject));
                    newSubject.Init(builder);
                }
                newSubject.source = world;
                builder.End(out newSubject.mask);
                newSubject._isInit = true;
                return (TSubject)newSubject;
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
                if (_inc.Contains(id) || _exc.Contains(id)) throw new EcsFrameworkException($"{type.Name} already in constraints list.");
#endif
                _inc.Add(id);
            }
            private void ExcludeImplicit(Type type)
            {
                int id = _world.GetComponentID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) throw new EcsFrameworkException($"{type.Name} already in constraints list.");
#endif
                _exc.Add(id);
            }
            #endregion

            #region Combine
            public TOtherSubject Combine<TOtherSubject>(int order = 0) where TOtherSubject : EcsSubject
            {
                var result = _world.GetSubject<TOtherSubject>();
                _subjects.Add(new CombinedSubject(result, order));
                return result;
            }
            #endregion

            private void End(out EcsMask mask)
            {
                HashSet<int> maskInc;
                HashSet<int> maskExc;
                if (_subjects.Count > 0)
                {
                    maskInc = new HashSet<int>();
                    maskExc = new HashSet<int>();
                    _subjects.Sort((a, b) => a.order - b.order);
                    foreach (var item in _subjects)
                    {
                        EcsMask submask = item.subject.mask;
                        maskInc.ExceptWith(submask._exc);//удаляю конфликтующие ограничения
                        maskExc.ExceptWith(submask._inc);//удаляю конфликтующие ограничения
                        maskInc.UnionWith(submask._inc);
                        maskExc.UnionWith(submask._exc);
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

                mask = new EcsMask(_world.WorldTypeID, inc, exc);
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
        public EcsSubjectIterator GetIterator()
        {
            return new EcsSubjectIterator(this, source.Entities);
        }
        public EcsSubjectIterator GetIteratorFor(EcsReadonlyGroup sourceGroup)
        {
            return new EcsSubjectIterator(this, sourceGroup);
        }
        #endregion

        private struct CombinedSubject
        {
            public EcsSubject subject;
            public int order;
            public CombinedSubject(EcsSubject subject, int order)
            {
                this.subject = subject;
                this.order = order;
            }
        }
    }

    #region BuilderBase
    public abstract class EcsSubjectBuilderBase
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
        internal readonly int _worldTypeID;
        /// <summary>Including constraints</summary>
        internal readonly int[] _inc;
        /// <summary>Excluding constraints</summary>
        internal readonly int[] _exc;
        internal EcsMask(int worldTypeID, int[] inc, int[] exc)
        {
#if DEBUG
            if (worldTypeID == 0) throw new ArgumentException();
            CheckConstraints(inc, exc);
#endif
            _worldTypeID = worldTypeID;
            _inc = inc;
            _exc = exc;
        }

        #region Object
        public override string ToString() => CreateLogString(_worldTypeID, _inc, _exc);
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
        private static string CreateLogString(int worldTypeID, int[] inc, int[] exc)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string converter(int o) => EcsDebugUtility.GetGenericTypeName(WorldMetaStorage.GetComponentType(worldTypeID, o), 1);
            return $"Inc({string.Join(", ", inc.Select(converter))}) Exc({string.Join(", ", exc.Select(converter))})";
#else
            return $"Inc({string.Join(", ", inc)}) Exc({string.Join(", ", exc)})"; // Release optimization
#endif
        }
        internal class DebuggerProxy
        {
            public readonly Type worldType;
            public readonly int worldTypeID;
            public readonly int[] included;
            public readonly int[] excluded;
            public readonly Type[] includedTypes;
            public readonly Type[] excludedTypes;
            public DebuggerProxy(EcsMask mask)
            {
                worldType = WorldMetaStorage.GetWorldType(mask._worldTypeID);
                worldTypeID = mask._worldTypeID;
                included = mask._inc;
                excluded = mask._exc;
                Type converter(int o) => WorldMetaStorage.GetComponentType(worldTypeID, o);
                includedTypes = included.Select(converter).ToArray();
                excludedTypes = excluded.Select(converter).ToArray();
            }
            public override string ToString() => CreateLogString(worldTypeID, included, excluded);
        }
        #endregion

        #region ThrowHelper
        internal static class ThrowHelper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowArgumentDifferentWorldsException() => throw new ArgumentException("The groups belong to different worlds.");
        }
        #endregion
    }
    #endregion

    #region Iterator
    public ref struct EcsSubjectIterator
    {
        public readonly EcsMask mask;
        private EcsReadonlyGroup _sourceGroup;
        private Enumerator _enumerator;

        public EcsSubjectIterator(EcsSubject subject, EcsReadonlyGroup sourceGroup)
        {
            mask = subject.mask;
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
                _inc = mask._inc;
                _exc = mask._exc;
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

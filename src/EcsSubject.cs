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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EcsMask Mask => mask;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EcsWorld World => source;
        [EditorBrowsable(EditorBrowsableState.Never)]
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
                _inc = new HashSet<int>(8);
                _exc = new HashSet<int>(4);
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
                if(_subjects.Count > 0)
                {
                    _subjects.Sort((a, b) => a.order - b.order);
                    foreach (var item in _subjects)
                    {
                        EcsMask submask = item.subject.mask;
                        _inc.ExceptWith(submask._exc);//удаляю конфликтующие ограничения
                        _exc.ExceptWith(submask._inc);//удаляю конфликтующие ограничения
                        _inc.UnionWith(submask._inc);
                        _exc.UnionWith(submask._exc);
                    }
                }

                var inc = _inc.ToArray();
                Array.Sort(inc);
                var exc = _exc.ToArray();
                Array.Sort(exc);

                mask = new EcsMask(_world.Archetype, inc, exc);
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
            return new EcsSubjectIterator(this, World.Entities);
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
        internal readonly Type _worldType;
        internal readonly int[] _inc;
        internal readonly int[] _exc;
        internal EcsMask(Type worldType, int[] inc, int[] exc)
        {
#if DEBUG
            if (worldType is null) throw new ArgumentNullException();
            CheckConstraints(inc, exc);
#endif
            _worldType = worldType;
            _inc = inc;
            _exc = exc;
        }

        #region Object
        public override string ToString() => CreateLogString(_worldType, _inc, _exc);
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
                if (_dummyHashSet.Contains(item)) return false;
                _dummyHashSet.Add(item);
            }
            return true;
        }
#endif
        private static string CreateLogString(Type worldType, int[] inc, int[] exc)
        {
#if (DEBUG && !DISABLE_DEBUG)
            int worldID = WorldMetaStorage.GetWorldID(worldType);
            string converter(int o) => EcsDebugUtility.GetGenericTypeName(WorldMetaStorage.GetComponentType(worldID, o), 1);
            return $"Inc({string.Join(", ", inc.Select(converter))}) Exc({string.Join(", ", exc.Select(converter))})";
#else
            return $"Inc({string.Join(", ", inc)}) Exc({string.Join(", ", exc)})"; // Release optimization
#endif
        }
        internal class DebuggerProxy
        {
            public readonly Type worldType;
            public readonly int[] inc;
            public readonly int[] exc;
            public readonly Type[] incTypes;
            public readonly Type[] excTypes;
            public DebuggerProxy(EcsMask mask)
            {
                worldType = mask._worldType;
                int worldID = WorldMetaStorage.GetWorldID(worldType);
                inc = mask._inc;
                exc = mask._exc;
                Type converter(int o) => WorldMetaStorage.GetComponentType(worldID, o);
                incTypes = inc.Select(converter).ToArray();
                excTypes = exc.Select(converter).ToArray();
            }
            public override string ToString() => CreateLogString(worldType, inc, exc);
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

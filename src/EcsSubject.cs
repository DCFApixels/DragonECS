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
            private List<int> _inc;
            private List<int> _exc;

            public EcsWorld World => _world;

            private Builder(EcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }
            internal static TSubject Build<TSubject>(EcsWorld world) where TSubject : EcsSubject
            {
                Builder builder = new Builder(world);
                Type queryType = typeof(TSubject);
                ConstructorInfo constructorInfo = queryType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
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
            public sealed override TPool Include<TComponent, TPool>()
            {
                IncludeImplicit<TComponent>();
                return _world.GetPool<TComponent, TPool>();
            }
            public sealed override TPool Exclude<TComponent, TPool>()
            {
                ExcludeImplicit<TComponent>();
                return _world.GetPool<TComponent, TPool>();
            }
            public sealed override TPool Optional<TComponent, TPool>()
            {
                return _world.GetPool<TComponent, TPool>();
            }
            private void IncludeImplicit<TComponent>()
            {
                int id = _world.GetComponentID<TComponent>();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) throw new EcsFrameworkException($"{typeof(TComponent).Name} already in constraints list.");
#endif
                _inc.Add(_world.GetComponentID<TComponent>());
            }
            private void ExcludeImplicit<TComponent>()
            {
                int id = _world.GetComponentID<TComponent>();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) throw new EcsFrameworkException($"{typeof(TComponent).Name} already in constraints list.");
#endif
                _exc.Add(_world.GetComponentID<TComponent>());
            }
            #endregion

            private void End(out EcsMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsMask(_world.Archetype, _inc.ToArray(), _exc.ToArray());
                _world = null;
                _inc = null;
                _exc = null;
            }

            #region SupportReflectionHack
#if UNITY_2020_3_OR_NEWER
            [UnityEngine.Scripting.Preserve]
#endif
            private void SupportReflectionHack<TComponent, TPool>() where TPool : IEcsPoolImplementation<TComponent>, new()
            {
                Include<TComponent, TPool>();
                Exclude<TComponent, TPool>();
                Optional<TComponent, TPool>();
                IncludeImplicit<TComponent>();
                ExcludeImplicit<TComponent>();
            }
            #endregion
        }
        #endregion

        #region Iterator
        public EcsSubjectIterator GetIterator<TSubject>() where TSubject : EcsSubject
        {
            return new EcsSubjectIterator(this, World.Entities);
        }
        public EcsSubjectIterator GetIteratorFor<TSubject>(EcsReadonlyGroup sourceGroup) where TSubject : EcsSubject
        {
            return new EcsSubjectIterator(this, sourceGroup);
        }
        #endregion
    }

    #region BuilderBase
    public abstract class EcsSubjectBuilderBase
    {
        public abstract TPool Include<TComponent, TPool>() where TPool : IEcsPoolImplementation<TComponent>, new();
        public abstract TPool Exclude<TComponent, TPool>() where TPool : IEcsPoolImplementation<TComponent>, new();
        public abstract TPool Optional<TComponent, TPool>() where TPool : IEcsPoolImplementation<TComponent>, new();
    }
    #endregion

    #region Mask
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask
    {
        internal readonly Type _worldType;
        internal readonly int[] _inc;
        internal readonly int[] _exc;
        public EcsMask(Type worldType, int[] inc, int[] exc)
        {
            _worldType = worldType;
            _inc = inc;
            _exc = exc;
        }

        public override string ToString() => CreateLogString(_worldType, _inc, _exc);

        #region Debug utils
        private static string CreateLogString(Type worldType, int[] inc, int[] exc)
        {
#if DEBUG
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

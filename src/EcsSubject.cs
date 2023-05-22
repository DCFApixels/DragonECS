using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public bool IsMatches(int entityID) => source.IsMaskCompatible(mask, entityID);
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

            public void IncludeImplicit<TComponent>()
            {
                int id = _world.GetComponentID<TComponent>();
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
                if (_inc.Contains(id) || _exc.Contains(id)) throw new EcsFrameworkException($"{typeof(TComponent).Name} already in constraints list.");
#endif
                _inc.Add(_world.GetComponentID<TComponent>());
            }
            public void ExcludeImplicit<TComponent>()
            {
                int id = _world.GetComponentID<TComponent>();
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
                if (_inc.Contains(id) || _exc.Contains(id)) throw new EcsFrameworkException($"{typeof(TComponent).Name} already in constraints list.");
#endif
                _exc.Add(_world.GetComponentID<TComponent>());
            }

            private void End(out EcsMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsMask(_world.Archetype, _inc.ToArray(), _exc.ToArray());
                _world = null;
                _inc = null;
                _exc = null;
            }
        }
        #endregion
    }

    public static class EcsSubjectExtensions
    {
        public static EcsSubjectIterator<TSubject> GetIterator<TSubject>(this TSubject self) where TSubject : EcsSubject
        {
            return new EcsSubjectIterator<TSubject>(self, self.World.Entities);
        }
        public static EcsSubjectIterator<TSubject> GetIteratorFor<TSubject>(this TSubject self, EcsReadonlyGroup sourceGroup) where TSubject : EcsSubject
        {
            return new EcsSubjectIterator<TSubject>(self, sourceGroup);
        }
    }

    #region BuilderBase
    public abstract class EcsSubjectBuilderBase
    {
        public abstract TPool Include<TComponent, TPool>() where TComponent : struct where TPool : IEcsPoolImplementation<TComponent>, new();
        public abstract TPool Exclude<TComponent, TPool>() where TComponent : struct where TPool : IEcsPoolImplementation<TComponent>, new();
        public abstract TPool Optional<TComponent, TPool>() where TComponent : struct where TPool : IEcsPoolImplementation<TComponent>, new();
    }
    #endregion

    #region Mask
    public sealed class EcsMask
    {
        internal readonly Type WorldType;
        internal readonly int[] Inc;
        internal readonly int[] Exc;

        public EcsMask(Type worldType, int[] inc, int[] exc)
        {
            WorldType = worldType;
            Inc = inc;
            Exc = exc;
        }

        public override string ToString()
        {
            return $"Inc({string.Join(", ", Inc)}) Exc({string.Join(", ", Exc)})";
        }
    }
    #endregion

    #region Iterator
    public ref struct EcsSubjectIterator<TSubject> where TSubject : EcsSubject
    {
        public readonly TSubject s;
        private EcsReadonlyGroup sourceGroup;
        private Enumerator enumerator;

        public EcsSubjectIterator(TSubject s, EcsReadonlyGroup sourceGroup)
        {
            this.s = s;
            this.sourceGroup = sourceGroup;
            enumerator = default;
        }

        public int Entity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => enumerator.Current;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin() => enumerator = GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Next() => enumerator.MoveNext();
        public void CopyTo(EcsGroup group)
        {
            group.Clear();
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
                group.AggressiveAdd(enumerator.Current);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(sourceGroup, s);

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

        public ref struct Enumerator
        {
            private EcsGroup.Enumerator _sourceGroup;
            private readonly int[] _inc;
            private readonly int[] _exc;
            private readonly IEcsPoolImplementation[] _pools;

            public Enumerator(EcsReadonlyGroup sourceGroup, EcsSubject subject)
            {
                _sourceGroup = sourceGroup.GetEnumerator();
                _inc = subject.mask.Inc;
                _exc = subject.mask.Exc;
                _pools = subject.World.pools;
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
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public abstract class EcsQueryBase
    {
        internal EcsWorld source;
        internal EcsGroup groupFilter;
        internal EcsQueryMask mask;

        private bool _isInit;

        #region Properties
        public EcsQueryMask Mask => mask;
        public EcsWorld World => source;
        public bool IsInit => _isInit;

        public abstract long WhereVersion { get; }
        #endregion

        #region Builder
        protected virtual void Init(Builder b) { }
        protected abstract void OnBuild(Builder b);
        public sealed class Builder : EcsQueryBuilderBase
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
            internal static TQuery Build<TQuery>(EcsWorld world) where TQuery : EcsQueryBase
            {
                Builder builder = new Builder(world);
                Type queryType = typeof(TQuery);
                ConstructorInfo constructorInfo = queryType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                EcsQueryBase newQuery;
                if (constructorInfo != null)
                {
                    newQuery = (EcsQueryBase)constructorInfo.Invoke(new object[] { builder });
                }
                else
                {
                    newQuery = (EcsQueryBase)Activator.CreateInstance(typeof(TQuery));
                    newQuery.Init(builder);
                }
                newQuery.groupFilter = EcsGroup.New(world);
                newQuery.source = world;
                newQuery.OnBuild(builder);
                builder.End(out newQuery.mask);
                newQuery._isInit = true;
                return (TQuery)(object)newQuery;
            }

            public sealed override TPool Include<TComponent, TPool>()
            {
                _inc.Add(_world.GetComponentID<TComponent>());
                return _world.GetPool<TComponent, TPool>();
            }
            public sealed override TPool Exclude<TComponent, TPool>()
            {
                _exc.Add(_world.GetComponentID<TComponent>());
                return _world.GetPool<TComponent, TPool>();
            }
            public sealed override TPool Optional<TComponent, TPool>()
            {
                return _world.GetPool<TComponent, TPool>();
            }

            private void End(out EcsQueryMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsQueryMask(_world.Archetype, _inc.ToArray(), _exc.ToArray());
                _world = null;
                _inc = null;
                _exc = null;
            }
        }
        #endregion
        public abstract WhereResult Where();

        protected void ExecuteWhere(EcsReadonlyGroup group, EcsGroup result)
        {
            var pools = World.pools;
            result.Clear();
            foreach (var e in group)
            {
                for (int i = 0, iMax = mask.Inc.Length; i < iMax; i++)
                {
                    if (!pools[mask.Inc[i]].Has(e))
                        goto next;
                }
                for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
                {
                    if (pools[mask.Exc[i]].Has(e))
                        goto next;
                }
                result.AggressiveAdd(e);
                next: continue;
            }
        }
        //protected void IsMaskCompatible
        protected void ExecuteWhereAndSort(EcsReadonlyGroup group, EcsGroup result)
        {
            ExecuteWhere(group, result);
            result.Sort();
        }
    }

    //TODO есть идея проверки того что запросбылвыполнен путем создания ref struct которыйсодержит результат выполнения заапроса и существует только в стеке.
    //таким образом для каждой системы он будет выполняться единожды, без холостых перезапусков, скорее всего эта система будет работать лучше чем "Версии запросов"
    public abstract class EcsQuery : EcsQueryBase
    {
        private ProfilerMarker _execute = new ProfilerMarker("EcsQuery.Where");

        private long _executeWhereVersion = 0;

        #region Properties
        //на данный момент бесполное свойство, основная идея, реализовать все методы запроса с аргументом (..., ref long version),
        //далее в теле метододов сравнивать с текущей версией, и если они отличаются, выполнять запрос и перезаписывать значение version
        //таким образом можно добиться сокращение "холостых" выполнений запроса, тоесть в рамках одной системы запрос всегда будет выполняться один раз
        //даже при повторном вызове.
        //Но нужно добавить метод для принудительного повторения запроса без сравнения вресий.
        //TODO реализовать описанное выше поведение
        //TODO проверить что лучше подходит int или long. long делает этот механизм очень надежным, но возможно его использование будет существенно влиять на производительность.
        public sealed override long WhereVersion => _executeWhereVersion;
        #endregion

        protected sealed override void OnBuild(Builder b) { }
        public sealed override WhereResult Where()
        {
            using (_execute.Auto())
            {
                ExecuteWhereAndSort(World.Entities, groupFilter);
                return new WhereResult(this, ++_executeWhereVersion);
            }
        }

        public bool Has(int entityID)
        {
            return groupFilter.Has(entityID);
        }
    }

    public class EcsQueryMask : EcsComponentMask
    {
        public EcsQueryMask(Type worldArchetypeType, int[] inc, int[] exc)
        {
            WorldArchetype = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }
    public abstract class EcsQueryBuilderBase
    {
        public abstract TPool Include<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
        public abstract TPool Exclude<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
        public abstract TPool Optional<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 16)]
    public readonly ref struct WhereResult
    {
        public readonly EcsQueryBase query; //ref = 8 byte
        public readonly long version; //long = 8 byte

        #region Properties
        public bool IsNull => query == null;
        public EcsWorld World => query.World;
        public bool IsActual => query.WhereVersion == version;
        #endregion

        public WhereResult(EcsQueryBase query, long version)
        {
            this.query = query;
            this.version = version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => query.groupFilter.GetEnumerator();
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsQuery
    {
        internal void AddEntity(int entityID);
        internal void RemoveEntity(int entityID);
        public EcsQueryMask Mask { get; }
    }
    public abstract class EcsQueryBase : IEcsQuery
    {
        internal EcsGroup group;
        internal EcsQueryMask mask;

        public EcsQueryMask Mask => mask;

        public void AddEntity(int entityID) => group.Add(entityID);
        public void RemoveEntity(int entityID) => group.Remove(entityID);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsQuery.AddEntity(int entityID) => group.Add(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsQuery.RemoveEntity(int entityID) => group.Remove(entityID);
    }

    public abstract class EcsQuery<TWorldArchetype> : EcsQueryBase
        where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        private int _id;

        public int ID => _id;
        public EcsReadonlyGroup entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => group.Readonly;
        }

        public EcsGroup.Enumerator GetEnumerator() => group.GetEnumerator();

        protected virtual void Init(Builder b) { }

        #region Builder
        public sealed class Builder : EcsQueryBuilder
        {
            private IEcsWorld _world;
            private List<int> _inc;
            private List<int> _exc;

            internal static TQuery Build<TQuery>(IEcsWorld world) where TQuery : IEcsQuery
            {
                Builder builder = new Builder(world);

                Type queryType = typeof(TQuery);
                ConstructorInfo constructorInfo = queryType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Builder) }, null);
                EcsQuery<TWorldArchetype> newQuery;
                if (constructorInfo != null)
                {
                    newQuery = (EcsQuery<TWorldArchetype>)constructorInfo.Invoke(new object[] { builder });
                }
                else
                {
                    newQuery = (EcsQuery<TWorldArchetype>)Activator.CreateInstance(typeof(TQuery));
                    newQuery.Init(builder);
                }

                builder.End(out newQuery.mask);
                newQuery.group = new EcsGroup(world);
                return (TQuery)(object)newQuery;
            }

            private Builder(IEcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }

            #region Init query member methods
            public override inc<TComponent> Include<TComponent>() where TComponent : struct
            {
                _inc.Add(_world.GetComponentID<TComponent>());
                return new inc<TComponent>(_world.GetPool<TComponent>());
            }
            public override exc<TComponent> Exclude<TComponent>() where TComponent : struct
            {
                _exc.Add(_world.GetComponentID<TComponent>());
                return new exc<TComponent>(_world.GetPool<TComponent>());
            }
            public override opt<TComponent> Optional<TComponent>() where TComponent : struct
            {
                return new opt<TComponent>(_world.GetPool<TComponent>());
            }
            #endregion

            private void End(out EcsQueryMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsQueryMask(_world.ArchetypeType, _inc.ToArray(), _exc.ToArray());

                _world = null;
                _inc.Clear();
                _inc = null;
                _exc.Clear();
                _exc = null;
            }
        }
        #endregion
    }

    public class EcsQueryMask : EcsComponentMask
    {
        public EcsQueryMask(Type worldArchetypeType, int[] inc, int[] exc)
        {
            WorldArchetypeType = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }
    public abstract class EcsQueryBuilder
    {
        public abstract inc<TComponent> Include<TComponent>() where TComponent : struct;
        public abstract exc<TComponent> Exclude<TComponent>() where TComponent : struct;
        public abstract opt<TComponent> Optional<TComponent>() where TComponent : struct;
    }
}

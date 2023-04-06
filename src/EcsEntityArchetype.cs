using DCFApixels.DragonECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public abstract class EcsEntityArchetypeBuilder
    {
        public abstract inc<TComponent> Include<TComponent>() where TComponent : struct;
        public abstract exc<TComponent> Exclude<TComponent>() where TComponent : struct;
        public abstract opt<TComponent> Optional<TComponent>() where TComponent : struct;
    }
    public interface IEcsEntityArchetype
    {
        internal void AddEntity(int entityID);
        internal void RemoveEntity(int entityID);
    }
    public class EcsEntityArchetype<TWorldArchetype> : IEcsEntityArchetype
        where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        private int _id;
        internal EcsGroup group;

        public int ID => _id;
        public EcsReadonlyGroup entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => group.Readonly;
        }

        public EcsEntityArchetype(Builder b)
        {
        }

        public EcsGroup.Enumerator GetEnumerator() => group.GetEnumerator();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsEntityArchetype.AddEntity(int entityID) => group.Add(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsEntityArchetype.RemoveEntity(int entityID) => group.Remove(entityID);

        #region Builder
        public sealed class Builder : EcsEntityArchetypeBuilder
        {
            private IEcsWorld _world;
            private List<int> _inc;
            private List<int> _exc;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Builder(IEcsWorld world)
            {
                _world = world;
                _inc = new List<int>(8);
                _exc = new List<int>(4);
            }

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

            internal void End(out EcsEntityArhetypeMask mask)
            {
                _inc.Sort();
                _exc.Sort();
                mask = new EcsEntityArhetypeMask(_world.ArchetypeType, _inc.ToArray(), _exc.ToArray());
                _world = null;
                _inc.Clear();
                _inc = null;
                _exc.Clear();
                _exc = null;
            }
        }
        #endregion
    }

    public class EcsEntityArhetypeMask
    {
        internal readonly Type WorldArchetypeType;
        internal readonly int[] Inc;
        internal readonly int[] Exc;

        public int IncCount => Inc.Length;
        public int ExcCount => Exc.Length;
        public EcsEntityArhetypeMask(Type worldArchetypeType, int[] inc, int[] exc)
        {
            WorldArchetypeType = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }

}

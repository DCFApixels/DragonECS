using DCFApixels.DragonECS;
/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.WSA;
using static UnityEditor.Experimental.GraphView.Port;

namespace DCFApixels.Assets.DragonECS.src
{
    public struct Pose { }
    public struct Health { }
    public struct Mana { }
    public struct EnemyTag { }

    public class TestArhetype : EcsEntityArhetype<EcsDefaultWrold>
    {
        public inc<Pose> pose;
        public inc<Health> health;
        public opt<Mana> mana;
        public exc<EnemyTag> enemyTag;

        public TestArhetype(Builder b) : base(b)
        {
            pose = b.Include<Pose>();
            health = b.Include<Health>();
            mana = b.Optional<Mana>();
            enemyTag = b.Exclude<EnemyTag>();
        }
    }
    public class TestSystem : IEcsRunSystem
    {
        private TestArhetype test;
        public void Run(EcsPipeline pipeline)
        {
            foreach (var e in test)
            {
                test.health.Read(e.id);
                test.pose.Write(e.id) = new Pose();
            }
        }
    }










    public abstract class EcsWorldArhetype
    {
        public EcsWorldArhetype(IEcsWorld world) { }
    }

    public interface IFakeWorld { }
    public class FakeWorld<TWorldArhetype> : IFakeWorld
        where TWorldArhetype : EcsWorldArhetype
    {
        public readonly TWorldArhetype data;

        private EcsEntityArhetype<TWorldArhetype>[] _arhetypes;


        public FakeWorld()
        {
            _arhetypes = new EcsEntityArhetype<TWorldArhetype>[ArhetypeID.capacity];
        }

        public TArhetype Arhetype<TArhetype>() where TArhetype : EcsEntityArhetype<TWorldArhetype>
        {
            int id = ArhetypeID<IEcsEntityArhetype>.id;
            if (_arhetypes.Length < ArhetypeID.capacity)
                Array.Resize(ref _arhetypes, ArhetypeID.capacity);

            if (_arhetypes[id] == null)
            {
                EcsEntityArhetype<TWorldArhetype>.Builder builder = new EcsEntityArhetype<TWorldArhetype>.Builder(this);
                _arhetypes[id] = (TArhetype)Activator.CreateInstance(typeof(TArhetype), builder);
                builder.End();
            }

            return (TArhetype)_arhetypes[id];
        }


        #region ArhetypeID
        private static class ArhetypeID
        {
            public static int count = 0;
            public static int capacity = 128;
        }
        private static class ArhetypeID<TArhetype>
        {
            public static int id;
            static ArhetypeID()
            {
                id = ArhetypeID.count++;
                if (ArhetypeID.count > ArhetypeID.capacity)
                    ArhetypeID.capacity <<= 1;
            }
        }
        #endregion
    }

    public interface IEcsEntityArhetype
    {
        internal void AddEntity(int entityID);
        internal void RemoveEntity(int entityID);
    }
    public class EcsEntityArhetype<TWorldArhetype> : IEcsEntityArhetype
        where TWorldArhetype : EcsWorldArhetype
    {
        private int _id;
        private EcsGroup _group;

        public int ID => _id;
        public EcsReadonlyGroup entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _group.Readonly;
        }

        public EcsEntityArhetype(Builder b) { }

        public EcsGroup.Enumerator GetEnumerator() => _group.GetEnumerator();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsEntityArhetype.AddEntity(int entityID) => _group.Add(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsEntityArhetype.RemoveEntity(int entityID) => _group.Remove(entityID);

        #region Builder
        public class Builder
        {
            private IFakeWorld _world;
            private List<int> _inc;
            private List<int> _exc;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Builder(IFakeWorld world)
            {
                _world = world;
            }

            public inc<TComponent> Include<TComponent>() where TComponent : struct
            {
                _inc.Add(_world.GetComponentID<TComponent>());
                return new inc<TComponent>(_world.GetPool<TComponent>());
            }
            public exc<TComponent> Exclude<TComponent>() where TComponent : struct
            {
                _exc.Add(_world.GetComponentID<TComponent>());
                return new exc<TComponent>(_world.GetPool<TComponent>());
            }
            public opt<TComponent> Optional<TComponent>() where TComponent : struct
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


    public interface IEcsFiled<TComponent>
        where TComponent : struct
    {
        public ref TComponent Write(int entityID);
        public ref readonly TComponent Read(int entityID);
        public bool Has(int entityID);
        public void Del(int entityID);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct inc<TComponent> : IEcsFiled<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal inc(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(int entityID) => ref _pool.Write(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(int entityID) => ref _pool.Read(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _pool.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => _pool.Del(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}inc<{typeof(TComponent).Name}>";
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct exc<TComponent> : IEcsFiled<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal exc(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(int entityID) => ref _pool.Write(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(int entityID) => ref _pool.Read(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _pool.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => _pool.Del(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}exc<{typeof(TComponent).Name}>";
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct opt<TComponent> : IEcsFiled<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal opt(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(int entityID) => ref _pool.Write(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(int entityID) => ref _pool.Read(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _pool.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => _pool.Del(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}opt<{typeof(TComponent).Name}>";
    }

    public class EcsEntityArhetypeMask
    {
        internal readonly Type WorldArchetypeType;
        internal readonly int[] Inc;
        internal readonly int[] Exc;
        public EcsEntityArhetypeMask(Type worldArchetypeType, int[] inc, int[] exc)
        {
            WorldArchetypeType = worldArchetypeType;
            Inc = inc;
            Exc = exc;
        }
    }

}
*/
#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Core.Unchecked
{
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    [DebuggerTypeProxy(typeof(EntityDebuggerProxy))]
    public struct EntitySlotInfo : IEquatable<EntitySlotInfo>
    {
#if UNITY_EDITOR
        [UnityEngine.SerializeField]
#endif
        [FieldOffset(0)]
        public long full; //Union
        [FieldOffset(0), NonSerialized]
        public int id;
        [FieldOffset(4), NonSerialized]
        public short gen;
        [FieldOffset(6), NonSerialized]
        public short worldID;

        #region Properties
        public EcsWorld World { get { return EcsWorld.GetWorld(worldID); } }
        public StateFlag State { get { return full == 0 ? StateFlag.Null : World.IsAlive(id, gen) ? StateFlag.Alive : StateFlag.Dead; } }
        #endregion

        #region Constructors/Deconstructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntitySlotInfo(long full) : this()
        {
            this.full = full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntitySlotInfo(int id, short gen, short world) : this()
        {
            this.id = id;
            this.gen = gen;
            worldID = world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int gen, out int worldID)
        {
            id = this.id;
            gen = this.gen;
            worldID = this.worldID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int id, out int worldID)
        {
            id = this.id;
            worldID = this.worldID;
        }
        #endregion

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EntitySlotInfo a, EntitySlotInfo b) { return a.full == b.full; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(EntitySlotInfo a, EntitySlotInfo b) { return a.full != b.full; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator EntitySlotInfo(entlong a) { return new EntitySlotInfo(a._full); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator entlong(EntitySlotInfo a) { return new entlong(a.full); }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return unchecked(id ^ gen ^ (worldID * EcsConsts.MAGIC_PRIME)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() { return $"slot(id:{id} g:{gen} w:{worldID} {(State == StateFlag.Null ? "null" : State == StateFlag.Alive ? "alive" : "not alive")})"; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) { return obj is EntitySlotInfo other && this == other; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EntitySlotInfo other) { return this == other; }

        public enum StateFlag { Null, Dead, Alive, }
        #endregion
    }

    internal class EntityDebuggerProxy
    {
        private List<object> _componentsList = new List<object>();
        private EntitySlotInfo _info;
        public virtual long full { get { return _info.full; } }
        public virtual int id { get { return _info.id; } }
        public virtual short gen { get { return _info.gen; } }
        public virtual short worldID { get { return _info.worldID; } }
        public virtual EntitySlotInfo.StateFlag State { get { return _info.State; } }
        public virtual EcsWorld World { get { return _info.World; } }
        public virtual IEnumerable<object> Components
        {
            get
            {
                if (State == EntitySlotInfo.StateFlag.Alive)
                {
                    World.GetComponentsFor(id, _componentsList);
                    return _componentsList;
                }
                return Array.Empty<object>();
            }
            set
            {
                if (State == EntitySlotInfo.StateFlag.Alive)
                {
                    foreach (var component in value)
                    {
                        if (component == null) { continue; }
                        var componentType = component.GetType();
                        var world = World;

                        if (componentType.IsValueType && world.TryFindPoolInstance(componentType, out IEcsPool pool))
                        {
                            pool.SetRaw(id, component);
                        }
                    }
                }
            }
        }
        public EntityDebuggerProxy(EntitySlotInfo info)
        {
            _info = info;
        }
        public EntityDebuggerProxy(entlong info)
        {
            _info = (EntitySlotInfo)info;
        }
        public EntityDebuggerProxy(int entityID, short gen, short worldID)
        {
            _info = new EntitySlotInfo(entityID, gen, worldID);
        }
    }
}
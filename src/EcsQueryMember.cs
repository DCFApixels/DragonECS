using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsQueryMember<TComponent>
            where TComponent : struct
    {
        public ref TComponent Write(ent entityID);
        public ref readonly TComponent Read(ent entityID);
        public bool Has(ent entityID);
        public void Del(ent entityID);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct inc<TComponent> : IEcsQueryMember<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal inc(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entityID) => ref _pool.Write(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entityID) => ref _pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => _pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => _pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}inc<{typeof(TComponent).Name}>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator inc<TComponent>(EcsQueryBuilder buider) => buider.Include<TComponent>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct exc<TComponent> : IEcsQueryMember<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal exc(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entityID) => ref _pool.Write(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entityID) => ref _pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => _pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => _pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}exc<{typeof(TComponent).Name}>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator exc<TComponent>(EcsQueryBuilder buider) => buider.Exclude<TComponent>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct opt<TComponent> : IEcsQueryMember<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal opt(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entityID) => ref _pool.Write(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entityID) => ref _pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => _pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => _pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}opt<{typeof(TComponent).Name}>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator opt<TComponent>(EcsQueryBuilder buider) => buider.Optional<TComponent>();
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsQueryMember<TComponent>
            where TComponent : struct
    {
        public ref TComponent Write(int entityID);
        public ref readonly TComponent Read(int entityID);
        public bool Has(int entityID);
        public void Del(int entityID);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct inc<TComponent> : IEcsQueryMember<TComponent>
        where TComponent : struct
    {
        private readonly EcsPool<TComponent> _pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal inc(EcsPool<TComponent> pool) => _pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entity) => ref _pool.Write(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(int entityID) => ref _pool.Write(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entity) => ref _pool.Read(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(int entityID) => ref _pool.Read(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entity) => _pool.Has(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _pool.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entity) => _pool.Del(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => _pool.Del(entityID);
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
        public ref TComponent Write(ent entity) => ref _pool.Write(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(int entityID) => ref _pool.Write(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entity) => ref _pool.Read(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(int entityID) => ref _pool.Read(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entity) => _pool.Has(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _pool.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entity) => _pool.Del(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => _pool.Del(entityID);
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
        public ref TComponent Write(ent entity) => ref _pool.Write(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(int entityID) => ref _pool.Write(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entity) => ref _pool.Read(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(int entityID) => ref _pool.Read(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entity) => _pool.Has(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _pool.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entity) => _pool.Del(entity.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID) => _pool.Del(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"{(_pool == null ? "NULL" : _pool.World.ArchetypeType.Name)}opt<{typeof(TComponent).Name}>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator opt<TComponent>(EcsQueryBuilder buider) => buider.Optional<TComponent>();
    }
}

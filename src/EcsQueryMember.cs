using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsQueryMember { }
    public interface IEcsQueryReadonlyField<TComponent> : IEcsQueryMember
    {
        public ref readonly TComponent Read(ent entityID);
        public bool Has(ent entityID);
    }
    public interface IEcsQueryField<TComponent> : IEcsQueryReadonlyField<TComponent>
            where TComponent : struct
    {
        public ref TComponent Add(ent entityID);
        public ref TComponent Write(ent entityID);
        public void Del(ent entityID);
    }

    #region select
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct inc_<TComponent> : IEcsQueryField<TComponent>
        where TComponent : struct
    {
        internal readonly EcsPool<TComponent> pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal inc_(EcsPool<TComponent> pool) => this.pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Add(ent entityID) => ref pool.Add(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entityID) => ref pool.Write(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entityID) => ref pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator inc_<TComponent>(EcsQueryBuilderBase buider) => buider.Include<TComponent>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct exc_<TComponent> : IEcsQueryField<TComponent>
        where TComponent : struct
    {
        internal readonly EcsPool<TComponent> pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal exc_(EcsPool<TComponent> pool) => this.pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Add(ent entityID) => ref pool.Add(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entityID) => ref pool.Write(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entityID) => ref pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator exc_<TComponent>(EcsQueryBuilderBase buider) => buider.Exclude<TComponent>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    public readonly struct opt_<TComponent> : IEcsQueryField<TComponent>
        where TComponent : struct
    {
        internal readonly EcsPool<TComponent> pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal opt_(EcsPool<TComponent> pool) => this.pool = pool;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Add(ent entityID) => ref pool.Add(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TComponent Write(ent entityID) => ref pool.Write(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TComponent Read(ent entityID) => ref pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator opt_<TComponent>(EcsQueryBuilderBase buider) => buider.Optional<TComponent>();
    }
    #endregion
}

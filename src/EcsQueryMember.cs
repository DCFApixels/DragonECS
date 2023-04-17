using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsQueryMember { }
    public interface IEcsQueryReadonlyField<TComponent> : IEcsQueryMember
    {
        public ref TComponent Read(ent entityID);
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
        public ref TComponent Read(ent entityID) => ref pool.Read(entityID.id);
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
        public ref TComponent Read(ent entityID) => ref pool.Read(entityID.id);
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
        public ref TComponent Read(ent entityID) => ref pool.Read(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(ent entityID) => pool.Has(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(ent entityID) => pool.Del(entityID.id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator opt_<TComponent>(EcsQueryBuilderBase buider) => buider.Optional<TComponent>();
    }
    #endregion

   // #region select_readonly
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct inc_readonly_<TComponent> : IEcsQueryReadonlyField<TComponent>
   //     where TComponent : struct
   // {
   //     internal readonly EcsPool<TComponent> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal inc_readonly_(EcsPool<TComponent> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref TComponent Read(ent entityID) => ref pool.Read(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //
   //     public static implicit operator inc_readonly_<TComponent>(EcsQueryBuilderBase buider) => buider.Include<TComponent>();
   //     public static implicit operator inc_readonly_<TComponent>(inc_<TComponent> o) => new inc_readonly_<TComponent>(o.pool);
   // }
   //
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct exc_readonly_<TComponent> : IEcsQueryReadonlyField<TComponent>
   //     where TComponent : struct
   // {
   //     internal readonly EcsPool<TComponent> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal exc_readonly_(EcsPool<TComponent> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref TComponent Read(ent entityID) => ref pool.Read(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //
   //     public static implicit operator exc_readonly_<TComponent>(EcsQueryBuilderBase buider) => buider.Exclude<TComponent>();
   //     public static implicit operator exc_readonly_<TComponent>(exc_<TComponent> o) => new exc_readonly_<TComponent>(o.pool);
   // }
   //
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct opt_readonly_<TComponent> : IEcsQueryReadonlyField<TComponent>
   //     where TComponent : struct
   // {
   //     internal readonly EcsPool<TComponent> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal opt_readonly_(EcsPool<TComponent> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref TComponent Read(ent entityID) => ref pool.Read(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //
   //     public static implicit operator opt_readonly_<TComponent>(EcsQueryBuilderBase buider) => buider.Optional<TComponent>();
   //     public static implicit operator opt_readonly_<TComponent>(opt_<TComponent> o) => new opt_readonly_<TComponent>(o.pool);
   // }
   // #endregion
   //
   // #region join
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct attach : IEcsQueryField<Attach>
   // {
   //     internal readonly EcsPool<Attach> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal attach(EcsPool<Attach> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref Attach Add(ent entityID) => ref pool.Add(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref Attach Write(ent entityID) => ref pool.Write(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref Attach Read(ent entityID) => ref pool.Read(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public void Del(ent entityID) => pool.Del(entityID.id);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static implicit operator attach(EcsQueryBuilderBase buider) => buider.Include<Attach>();
   //     public static implicit operator attach(inc_<Attach> o) => new attach(o.pool);
   // }
   // #endregion
}

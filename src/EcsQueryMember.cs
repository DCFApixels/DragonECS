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

   // #region select_readonly
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct inc_readonly_<T> : IEcsQueryReadonlyField<T>
   //     where T : struct
   // {
   //     internal readonly EcsPool<T> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal inc_readonly_(EcsPool<T> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref T Read(ent entityID) => ref pool.Read(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //
   //     public static implicit operator inc_readonly_<T>(EcsQueryBuilderBase buider) => buider.Include<T>();
   //     public static implicit operator inc_readonly_<T>(inc_<T> o) => new inc_readonly_<T>(o.pool);
   // }
   //
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct exc_readonly_<T> : IEcsQueryReadonlyField<T>
   //     where T : struct
   // {
   //     internal readonly EcsPool<T> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal exc_readonly_(EcsPool<T> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref T Read(ent entityID) => ref pool.Read(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //
   //     public static implicit operator exc_readonly_<T>(EcsQueryBuilderBase buider) => buider.Exclude<T>();
   //     public static implicit operator exc_readonly_<T>(exc_<T> o) => new exc_readonly_<T>(o.pool);
   // }
   //
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct opt_readonly_<T> : IEcsQueryReadonlyField<T>
   //     where T : struct
   // {
   //     internal readonly EcsPool<T> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal opt_readonly_(EcsPool<T> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref T Read(ent entityID) => ref pool.Read(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //
   //     public static implicit operator opt_readonly_<T>(EcsQueryBuilderBase buider) => buider.Optional<T>();
   //     public static implicit operator opt_readonly_<T>(opt_<T> o) => new opt_readonly_<T>(o.pool);
   // }
   // #endregion
   //
   // #region join
   // [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
   // public readonly struct attach : IEcsQueryField<Edge>
   // {
   //     internal readonly EcsPool<Edge> pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     internal attach(EcsPool<Edge> pool) => this.pool = pool;
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref Edge Add(ent entityID) => ref pool.Add(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref Edge Write(ent entityID) => ref pool.Write(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public ref Edge Read(ent entityID) => ref pool.Read(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public bool Has(ent entityID) => pool.Has(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public void Del(ent entityID) => pool.Del(entityID.uniqueID);
   //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   //     public static implicit operator attach(EcsQueryBuilderBase buider) => buider.Include<Edge>();
   //     public static implicit operator attach(inc_<Edge> o) => new attach(o.pool);
   // }
   // #endregion
}

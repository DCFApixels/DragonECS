#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.PoolsCore;

namespace DCFApixels.DragonECS
{
    public sealed class EmptyAspect : EcsAspect { }

    public sealed class SinglePoolAspect<TPool> : EcsAspect where TPool : IEcsPoolImplementation, new()
    {
        public readonly TPool pool = B.IncludePool<TPool>();
    }
    public sealed class SingleTagAspect<TComponent> : EcsAspect where TComponent : struct, IEcsTagComponent
    {
        public readonly EcsTagPool<TComponent> pool = B.IncludePool<EcsTagPool<TComponent>>();
    }
    public sealed class SingleAspect<TComponent> : EcsAspect where TComponent : struct, IEcsComponent
    {
        public readonly EcsPool<TComponent> pool = B.IncludePool<EcsPool<TComponent>>();
    }
    public sealed class CombinedAspect<A0, A1> : EcsAspect
        where A0 : EcsAspect, new()
        where A1 : EcsAspect, new()
    {
        public readonly A0 a0 = B.Combine<A0>();
        public readonly A1 a1 = B.Combine<A1>();
        public void Deconstruct(out A0 a0, out A1 a1)
        {
            a0 = this.a0;
            a1 = this.a1;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2> : EcsAspect
        where A0 : EcsAspect, new()
        where A1 : EcsAspect, new()
        where A2 : EcsAspect, new()
    {
        public readonly A0 a0 = B.Combine<A0>();
        public readonly A1 a1 = B.Combine<A1>();
        public readonly A2 a2 = B.Combine<A2>();
        public void Deconstruct(out A0 a0, out A1 a1, out A2 a2)
        {
            a0 = this.a0;
            a1 = this.a1;
            a2 = this.a2;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2, A3> : EcsAspect
        where A0 : EcsAspect, new()
        where A1 : EcsAspect, new()
        where A2 : EcsAspect, new()
        where A3 : EcsAspect, new()
    {
        public readonly A0 a0 = B.Combine<A0>();
        public readonly A1 a1 = B.Combine<A1>();
        public readonly A2 a2 = B.Combine<A2>();
        public readonly A3 a3 = B.Combine<A3>();
        public void Deconstruct(out A0 a0, out A1 a1, out A2 a2, out A3 a3)
        {
            a0 = this.a0;
            a1 = this.a1;
            a2 = this.a2;
            a3 = this.a3;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2, A3, A4> : EcsAspect
        where A0 : EcsAspect, new()
        where A1 : EcsAspect, new()
        where A2 : EcsAspect, new()
        where A3 : EcsAspect, new()
        where A4 : EcsAspect, new()
    {
        public readonly A0 a0 = B.Combine<A0>();
        public readonly A1 a1 = B.Combine<A1>();
        public readonly A2 a2 = B.Combine<A2>();
        public readonly A3 a3 = B.Combine<A3>();
        public readonly A4 a4 = B.Combine<A4>();
        public void Deconstruct(out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
        {
            a0 = this.a0;
            a1 = this.a1;
            a2 = this.a2;
            a3 = this.a3;
            a4 = this.a4;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2, A3, A4, A5> : EcsAspect
        where A0 : EcsAspect, new()
        where A1 : EcsAspect, new()
        where A2 : EcsAspect, new()
        where A3 : EcsAspect, new()
        where A4 : EcsAspect, new()
        where A5 : EcsAspect, new()
    {
        public readonly A0 a0 = B.Combine<A0>();
        public readonly A1 a1 = B.Combine<A1>();
        public readonly A2 a2 = B.Combine<A2>();
        public readonly A3 a3 = B.Combine<A3>();
        public readonly A4 a4 = B.Combine<A4>();
        public readonly A5 a5 = B.Combine<A5>();
        public void Deconstruct(out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
        {
            a0 = this.a0;
            a1 = this.a1;
            a2 = this.a2;
            a3 = this.a3;
            a4 = this.a4;
            a5 = this.a5;
        }
    }

    public static class CombinedAspectExtensions
    {
        #region Where 2
        public static EcsSpan Where<TCollection, A0, A1>(this TCollection entities, out A0 a0, out A1 a1)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1);
        }
        public static EcsSpan Where<A0, A1>(this EcsReadonlyGroup group, out A0 a0, out A1 a1)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
        {
            return group.ToSpan().Where(out a0, out a1);
        }
        public static EcsSpan Where<A0, A1>(this EcsSpan span, out A0 a0, out A1 a1)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
        {
            var result = span.Where(out CombinedAspect<A0, A1> combined);
            (a0, a1) = combined;
            return result;
        }
        #endregion

        #region Where 3
        public static EcsSpan Where<TCollection, A0, A1, A2>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2);
        }
        public static EcsSpan Where<A0, A1, A2>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
        {
            return group.ToSpan().Where(out a0, out a1, out a2);
        }
        public static EcsSpan Where<A0, A1, A2>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2> combined);
            (a0, a1, a2) = combined;
            return result;
        }
        #endregion

        #region Where 4
        public static EcsSpan Where<TCollection, A0, A1, A2, A3>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2, out a3);
        }
        public static EcsSpan Where<A0, A1, A2, A3>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
        {
            return group.ToSpan().Where(out a0, out a1, out a2, out a3);
        }
        public static EcsSpan Where<A0, A1, A2, A3>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2, A3> combined);
            (a0, a1, a2, a3) = combined;
            return result;
        }
        #endregion

        #region Where 5
        public static EcsSpan Where<TCollection, A0, A1, A2, A3, A4>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
        {
            return group.ToSpan().Where(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2, A3, A4> combined);
            (a0, a1, a2, a3, a4) = combined;
            return result;
        }
        #endregion

        #region Where 6
        public static EcsSpan Where<TCollection, A0, A1, A2, A3, A4, A5>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where A5 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4, A5>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where A5 : EcsAspect, new()
        {
            return group.ToSpan().Where(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4, A5>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where A5 : EcsAspect, new()
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2, A3, A4, A5> combined);
            (a0, a1, a2, a3, a4, a5) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 2
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1>(this TCollection entities, out A0 a0, out A1 a1)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1>(this EcsReadonlyGroup group, out A0 a0, out A1 a1)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
        {
            return group.ToSpan().WhereToGroup(out a0, out a1);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1>(this EcsSpan span, out A0 a0, out A1 a1)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1> combined);
            (a0, a1) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 3
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2> combined);
            (a0, a1, a2) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 4
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2, A3>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2, A3> combined);
            (a0, a1, a2, a3) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 5
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2, A3, A4>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2, A3, A4> combined);
            (a0, a1, a2, a3, a4) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 6
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2, A3, A4, A5>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where A5 : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4, A5>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where A5 : EcsAspect, new()
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4, A5>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect, new()
            where A1 : EcsAspect, new()
            where A2 : EcsAspect, new()
            where A3 : EcsAspect, new()
            where A4 : EcsAspect, new()
            where A5 : EcsAspect, new()
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2, A3, A4, A5> combined);
            (a0, a1, a2, a3, a4, a5) = combined;
            return result;
        }
        #endregion
    }
}

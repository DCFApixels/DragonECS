namespace DCFApixels.DragonECS
{
    public sealed class EmptyAspect : EcsAspect { }

    public sealed class SingleAspect<TPool> : EcsAspect where TPool : IEcsPoolImplementation, new()
    {
        public readonly TPool pool;
        public SingleAspect(Builder b)
        {
            pool = b.IncludePool<TPool>();
        }
    }
    public sealed class CombinedAspect<A0, A1> : EcsAspect
        where A0 : EcsAspect
        where A1 : EcsAspect
    {
        public readonly A0 a0;
        public readonly A1 a1;
        public CombinedAspect(Builder b)
        {
            a0 = b.Combine<A0>(0);
            a1 = b.Combine<A1>(1);
        }
        public void Deconstruct(out A0 a0, out A1 a1)
        {
            a0 = this.a0;
            a1 = this.a1;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2> : EcsAspect
        where A0 : EcsAspect
        where A1 : EcsAspect
        where A2 : EcsAspect
    {
        public readonly A0 a0;
        public readonly A1 a1;
        public readonly A2 a2;
        public CombinedAspect(Builder b)
        {
            a0 = b.Combine<A0>(0);
            a1 = b.Combine<A1>(1);
            a2 = b.Combine<A2>(2);
        }
        public void Deconstruct(out A0 a0, out A1 a1, out A2 a2)
        {
            a0 = this.a0;
            a1 = this.a1;
            a2 = this.a2;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2, A3> : EcsAspect
        where A0 : EcsAspect
        where A1 : EcsAspect
        where A2 : EcsAspect
        where A3 : EcsAspect
    {
        public readonly A0 a0;
        public readonly A1 a1;
        public readonly A2 a2;
        public readonly A3 a3;
        public CombinedAspect(Builder b)
        {
            a0 = b.Combine<A0>(0);
            a1 = b.Combine<A1>(1);
            a2 = b.Combine<A2>(2);
            a3 = b.Combine<A3>(3);
        }
        public void Deconstruct(out A0 a0, out A1 a1, out A2 a2, out A3 a3)
        {
            a0 = this.a0;
            a1 = this.a1;
            a2 = this.a2;
            a3 = this.a3;
        }
    }

    public sealed class CombinedAspect<A0, A1, A2, A3, A4> : EcsAspect
        where A0 : EcsAspect
        where A1 : EcsAspect
        where A2 : EcsAspect
        where A3 : EcsAspect
        where A4 : EcsAspect
    {
        public readonly A0 a0;
        public readonly A1 a1;
        public readonly A2 a2;
        public readonly A3 a3;
        public readonly A4 a4;
        public CombinedAspect(Builder b)
        {
            a0 = b.Combine<A0>(0);
            a1 = b.Combine<A1>(1);
            a2 = b.Combine<A2>(2);
            a3 = b.Combine<A3>(3);
            a4 = b.Combine<A4>(4);
        }
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
        where A0 : EcsAspect
        where A1 : EcsAspect
        where A2 : EcsAspect
        where A3 : EcsAspect
        where A4 : EcsAspect
        where A5 : EcsAspect
    {
        public readonly A0 a0;
        public readonly A1 a1;
        public readonly A2 a2;
        public readonly A3 a3;
        public readonly A4 a4;
        public readonly A5 a5;
        public CombinedAspect(Builder b)
        {
            a0 = b.Combine<A0>(0);
            a1 = b.Combine<A1>(1);
            a2 = b.Combine<A2>(2);
            a3 = b.Combine<A3>(3);
            a4 = b.Combine<A4>(4);
            a5 = b.Combine<A5>(5);
        }
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
            where A0 : EcsAspect
            where A1 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1);
        }
        public static EcsSpan Where<A0, A1>(this EcsReadonlyGroup group, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            return group.ToSpan().Where(out a0, out a1);
        }
        public static EcsSpan Where<A0, A1>(this EcsSpan span, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            var result = span.Where(out CombinedAspect<A0, A1> combined);
            (a0, a1) = combined;
            return result;
        }
        #endregion

        #region Where 3
        public static EcsSpan Where<TCollection, A0, A1, A2>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2);
        }
        public static EcsSpan Where<A0, A1, A2>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            return group.ToSpan().Where(out a0, out a1, out a2);
        }
        public static EcsSpan Where<A0, A1, A2>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2> combined);
            (a0, a1, a2) = combined;
            return result;
        }
        #endregion

        #region Where 4
        public static EcsSpan Where<TCollection, A0, A1, A2, A3>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2, out a3);
        }
        public static EcsSpan Where<A0, A1, A2, A3>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            return group.ToSpan().Where(out a0, out a1, out a2, out a3);
        }
        public static EcsSpan Where<A0, A1, A2, A3>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2, A3> combined);
            (a0, a1, a2, a3) = combined;
            return result;
        }
        #endregion

        #region Where 5
        public static EcsSpan Where<TCollection, A0, A1, A2, A3, A4>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            return group.ToSpan().Where(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2, A3, A4> combined);
            (a0, a1, a2, a3, a4) = combined;
            return result;
        }
        #endregion

        #region Where 6
        public static EcsSpan Where<TCollection, A0, A1, A2, A3, A4, A5>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().Where(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4, A5>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            return group.ToSpan().Where(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsSpan Where<A0, A1, A2, A3, A4, A5>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            var result = span.Where(out CombinedAspect<A0, A1, A2, A3, A4, A5> combined);
            (a0, a1, a2, a3, a4, a5) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 2
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1>(this TCollection entities, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1>(this EcsReadonlyGroup group, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            return group.ToSpan().WhereToGroup(out a0, out a1);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1>(this EcsSpan span, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1> combined);
            (a0, a1) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 3
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2> combined);
            (a0, a1, a2) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 4
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2, A3>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2, A3> combined);
            (a0, a1, a2, a3) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 5
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2, A3, A4>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2, A3, A4> combined);
            (a0, a1, a2, a3, a4) = combined;
            return result;
        }
        #endregion

        #region WhereToGroup 6
        public static EcsReadonlyGroup WhereToGroup<TCollection, A0, A1, A2, A3, A4, A5>(this TCollection entities, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4, A5>(this EcsReadonlyGroup group, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            return group.ToSpan().WhereToGroup(out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsReadonlyGroup WhereToGroup<A0, A1, A2, A3, A4, A5>(this EcsSpan span, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            var result = span.WhereToGroup(out CombinedAspect<A0, A1, A2, A3, A4, A5> combined);
            (a0, a1, a2, a3, a4, a5) = combined;
            return result;
        }
        #endregion
    }
}

namespace DCFApixels.DragonECS
{
    public sealed class EmptyAspect : EcsAspect { }

    public sealed class SingleAspect<TPool> : EcsAspect where TPool : IEcsPoolImplementation, new()
    {
        public readonly TPool pool;
        public SingleAspect(Builder b)
        {
            pool = b.Include<TPool>();
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
    }

    public static class CombinedAspectExtensions
    {
        #region Where 2
        public static EcsReadonlyGroup Where<A0, A1>(this EcsWorld self, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            return self.WhereFor(self.Entities, out a0, out a1);
        }
        public static EcsReadonlyGroup WhereFor<A0, A1>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out A0 a0, out A1 a1)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            var combined = self.GetAspect<CombinedAspect<A0, A1>>();
            a0 = combined.a0;
            a1 = combined.a1;
            return self.WhereToGroupFor<CombinedAspect<A0, A1>>(sourceGroup);
        }

        public static EcsReadonlyGroup Where<A0, A1>(this EcsWorld self)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            return self.WhereToGroup<CombinedAspect<A0, A1>>();
        }
        public static EcsReadonlyGroup WhereFor<A0, A1>(this EcsWorld self, EcsReadonlyGroup sourceGroup)
            where A0 : EcsAspect
            where A1 : EcsAspect
        {
            return self.WhereToGroupFor<CombinedAspect<A0, A1>>(sourceGroup);
        }
        #endregion

        #region Where 3
        public static EcsReadonlyGroup Where<A0, A1, A2>(this EcsWorld self, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            return self.WhereFor(self.Entities, out a0, out a1, out a2);
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out A0 a0, out A1 a1, out A2 a2)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            var combined = self.GetAspect<CombinedAspect<A0, A1, A2>>();
            a0 = combined.a0;
            a1 = combined.a1;
            a2 = combined.a2;
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2>>(sourceGroup);
        }

        public static EcsReadonlyGroup Where<A0, A1, A2>(this EcsWorld self)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            return self.WhereToGroup<CombinedAspect<A0, A1, A2>>();
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2>(this EcsWorld self, EcsReadonlyGroup sourceGroup)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
        {
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2>>(sourceGroup);
        }
        #endregion

        #region Where 4
        public static EcsReadonlyGroup Where<A0, A1, A2, A3>(this EcsWorld self, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            return self.WhereFor(self.Entities, out a0, out a1, out a2, out a3);
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2, A3>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out A0 a0, out A1 a1, out A2 a2, out A3 a3)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            var combined = self.GetAspect<CombinedAspect<A0, A1, A2, A3>>();
            a0 = combined.a0;
            a1 = combined.a1;
            a2 = combined.a2;
            a3 = combined.a3;
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2, A3>>(sourceGroup);
        }

        public static EcsReadonlyGroup Where<A0, A1, A2, A3>(this EcsWorld self)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            return self.WhereToGroup<CombinedAspect<A0, A1, A2, A3>>();
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2, A3>(this EcsWorld self, EcsReadonlyGroup sourceGroup)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
        {
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2, A3>>(sourceGroup);
        }
        #endregion

        #region Where 5
        public static EcsReadonlyGroup Where<A0, A1, A2, A3, A4>(this EcsWorld self, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            return self.WhereFor(self.Entities, out a0, out a1, out a2, out a3, out a4);
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2, A3, A4>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            var combined = self.GetAspect<CombinedAspect<A0, A1, A2, A3, A4>>();
            a0 = combined.a0;
            a1 = combined.a1;
            a2 = combined.a2;
            a3 = combined.a3;
            a4 = combined.a4;
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2, A3, A4>>(sourceGroup);
        }


        public static EcsReadonlyGroup Where<A0, A1, A2, A3, A4>(this EcsWorld self)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            return self.WhereToGroup<CombinedAspect<A0, A1, A2, A3, A4>>();
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2, A3, A4>(this EcsWorld self, EcsReadonlyGroup sourceGroup)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
        {
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2, A3, A4>>(sourceGroup);
        }
        #endregion

        #region Where 6
        public static EcsReadonlyGroup Where<A0, A1, A2, A3, A4, A5>(this EcsWorld self, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            return self.WhereFor(self.Entities, out a0, out a1, out a2, out a3, out a4, out a5);
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2, A3, A4, A5>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out A0 a0, out A1 a1, out A2 a2, out A3 a3, out A4 a4, out A5 a5)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            var combined = self.GetAspect<CombinedAspect<A0, A1, A2, A3, A4, A5>>();
            a0 = combined.a0;
            a1 = combined.a1;
            a2 = combined.a2;
            a3 = combined.a3;
            a4 = combined.a4;
            a5 = combined.a5;
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2, A3, A4, A5>>(sourceGroup);
        }


        public static EcsReadonlyGroup Where<A0, A1, A2, A3, A4, A5>(this EcsWorld self)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            return self.WhereToGroup<CombinedAspect<A0, A1, A2, A3, A4, A5>>();
        }
        public static EcsReadonlyGroup WhereFor<A0, A1, A2, A3, A4, A5>(this EcsWorld self, EcsReadonlyGroup sourceGroup)
            where A0 : EcsAspect
            where A1 : EcsAspect
            where A2 : EcsAspect
            where A3 : EcsAspect
            where A4 : EcsAspect
            where A5 : EcsAspect
        {
            return self.WhereToGroupFor<CombinedAspect<A0, A1, A2, A3, A4, A5>>(sourceGroup);
        }
        #endregion
    }
}

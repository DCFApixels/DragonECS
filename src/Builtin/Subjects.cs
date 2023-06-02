namespace DCFApixels.DragonECS
{
    public class SingleSubject<TComponent, TPool> : EcsSubject where TPool : IEcsPoolImplementation<TComponent>, new()
    {
        public readonly TPool pool;
        public SingleSubject(Builder b)
        {
            pool = b.Include<TComponent, TPool>();
        }
    }
    public class CombinedSubject<S0, S1> : EcsSubject
        where S0 : EcsSubject
        where S1 : EcsSubject
    {
        public readonly S0 s0;
        public readonly S1 s1;
        public CombinedSubject(Builder b)
        {
            s0 = b.Combine<S0>();
            s1 = b.Combine<S1>();
        }
    }

    public class CombinedSubject<S0, S1, S2> : EcsSubject
        where S0 : EcsSubject
        where S1 : EcsSubject
        where S2 : EcsSubject
    {
        public readonly S0 s0;
        public readonly S1 s1;
        public readonly S2 s2;
        public CombinedSubject(Builder b)
        {
            s0 = b.Combine<S0>();
            s1 = b.Combine<S1>();
            s2 = b.Combine<S2>();
        }
    }

    public class CombinedSubject<S0, S1, S2, S3> : EcsSubject
        where S0 : EcsSubject
        where S1 : EcsSubject
        where S2 : EcsSubject
        where S3 : EcsSubject
    {
        public readonly S0 s0;
        public readonly S1 s1;
        public readonly S2 s2;
        public readonly S3 s3;
        public CombinedSubject(Builder b)
        {
            s0 = b.Combine<S0>();
            s1 = b.Combine<S1>();
            s2 = b.Combine<S2>();
            s3 = b.Combine<S3>();
        }
    }

    public class CombinedSubject<S0, S1, S2, S3, S4> : EcsSubject
        where S0 : EcsSubject
        where S1 : EcsSubject
        where S2 : EcsSubject
        where S3 : EcsSubject
        where S4 : EcsSubject
    {
        public readonly S0 s0;
        public readonly S1 s1;
        public readonly S2 s2;
        public readonly S3 s3;
        public readonly S4 s4;
        public CombinedSubject(Builder b)
        {
            s0 = b.Combine<S0>();
            s1 = b.Combine<S1>();
            s2 = b.Combine<S2>();
            s3 = b.Combine<S3>();
            s4 = b.Combine<S4>();
        }
    }

    public class CombinedSubject<S0, S1, S2, S3, S4, S5> : EcsSubject
        where S0 : EcsSubject
        where S1 : EcsSubject
        where S2 : EcsSubject
        where S3 : EcsSubject
        where S4 : EcsSubject
        where S5 : EcsSubject
    {
        public readonly S0 s0;
        public readonly S1 s1;
        public readonly S2 s2;
        public readonly S3 s3;
        public readonly S4 s4;
        public readonly S5 s5;
        public CombinedSubject(Builder b)
        {
            s0 = b.Combine<S0>();
            s1 = b.Combine<S1>();
            s2 = b.Combine<S2>();
            s3 = b.Combine<S3>();
            s4 = b.Combine<S4>();
            s5 = b.Combine<S5>();
        }
    }

    public static class CombinedSubjectExtensions
    {
        #region 2
        public static EcsReadonlyGroup Where<S0, S1>(this EcsWorld self, out S0 s0, out S1 s1)
            where S0 : EcsSubject
            where S1 : EcsSubject
        {
            return self.WhereFor(self.Entities, out s0, out s1);
        }
        public static EcsReadonlyGroup WhereFor<S0, S1>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out S0 s0, out S1 s1)
            where S0 : EcsSubject
            where S1 : EcsSubject
        {
            var combined = self.GetSubject<CombinedSubject<S0, S1>>();
            s0 = combined.s0;
            s1 = combined.s1;
            return self.WhereFor<CombinedSubject<S0, S1>>(sourceGroup);
        }
        #endregion

        #region 3
        public static EcsReadonlyGroup Where<S0, S1, S2>(this EcsWorld self, out S0 s0, out S1 s1, out S2 s2)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
        {
            return self.WhereFor(self.Entities, out s0, out s1, out s2);
        }
        public static EcsReadonlyGroup WhereFor<S0, S1, S2>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out S0 s0, out S1 s1, out S2 s2)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
        {
            var combined = self.GetSubject<CombinedSubject<S0, S1, S2>>();
            s0 = combined.s0;
            s1 = combined.s1;
            s2 = combined.s2;
            return self.WhereFor<CombinedSubject<S0, S1, S2>>(sourceGroup);
        }
        #endregion

        #region 4
        public static EcsReadonlyGroup Where<S0, S1, S2, S3>(this EcsWorld self, out S0 s0, out S1 s1, out S2 s2, out S3 s3)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
            where S3 : EcsSubject
        {
            return self.WhereFor(self.Entities, out s0, out s1, out s2, out s3);
        }
        public static EcsReadonlyGroup WhereFor<S0, S1, S2, S3>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out S0 s0, out S1 s1, out S2 s2, out S3 s3)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
            where S3 : EcsSubject
        {
            var combined = self.GetSubject<CombinedSubject<S0, S1, S2, S3>>();
            s0 = combined.s0;
            s1 = combined.s1;
            s2 = combined.s2;
            s3 = combined.s3;
            return self.WhereFor<CombinedSubject<S0, S1, S2, S3>>(sourceGroup);
        }
        #endregion

        #region 5
        public static EcsReadonlyGroup Where<S0, S1, S2, S3, S4>(this EcsWorld self, out S0 s0, out S1 s1, out S2 s2, out S3 s3, out S4 s4)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
            where S3 : EcsSubject
            where S4 : EcsSubject
        {
            return self.WhereFor(self.Entities, out s0, out s1, out s2, out s3, out s4);
        }
        public static EcsReadonlyGroup WhereFor<S0, S1, S2, S3, S4>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out S0 s0, out S1 s1, out S2 s2, out S3 s3, out S4 s4)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
            where S3 : EcsSubject
            where S4 : EcsSubject
        {
            var combined = self.GetSubject<CombinedSubject<S0, S1, S2, S3, S4>>();
            s0 = combined.s0;
            s1 = combined.s1;
            s2 = combined.s2;
            s3 = combined.s3;
            s4 = combined.s4;
            return self.WhereFor<CombinedSubject<S0, S1, S2, S3, S4>>(sourceGroup);
        }
        #endregion

        #region 6
        public static EcsReadonlyGroup Where<S0, S1, S2, S3, S4, S5>(this EcsWorld self, out S0 s0, out S1 s1, out S2 s2, out S3 s3, out S4 s4, out S5 s5)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
            where S3 : EcsSubject
            where S4 : EcsSubject
            where S5 : EcsSubject
        {
            return self.WhereFor(self.Entities, out s0, out s1, out s2, out s3, out s4, out s5);
        }
        public static EcsReadonlyGroup WhereFor<S0, S1, S2, S3, S4, S5>(this EcsWorld self, EcsReadonlyGroup sourceGroup, out S0 s0, out S1 s1, out S2 s2, out S3 s3, out S4 s4, out S5 s5)
            where S0 : EcsSubject
            where S1 : EcsSubject
            where S2 : EcsSubject
            where S3 : EcsSubject
            where S4 : EcsSubject
            where S5 : EcsSubject
        {
            var combined = self.GetSubject<CombinedSubject<S0, S1, S2, S3, S4, S5>>();
            s0 = combined.s0;
            s1 = combined.s1;
            s2 = combined.s2;
            s3 = combined.s3;
            s4 = combined.s4;
            s5 = combined.s5;
            return self.WhereFor<CombinedSubject<S0, S1, S2, S3, S4, S5>>(sourceGroup);
        }
        #endregion
    }
}

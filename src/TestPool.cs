using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS.Test
{
    public class TestWorld
    {
        public PoolToken RegisterPool<TComponent>()
        {
            return new PoolToken(1);
        }
    }

    public readonly struct PoolToken
    {
        internal readonly ushort id;
        public PoolToken(ushort id)
        {
            this.id = id;
        }
    }
    //реализовать query так чтоб на вход он получал какуюто коллекцию и заполнял ее. по итогу на выходе запроса юзер будет иметь просто список
    //таким образом во первых он сам может решить как его прокрутить, через for или foreach или еще как. во вторых можно будет прикрутить поддержку nativearray от unity

    public class TestPool<TComponent>
    {
        private PoolToken _token;
        public TestPool(TestWorld world)
        {
            _token = world.RegisterPool<TComponent>();
        }
    }
}

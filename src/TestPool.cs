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

        public IEcsPool GetPoolX()
        {
            return null;
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

    public interface IPool { }
    public class Pool1<TComponent> : IEcsPool<TComponent>
        where TComponent : struct
    {
        public Type ComponentType => throw new NotImplementedException();

        public EcsWorld World => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public int Capacity => throw new NotImplementedException();

        public ref TComponent Add(int entityID)
        {
            throw new NotImplementedException();
        }

        public void Del(int entityID)
        {
            throw new NotImplementedException();
        }

        public bool Has(int entityID)
        {
            throw new NotImplementedException();
        }

        public ref TComponent Read(int entityID)
        {
            throw new NotImplementedException();
        }

        public ref TComponent Write(int entityID)
        {
            throw new NotImplementedException();
        }

        void IEcsPool.Add(int entityID)
        {
            throw new NotImplementedException();
        }

        void IEcsPool.OnWorldResize(int newSize)
        {
            throw new NotImplementedException();
        }

        void IEcsPool.Write(int entityID, object data)
        {
            throw new NotImplementedException();
        }
    }

    public interface IComponentPool<TPool, TComponent>
        where TPool : IEcsPool<TComponent>
        where TComponent : struct
    { }

    public interface IComponent1<TComponent> : IComponentPool<Pool1<TComponent>, TComponent>
        where TComponent : struct
    { }
    public interface IComponent2<TComponent> : IComponentPool<EcsPool<TComponent>, TComponent>
        where TComponent : struct
    { }

    public struct ComponentX1 : IComponent1<ComponentX1> { }
    public struct ComponentX2 : IComponent2<ComponentX2> { }

    public static class Pool1Ext
    {
        public static Pool1<TComponent> GetPool<TComponent>(this TestWorld self)
            where TComponent : struct, IComponent1<TComponent>
        {
            return (Pool1<TComponent>)self.GetPoolX();
        }
    }

    public static class Pool2Ext
    {
        public static EcsPool<TComponent> GetPool<TComponent>(this TestWorld self)
            where TComponent : struct, IComponent2<TComponent>
        {
            return (EcsPool<TComponent>)self.GetPoolX();
        }
    }

    public class Foo
    {
        private TestWorld world;
        public void Do()    
        {
            var poola = world.GetPool<ComponentX1>();
        }
    }


}


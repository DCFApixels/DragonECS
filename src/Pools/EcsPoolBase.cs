using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsPoolBase
    {
        private int _id = -1;
        private EcsWorld _world;
        internal void PreInitInternal(EcsWorld world, int id)
        {
            _id = id;
            _world = world;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void IncrementEntityComponentCount(int entityID)
        {
            _world.IncrementEntityComponentCount(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DecrementEntityComponentCount(int entityID)
        {
            _world.DecrementEntityComponentCount(entityID);
        }

        #region Properties
        public abstract Type ComponentType { get; }
        public EcsWorld World => _world;
        #endregion

        #region Methods
        public abstract bool Has(int entityID);

        protected abstract void Init(EcsWorld world);
        protected abstract void OnWorldResize(int newSize);
        protected abstract void OnDestroy();
        #endregion

        #region Internal
        internal void InvokeInit(EcsWorld world) => Init(world);
        internal void InvokeOnWorldResize(int newSize) => OnWorldResize(newSize);
        internal void InvokeOnDestroy() => OnDestroy();
        #endregion
    }
    public abstract class EcsPoolBase<T> : EcsPoolBase, IEnumerable<T>
    {
        public sealed override Type ComponentType => typeof(T);
        //Релазиация интерфейса IEnumerator не работает, нужно только чтобы IntelliSense предлагала названия на основе T. Не нашел другого способа
        #region IEnumerable 
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        #endregion
    }

    public struct NullComponent { }
    public sealed class EcsNullPool : EcsPoolBase
    {
        public static EcsNullPool instance => new EcsNullPool();

        #region Properties
        public sealed override Type ComponentType => typeof(NullComponent);
        #endregion

        #region Methods
        public sealed override bool Has(int index) => false;
        #endregion

        #region Callbacks
        protected override void Init(EcsWorld world) { }
        protected override void OnWorldResize(int newSize) { }
        protected override void OnDestroy() { }
        #endregion
    }
}

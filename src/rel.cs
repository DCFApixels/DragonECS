using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.TODO
{
    //TODO реализовать систему отношений.
    //идея есть миры-коннекторы, они являются наборами сущьностей-отношений
    //сущьности отношения, это теже сущьности но их полный идентификатор - это 32 бита - одна сущьность и 32 - другая
    //айди миров левой и правой части записываются в мир-коннектор и для конвертации айди сущьности в полный идентификатор, надо взять данные из него.
    //Проблема: если хранить айди мира для левой части отношения в одном месте, а для правого в другом и только раграничивать на лево и право будет проблема с тем что связи работают только в одном направлении
    //в этом мире вообще не будет вестиь учет гена, потому что для отношений он не нужен

    //миры коннекторы можно назвать Relations или RealtionTable



    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly ref partial struct rel
    {
        public readonly int id;
        public readonly short leftWorld;
        public readonly short rightWorld;

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public rel(int id, short leftWorld, short rightWorld)
        {
            this.id = id;
            this.leftWorld = leftWorld;
            this.rightWorld = rightWorld;
        }
        #endregion
    }
}

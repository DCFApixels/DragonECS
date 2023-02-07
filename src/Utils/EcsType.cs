using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{

    public class EcsType : IEquatable<EcsType>
    {
        private static int _idIncrement = 0;

        //свободно еще 4 байта, возможно можно выделить под айдишники
        private string _name;
        private int _uniqueID;

        #region Constructors
        internal EcsType(string name)
        {
            _name = name;
            _uniqueID = _idIncrement++;

#if DEBUG
            if (_idIncrement == 0)
            {
                throw new EcsFrameworkException($"The maximum number of identifiers is allocated. Max:{uint.MaxValue}");
            }
#endif
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is EcsType key && _name == key._name;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EcsType other)
        {
            return _uniqueID == other._uniqueID;
        }
        #endregion

        #region GetHashCode/ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _uniqueID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => "EcsType." + _name;

        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in EcsType left, in EcsType right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in EcsType left, in EcsType right) => !left.Equals(right);
        #endregion
    }


    public static class EcsTypeMap
    {
        private static Dictionary<string, EcsType> _types = new Dictionary<string, EcsType>(256);

        public static EcsType GetEcsType(string name)
        {
            if (_types.TryGetValue(name, out EcsType type))
                return type;

            type = new EcsType(name);
            _types.Add(name, type);
            return type;
        }
    }
}
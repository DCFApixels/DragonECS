using System;
using System.Runtime.CompilerServices;
using DCFApixels.DragonECS.Reflection;

namespace DCFApixels.DragonECS
{
    public readonly struct mem<T> : IEquatable<mem<T>>, IEquatable<int>
    {
        public static readonly mem<T> NULL = new mem<T>(-1);

        internal readonly int offsetedUniqueID;

        #region Properties
        public int UniqueID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => offsetedUniqueID - 1;
        }
        public bool HasValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => offsetedUniqueID != 0;
        }
        #endregion

        #region Constructors
        private mem(int uniqueID)
        {
            offsetedUniqueID = uniqueID + 1;
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is mem<T> key && offsetedUniqueID == key.offsetedUniqueID;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(mem<T> other) => offsetedUniqueID == other.offsetedUniqueID;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int other) => offsetedUniqueID == (other + 1);
        #endregion

        #region GetHashCode/ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => offsetedUniqueID - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => HasValue ? MemberDeclarator.GetMemberInfo(this).ToString() : "NULL";

        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in mem<T> left, in mem<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in mem<T> left, in mem<T> right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator mem<T>(string name) => new mem<T>(MemberDeclarator.GetOrDeclareMember<T>(name).UniqueID);
        #endregion
    }
}

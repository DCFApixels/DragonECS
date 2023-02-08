using System;
using System.Runtime.CompilerServices;
using DCFApixels.DragonECS.Reflection;

namespace DCFApixels.DragonECS
{
    public readonly struct mem<T> : IEquatable<mem<T>>, IEquatable<int>
            where T : struct
    {
        public static readonly mem<T> NULL = default;

        internal readonly int uniqueID;

        #region Properties
        public bool HasValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => uniqueID != 0;
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private mem(int uniqueID) => this.uniqueID = uniqueID;
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is mem<T> key && uniqueID == key.uniqueID;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(mem<T> other) => uniqueID == other.uniqueID;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int other) => uniqueID == other;
        #endregion

        #region GetHashCode/ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => uniqueID;

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

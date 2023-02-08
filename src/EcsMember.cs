using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Reflection
{
    public static class MemberDeclarator
    {
        private static Dictionary<string, EcsMemberBase> _nameMembersPairs = new Dictionary<string, EcsMemberBase>(1024);
        private static EcsMemberBase[] _members = new EcsMemberBase[1024];
        private static int _increment = 1; // 0 индекс всегда пустой, так как он используется в mem<T> для обозначения NULL mem<T>

        public static int MembersCount => _increment - 1;
        public static EcsMember<T> Declare<T>(string name)
            where T : struct
        {
            name = $"{typeof(T).FullName}__{name}";
#if DEBUG && !DCFA_ECS_NO_SANITIZE_CHECKS
            if (_increment < 0)
            {
                throw new EcsFrameworkException($"Maximum available members exceeded. The member of \"{name}\" was not declared");
            }
            if (_nameMembersPairs.ContainsKey(name))
            {
                throw new EcsFrameworkException($"The node with the name \"{name}\" has already been declared");
            }
#endif
            if (_increment >= _members.Length)
            {
                Array.Resize(ref _members, _members.Length << 1);
            }

            EcsMember<T> member = new EcsMember<T>(name, _increment);
            _nameMembersPairs.Add(name, member);
            _members[_increment++] = member;

            return member;
        }

        public static EcsMember<T> GetOrDeclareMember<T>(string name)
            where T : struct
        {
            if (_nameMembersPairs.TryGetValue(name, out EcsMemberBase memberBase))
            {
                return (EcsMember<T>)memberBase;
            }

            return Declare<T>(name);
        }

        public static EcsMember<T> GetMemberInfo<T>(mem<T> member)
            where T : struct
        {
#if DEBUG && !DCFA_ECS_NO_SANITIZE_CHECKS
            if (member.HasValue == false)
            {
                throw new ArgumentException($"The mem<{typeof(T).Name}> argument is empty");
            }
#endif

            return (EcsMember<T>)_members[member.uniqueID];
        }
    }

    public abstract class EcsMemberBase : IEquatable<EcsMemberBase>
    {
        protected const string TO_STRING_HEADER = "EcsMember:";

        protected string _name;
        protected int _uniqueID;
        protected Type _type;

        #region Propertiees
        public int UniqueID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _uniqueID;
        }
        #endregion

        #region GetHashCode/ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _uniqueID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => TO_STRING_HEADER + _name;

        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is EcsMemberBase key && _name == key._name;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EcsMemberBase other)
        {
            return _uniqueID == other._uniqueID;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in EcsMemberBase left, in EcsMemberBase right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in EcsMemberBase left, in EcsMemberBase right) => !left.Equals(right);
        #endregion
    }
    public class EcsMember<T> : EcsMemberBase, IEquatable<EcsMember<T>>
        where T : struct
    {
        #region Constructors
        private EcsMember() { }
        internal EcsMember(string name, int uniqueID)
        {
            _name = name;
            _uniqueID = uniqueID;
            _type = typeof(T);
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is EcsMember<T> key && _uniqueID == key._uniqueID;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EcsMember<T> other) => _uniqueID == other._uniqueID;
        #endregion

        #region GetHashCode/ToString
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _uniqueID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => TO_STRING_HEADER + _name;

        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in EcsMember<T> left, in EcsMember<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in EcsMember<T> left, in EcsMember<T> right) => !left.Equals(right);


        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator EcsMember<T>(string name) => MemberDeclarator.Declare<T>(name);
        #endregion
    }
}
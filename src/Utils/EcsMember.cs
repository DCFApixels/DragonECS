﻿using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using DCFApixels.DragonECS.Reflection;

namespace DCFApixels.DragonECS
{
    namespace Reflection
    {
        public static class MemberDeclarator
        {
#if !DCFA_ECS_NO_SANITIZE_CHECKS
            private static HashSet<string> _usedNames = new HashSet<string>(1024);
#endif
            private static EcsMemberBase[] _member = new EcsMemberBase[1024];
            private static int _typesCount = 0;
            public static EcsMember<T> Declare<T>(string name)
            {
                name = $"{typeof(T).FullName}__{name}";
#if DEBUG && !DCFA_ECS_NO_SANITIZE_CHECKS
                if (_typesCount < 0)
                {
                    throw new EcsFrameworkException($"Maximum available members exceeded. The member of \"{name}\" was not declared");
                }
                if (_usedNames.Contains(name))
                {
                    throw new EcsFrameworkException($"The node with the name \"{name}\" has already been declared");
                }
                _usedNames.Add(name);
#endif
                if (_typesCount >= _member.Length)
                {
                    Array.Resize(ref _member, _member.Length << 1);
                }

                EcsMember<T> member = new EcsMember<T>(name, _typesCount);
                _member[_typesCount++] = member;

                return member;
            }

            public static EcsMember<T> GetMemberInfo<T>(mem<T> member)
            {
#if DEBUG && !DCFA_ECS_NO_SANITIZE_CHECKS
                if (member.HasValue == false)
                {
                    throw new ArgumentException($"The member argument is empty");
                }
#endif

                return (EcsMember<T>)_member[member.UniqueID];
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

}
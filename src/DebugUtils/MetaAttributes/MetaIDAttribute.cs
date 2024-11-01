using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaIDAttribute : EcsMetaAttribute
    {
        public readonly string ID;
        public MetaIDAttribute(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Throw.ArgumentNull(nameof(id));
            }
            if (MetaID.IsGenericID(id) == false)
            {
                Throw.ArgumentException($"Identifier {id} contains invalid characters: ,<>");
            }
            id = string.Intern(id);
            ID = id;
        }
    }

    public static class MetaID
    {
        [ThreadStatic]
        private static Random _randon;
        [ThreadStatic]
        private static byte[] _buffer;
        [ThreadStatic]
        private static bool _isInit;

        public static bool IsGenericID(string id)
        {
            return Regex.IsMatch(id, @"^[^,<>\s]*$");
        }

        public static unsafe string GenerateNewUniqueID()
        {
            if (_isInit == false)
            {
                IntPtr prt = Marshal.AllocHGlobal(1);
                long alloc = (long)prt;
                Marshal.Release(prt);
                _randon = new Random((int)alloc);
                _buffer = new byte[8];
                _isInit = true;
            }

            byte* hibits = stackalloc byte[8];
            long* hibitsL = (long*)hibits;
            hibitsL[0] = DateTime.Now.Ticks;
            hibitsL[1] = _randon.Next();

            for (int i = 0; i < 8; i++)
            {
                _buffer[i] = hibits[i];
            }

            return BitConverter.ToString(_buffer).Replace("-", "");
        }
        public static string IDToAttribute(string id)
        {
            return $"[MetaID(\"id\")]";
        }
        public static string GenerateNewUniqueIDWithAttribute()
        {
            return IDToAttribute(GenerateNewUniqueID());
        }
    }
}
using DCFApixels.DragonECS.Internal;
using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaIDAttribute : EcsMetaAttribute
    {
        //private static HashSet<string> _ids = new HashSet<string>();

        public readonly string ID;
        public MetaIDAttribute(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Throw.ArgumentNull(nameof(id));
            }
            if (id.Contains(','))
            {
                Throw.ArgumentException($"Аргумент {nameof(id)} не может содержать символ запятой ','");
            }
            //if (_ids.Add(id) == false) //этот ексепшен не работает, так как атрибуты не кешируются а пересоздаются
            //{
            //    //TODO перевести ексепшен
            //    Throw.ArgumentException($"Дублирование MetaID: {id}");
            //}
            ID = id;
        }
    }

    public static class MetaIDUtility
    {
        public static unsafe string GenerateNewUniqueID()
        {
            long ticks = DateTime.Now.Ticks;
            byte* hibits = stackalloc byte[8];
            hibits = (byte*)ticks;

            byte[] byteArray = Guid.NewGuid().ToByteArray();

            fixed (byte* ptr = byteArray)
            {
                for (int i = 0; i < 8; i++)
                {
                    byteArray[i] = hibits[i];
                }
            }
            return BitConverter.ToString(byteArray).Replace("-", "");
        }
        public static unsafe string GenerateNewUniqueIDWithAttribute()
        {
            return $"[MetaID(\"{GenerateNewUniqueID()}\")]";
        }
    }
}

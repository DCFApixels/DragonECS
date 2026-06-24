#if !UNITY_2020_3_OR_NEWER
using System;

namespace UnityEngine.Scripting
{
    /// <summary>
    /// Dummy stub for Unity's PreserveAttribute to compile outside Unity environment (no actual preservation effect).
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct, Inherited = false)]
    internal class PreserveAttribute : Attribute { }
}
#endif
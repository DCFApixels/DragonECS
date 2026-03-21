#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal class InjectionBranch
    {
        private readonly Type _type;
        private StructList<InjectionNodeBase> _nodes = new StructList<InjectionNodeBase>(4);
        public Type Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _type; }
        }
        public ReadOnlySpan<InjectionNodeBase> Nodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _nodes.AsReadOnlySpan(); }
        }
        public InjectionBranch(Type type)
        {
            _type = type;
        }
        public void AddNode(InjectionNodeBase node)
        {
            _nodes.Add(node);
        }
    }
}

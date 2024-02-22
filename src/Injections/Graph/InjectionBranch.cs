using System;

namespace DCFApixels.DragonECS
{
    public class InjectionBranch
    {
        private Injector _source;
        private Type _type;
        private InjectionNodeBase[] _nodes = new InjectionNodeBase[2];
        private int _nodesCount = 0;
        private bool _isDeclared = false;

        public Type Type
        {
            get { return _type; }
        }
        public bool IsDeclared
        {
            get { return _isDeclared; }
        }
        public InjectionBranch(Injector source, Type type, bool isDeclared)
        {
            _source = source;
            _isDeclared = isDeclared;
            _type = type;
        }
        public void SetDeclaredTrue()
        {
            _isDeclared = true;
        }
        public void Inject(object obj)
        {
            for (int i = 0; i < _nodesCount; i++)
            {
                _nodes[i].Inject(obj);
            }
            if (obj is IInjectionBlock container)
            {
                container.InjectTo(new BlockInjector(_source));
            }
        }
        public void AddNode(InjectionNodeBase node)
        {
            if (_nodesCount >= _nodes.Length)
            {
                Array.Resize(ref _nodes, (_nodes.Length << 1) + 1);
            }
            _nodes[_nodesCount++] = node;
        }
        public void Trim()
        {
            if (_nodesCount <= 0)
            {
                _nodes = Array.Empty<InjectionNodeBase>();
                return;
            }

            InjectionNodeBase[] newNodes = new InjectionNodeBase[_nodesCount];
            for (int i = 0; i < newNodes.Length; i++)
            {
                newNodes[i] = _nodes[i];
            }
        }
    }
}

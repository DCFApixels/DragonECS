using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsTableCore
    {
        internal EcsWorld _source;
        internal ent entity;

        private bool _enabled = false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ent Entity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entity;
        }

        internal void Enable()
        {
            _enabled = true;
        }

        internal void Disable()
        {
            _enabled = false;
            entity = ent.NULL;
        }

        internal void MoveToNextEntity()
        {

        }


        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }


        public ref struct Enumerator
        {
            private readonly EcsTableCore _source;

            public Enumerator(EcsTableCore source)
            {
                _source = source;
            }

            public EcsTableCore Current => _source;

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}

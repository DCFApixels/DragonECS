using Mono.CompilerServices.SymbolWriter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;

namespace DCFApixels.DragonECS
{
    public abstract class EcsJoinQueryBase : EcsQueryBase
    {
        public abstract void ExecuteJoin();
    }
    public abstract class EcsJoinAttachQuery<TAttachComponent> : EcsJoinQueryBase
        where TAttachComponent : struct, IEcsAttachComponent
    {
        private EcsWorld _targetWorld;
        private EcsAttachPool<TAttachComponent> _targetPool;
        public EcsAttachPool<TAttachComponent> Attach => _targetPool;


        private int[] _mapping = Array.Empty<int>();
        //private LinkedList<int> 


        private bool _isInitTargetWorlds = false;

        protected sealed override void OnBuild(Builder b)
        {
            _targetPool = b.Include<TAttachComponent>();
        }
        public sealed override void ExecuteWhere()
        {
            ExecuteWhere(_targetPool.Entites, groupFilter);
        }

        public sealed override void ExecuteJoin()
        {
            ExecuteWhere(_targetPool.Entites, groupFilter);
            if (_isInitTargetWorlds == false) InitTargetWorlds();

            if (source.Capacity != _mapping.Length)
                _mapping = new int[World.Capacity];
            else
                ArrayUtility.Fill(_mapping, 0);

            foreach (var e in groupFilter)
            {
                int entityID = e.id;

            }
        }

        private void InitTargetWorlds()
        {
            foreach (var e in groupFilter)
            {
                ref readonly var rel = ref _targetPool.Read(e);
                if (rel.Target.IsNotNull)
                    _targetWorld = EcsWorld.Worlds[rel.Target.world];

                if (_targetWorld != null)
                {
                    _isInitTargetWorlds = true;
                    break;
                }
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
        }

        public NodesEnumrable GetNodes(int entityID)
        {
            throw new NotImplementedException();
        }
    }
    public abstract class EcsJoinRelationQuery<TRelationComponent> : EcsJoinQueryBase
        where TRelationComponent : struct, IEcsRelationComponent
    {
        private EcsWorld _firstWorld;
        private EcsWorld _secondWorld;
        private EcsRelationPool<TRelationComponent> _targetPool;
        public EcsRelationPool<TRelationComponent> Relation => _targetPool;

        private bool _isInitTargetWorlds = false;


        protected sealed override void OnBuild(Builder b)
        {
            _targetPool = b.Include<TRelationComponent>();
        }
        public sealed override void ExecuteWhere()
        {
            ExecuteWhere(_targetPool.Entites, groupFilter);
        }
        public sealed override void ExecuteJoin()
        {
            if (_isInitTargetWorlds == false) InitTargetWorlds();
        }

        private void InitTargetWorlds()
        {
            foreach (var e in groupFilter)
            {
                ref readonly var rel = ref _targetPool.Read(e);
                if (rel.First.IsNotNull)
                    _firstWorld = EcsWorld.Worlds[rel.First.world];
                if (rel.Second.IsNotNull)
                    _secondWorld = EcsWorld.Worlds[rel.Second.world];
                if (_firstWorld != null && _secondWorld != null)
                {
                    _isInitTargetWorlds = true;
                    break;
                }
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
        }

        public NodesEnumrable GetNodes(int entityID)
        {
            throw new NotImplementedException();
        }
    }

    public readonly ref struct NodesEnumrable
    {
        private readonly int[] _nodes;
        private readonly int _start;
        private readonly int _count;
        public NodesEnumrable(int[] nodes, int start, int count)
        {
            _nodes = nodes;
            _start = start;
            _count = count;
        }
        public NodesEnumerator GetEnumerator() => new NodesEnumerator(_nodes, _start, _count);
    }
    public ref struct NodesEnumerator
    {
        private readonly int[] _nodes;
        private readonly int _end;
        private int _index;
        public NodesEnumerator(int[] nodes, int start, int count)
        {
            _nodes = nodes;
            int end = start + count;
            _end = end < _nodes.Length ? end : _nodes.Length;
            _index = start;
        }
        public ent Current => new ent(_nodes[_index]);
        public bool MoveNext() => ++_index <= _end;
    }
}

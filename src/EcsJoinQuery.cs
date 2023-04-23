using Mono.CompilerServices.SymbolWriter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


        private int _targetWorldCapacity = -1;
        private int _targetPoolCapacity = -1;

        private int[] _mapping;
        private int[] _counts;
        //private int[] _entites;
        private EntityLinkedList _linkedBasket;

        private bool _isJoinExecuted = false;
        public bool IsJoinExecuted => _isJoinExecuted;
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
        private ProfilerMarker _execute = new ProfilerMarker("Query.ExecuteJoin");

        public sealed override void ExecuteJoin()
        {
            _execute.Begin();
            if (_isInitTargetWorlds == false)
            {
                InitTargetWorlds();
                if (_isInitTargetWorlds == false)
                    return;
            }

            //Подготовка массивов
            if (_targetWorldCapacity < _targetWorld.Capacity)
            {
                _mapping = new int[_targetWorldCapacity];
                _counts = new int[_targetWorldCapacity];
                _targetWorldCapacity = _targetWorld.Capacity;
            }
            else
            {
                ArrayUtility.Fill(_counts, 0);
                ArrayUtility.Fill(_mapping, 0);
            }
            if (_targetPoolCapacity < _targetPool.Capacity)
            {
                _linkedBasket.Resize(_targetPoolCapacity);
                _targetPoolCapacity = _targetPool.Capacity;
            }
            _linkedBasket.Clear();
            //Конец подготовки массивов

            ExecuteWhere();
            foreach (var e in groupFilter)
            {
                int attachID = e.id;
                EcsEntity attachTarget = _targetPool.Read(attachID).Target;
                // if (!attachTarget.IsAlive)//TODO пофиксить IsAlive
                //{
                //    _targetPool.Del(attachID);
                //    continue;
                //}
                int attachTargetID = attachTarget.id;

                ref int nodeIndex = ref _mapping[attachTargetID]; 
                if(nodeIndex <= 0)
                    nodeIndex = _linkedBasket.Add(attachID);
                else
                    _linkedBasket.Insert(nodeIndex, attachID);
                _counts[attachTargetID]++;
            }

            _isJoinExecuted = true;
            _execute.End();
        }
        public EntityLinkedList.EnumerableSpan GetNodes(int entityID) => _linkedBasket.Span(_mapping[entityID], _counts[entityID]);
        private void InitTargetWorlds()
        {
            foreach (var e in _targetPool.Entites)
            {
                ref readonly var rel = ref _targetPool.Read(e);
                //if (rel.Target.IsNotNull)
                    _targetWorld = EcsWorld.Worlds[rel.Target.world];

                if (_targetWorld != null)
                {
                    _isInitTargetWorlds = true;
                    break;
                }
            }

            if (_isInitTargetWorlds)
            {
                _targetWorldCapacity = _targetWorld.Capacity;
                _mapping = new int[_targetWorldCapacity];
                _counts = new int[_targetWorldCapacity];

                _targetPoolCapacity = _targetPool.Capacity;
                //_entites = new int[_targetPoolCapacity];
                _linkedBasket = new EntityLinkedList(_targetPoolCapacity);
            }
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
            return groupFilter.GetEnumerator();
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

using System;
using System.Xml.Schema;
using Unity.Profiling;
using UnityEditor.Search;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public abstract class EcsJoinAttachQueryBase : EcsQueryBase
    {
        public abstract void Join(WhereResult targetWorldWhereQuery);
    }
    public abstract class EcsJoinAttachQuery<TAttachComponent> : EcsJoinAttachQueryBase
       where TAttachComponent : struct, IEcsAttachComponent
    {
        private EcsWorld _targetWorld;
        private EcsAttachPool<TAttachComponent> _targetPool;

        private int _targetWorldCapacity = -1;
        private int _targetPoolCapacity = -1;

        private int[] _mapping;
        private int[] _counts;
        private EntityLinkedList _linkedBasket;

        private bool _isInitTargetWorld = false;

        private long _executeWhereVersion = 0;
        private long _executeJoinVersion = 0;

        private ProfilerMarker _executeWhere = new ProfilerMarker("JoinAttachQuery.Where");
        private ProfilerMarker _executeJoin = new ProfilerMarker("JoinAttachQuery.Join");

        #region Properties
        public EcsWorld AttachWorld => _targetWorld;
        public EcsAttachPool<TAttachComponent> Attach => _targetPool;

        public sealed override long WhereVersion => _executeWhereVersion;
        public long JoinVersion => _executeJoinVersion;
        #endregion

        protected sealed override void OnBuild(Builder b)
        {
            _targetPool = b.Include<TAttachComponent>();
        }
        public sealed override WhereResult Where()
        {
            using (_executeWhere.Auto())
            {
                _executeWhereVersion++;
                ExecuteWhere(_targetPool.Entities, groupFilter);
                return new WhereResult(this, WhereVersion);
            }
        }

        public sealed override void Join(WhereResult targetWorldWhereQuery)
        {
            _executeJoin.Begin();
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (targetWorldWhereQuery.IsNull)
                throw new ArgumentNullException();//TODO составить текст исключения. 
#endif
            if (!_isInitTargetWorld)
                InitTargetWorlds(targetWorldWhereQuery.World);
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            else
                if (_targetWorld != targetWorldWhereQuery.World) throw new ArgumentException();//TODO составить текст исключения. это проверка на то что пользователь использует правильный мир
#endif

            //Подготовка массивов
            if (_targetWorldCapacity < _targetWorld.Capacity)
            {
                _targetWorldCapacity = _targetWorld.Capacity;
                _mapping = new int[_targetWorldCapacity];
                _counts = new int[_targetWorldCapacity];
            }
            else
            {
                ArrayUtility.Fill(_counts, 0);
                ArrayUtility.Fill(_mapping, 0);
            }
            if (_targetPoolCapacity < _targetPool.Capacity)
            {
                _targetPoolCapacity = _targetPool.Capacity;
                _linkedBasket.Resize(_targetPoolCapacity);
            }
            _linkedBasket.Clear();
            //Конец подготовки массивов

            Where();
            foreach (var attachID in groupFilter)
            {
                EcsEntity attachTarget = _targetPool.Read(attachID).Target;
                if (!attachTarget.IsAlive)
                {
                    //_targetPool.Del(attachID);
                    continue;
                }
                int attachTargetID = attachTarget.id;

                ref int nodeIndex = ref _mapping[attachTargetID];
                if (nodeIndex <= 0)
                    nodeIndex = _linkedBasket.Add(attachID);
                else
                    _linkedBasket.Insert(nodeIndex, attachID);
                _counts[attachTargetID]++;
            }

            _executeJoinVersion++;
            _executeJoin.End();
        }
        private void InitTargetWorlds(EcsWorld targetWorld)
        {
            _targetWorld = targetWorld;

            _targetWorldCapacity = _targetWorld.Capacity;
            _mapping = new int[_targetWorldCapacity];
            _counts = new int[_targetWorldCapacity];

            _targetPoolCapacity = _targetPool.Capacity;
            _linkedBasket = new EntityLinkedList(_targetPoolCapacity);
            _isInitTargetWorld = true;
        }

        public bool Has(int attachedEnttiyID) => groupFilter.Has(attachedEnttiyID);
        public EntityLinkedList.EnumerableSpan GetNodes(int entityID) => _linkedBasket.Span(_mapping[entityID], _counts[entityID]);
        public int GetNode(int entityID) => _counts[entityID] > 0 ? _linkedBasket.Get(_mapping[entityID]) : 0;
        public int GetNodesCount(int entityID) => _counts[entityID];
        public bool HasNode(int entityID, int attachedEntityID) => groupFilter.Has(attachedEntityID) && _targetPool.Read(attachedEntityID).Target.id == entityID;
    }
    /*
    public abstract class EcsJoinRelationQuery<TRelationComponent> : EcsQueryBase
        where TRelationComponent : struct, IEcsRelationComponent
    {
        private EcsWorld _firstWorld;
        private EcsWorld _secondWorld;
        private EcsRelationPool<TRelationComponent> _targetPool;

        internal int _targetPoolCapacity = -1;

        private bool _isInitTargetWorlds = false;
        private bool _isJoinExecuted = false;

        private long _executeWhereVersion = 0;
        private long _executeJoinVersion = 0;

        public readonly Orientation ToSecond = new Orientation();
        public readonly Orientation ToFirst = new Orientation();

        private ProfilerMarker _executeWhere = new ProfilerMarker("JoinRelationQuery.Where");
        private ProfilerMarker _executeJoin = new ProfilerMarker("JoinRelationQuery.Join");

        #region Properties
        public EcsWorld RelationFirstWorld => _firstWorld;
        public EcsWorld RelationSecondWorld => _secondWorld;
        public EcsRelationPool<TRelationComponent> Relation => _targetPool;
        public bool IsMonoWorldRelation => _firstWorld == _secondWorld;

        public sealed override long WhereVersion => _executeWhereVersion;
        public long JoinVersion => _executeJoinVersion;
        #endregion

        protected sealed override void OnBuild(Builder b)
        {
            _targetPool = b.Include<TRelationComponent>();
        }
        public sealed override WhereResult Where()
        {
            using (_executeWhere.Auto())
            {
                _executeWhereVersion++;
                ExecuteWhere(_targetPool.Entities, groupFilter);
                return new WhereResult(this, WhereVersion);
            }
        }

    //TODO 
    // реализовать возможность получить список всех связей между двумя сущьностями одной напрваленности, и сделать метод для получения одной такой связи
    //

        public void Join(WhereResult firstWorldWhereQuery, WhereResult secondWorldWhereQuery)
        {
            _executeJoin.Begin();
            _isJoinExecuted = false;
            if (_isInitTargetWorlds == false)
            {
                InitTargetWorlds();
                if (_isInitTargetWorlds == false)
                    return;
            };

        //    //Подготовка массивов
        //    if (_targetWorldCapacity < _targetWorld.Capacity)
        //    {
        //        _targetWorldCapacity = _targetWorld.Capacity;
        //        _mapping = new int[_targetWorldCapacity];
        //        _counts = new int[_targetWorldCapacity];
        //    }
        //    else
        //    {
        //        ArrayUtility.Fill(_counts, 0);
        //        ArrayUtility.Fill(_mapping, 0);
        //    }
        //    if (_targetPoolCapacity < _targetPool.Capacity)
        //    {
        //        _targetPoolCapacity = _targetPool.Capacity;
        //        _linkedBasket.Resize(_targetPoolCapacity);
        //    }
        //    _linkedBasket.Clear();
        //    //Конец подготовки массивов

            Where();
            foreach (var attachID in groupFilter)
            {
                EcsEntity attachTarget = _targetPool.Read(attachID).First;
                if (!attachTarget.IsAlive)
                {
                    //_targetPool.Del(attachID);
                    continue;
                }
                int attachTargetID = attachTarget.id;

                ref int nodeIndex = ref _mapping[attachTargetID];
                if (nodeIndex <= 0)
                    nodeIndex = _linkedBasket.Add(attachID);
                else
                    _linkedBasket.Insert(nodeIndex, attachID);
                _counts[attachTargetID]++;
            }

            _isJoinExecuted = true;
            _executeJoinVersion++;
            _executeJoin.End();

            _executeJoinVersion++;
            _isJoinExecuted = true;
            _executeJoin.End();
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

            if (_isInitTargetWorlds)
            {
                _targetWorldCapacity = _targetWorld.Capacity;
                _mapping = new int[_targetWorldCapacity];
                _counts = new int[_targetWorldCapacity];

                _targetPoolCapacity = _targetPool.Capacity;
                _linkedBasket = new EntityLinkedList(_targetPoolCapacity);
            }
        }


        public bool Has(int relationEntityID) => groupFilter.Has(relationEntityID);


        public class Orientation
        {
            internal int targetWorldCapacity = -1;

            internal int[] mapping;
            internal int[] counts;
            internal EntityLinkedList linkedBasket;

            public bool HasRelation(int fromEntityID, int toEntityID)
            {
                throw new NotImplementedException();
            }
            public bool HasNode(int fromEntityID, int toEntityID)
            {
                throw new NotImplementedException();
            }
            public EntityLinkedList.EnumerableSpan GetRelations(int fromEntityID) => linkedBasket.Span(mapping[fromEntityID], counts[fromEntityID]);
            public int GetRelation(int fromEntityID) => counts[fromEntityID] > 0 ? linkedBasket.Get(mapping[fromEntityID]) : 0;
            public int GetRelationsCount(int fromEntityID) => counts[fromEntityID];
        }
    }*/

    #region Extensions
    public static class EcsJoinQueryBaseExtensions
    {
        public static void Join(this EcsJoinAttachQueryBase self, EcsWorld targetWorld)
        {
            self.Join(targetWorld.Where<EmptyQuery>());
        }

        public static TQuery Join<TQuery>(this EcsWorld self, EcsWorld targetWorld, out TQuery query) where TQuery : EcsJoinAttachQueryBase
        {
            return self.Join(targetWorld.WhereAll(), out query);
        }
        public static TQuery Join<TQuery>(this EcsWorld self, EcsWorld targetWorld) where TQuery : EcsJoinAttachQueryBase
        {
            return self.Join<TQuery>(targetWorld.WhereAll());
        }
        /* public static class EcsJoinRelationQueryExtensions
         {
             public static void Join<TRelationComponent>(this EcsJoinRelationQuery<TRelationComponent> self, EcsWorld firstWorld, EcsWorld secondWorld)
                 where TRelationComponent : struct, IEcsRelationComponent
             {
                 self.Join(firstWorld.Where<EmptyQuery>(), secondWorld.Where<EmptyQuery>());
             }
             public static void Join<TRelationComponent>(this EcsJoinRelationQuery<TRelationComponent> self, EcsWorld firstWorld, WhereResult secondWorldWhereQuery)
                 where TRelationComponent : struct, IEcsRelationComponent
             {
                 self.Join(firstWorld.Where<EmptyQuery>(), secondWorldWhereQuery);
             }
             public static void Join<TRelationComponent>(this EcsJoinRelationQuery<TRelationComponent> self, WhereResult firstWorldWhereQuery, EcsWorld secondWorld)
                 where TRelationComponent : struct, IEcsRelationComponent
             {
                 self.Join(firstWorldWhereQuery, secondWorld.Where<EmptyQuery>());
             }
         }*/
    }
    #endregion
}

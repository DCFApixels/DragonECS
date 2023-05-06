using System;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsJoinHierarchyExecutor<TSubject, TAttachComponent> : EcsQueryExecutor
        where TSubject : EcsSubject
        where TAttachComponent : struct, IEcsAttachComponent
    {
        private readonly TSubject _subject;
        internal readonly EcsGroup _filteredGroup;
        private EcsWorld _targetWorld;

        private EcsAttachPool<TAttachComponent> _targetPool;

        private int _targetWorldCapacity = -1;
        private int _targetPoolCapacity = -1;

        private int[] _mapping;
        private int[] _counts;
        private EntityLinkedList _linkedBasket;

        private bool _isInitTargetWorld = false;

        private ProfilerMarker _executeWhere = new ProfilerMarker("JoinAttachQuery.Where");
        private ProfilerMarker _executeJoin = new ProfilerMarker("JoinAttachQuery.Join");

        private long _executeVersion;

        #region Properties
        public TSubject Subject => _subject;
        internal long ExecuteVersion => _executeVersion;
        #endregion

        #region Constructors
        public EcsJoinHierarchyExecutor(TSubject subject)
        {
            _subject = subject;
            _filteredGroup = EcsGroup.New(subject.World);
            _targetPool = subject.World.GetPool<TAttachComponent>();
        }
        #endregion

        #region Methods
        public EcsJoinHierarchyResult<TSubject, TAttachComponent> Execute() => ExecuteFor(_targetPool.Entities);
        public EcsJoinHierarchyResult<TSubject, TAttachComponent> ExecuteFor(EcsReadonlyGroup sourceGroup)
        {
            var world = _subject.World;
            _executeJoin.Begin();
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (sourceGroup.IsNull) throw new ArgumentNullException();//TODO составить текст исключения. 
#endif
            if (!_isInitTargetWorld)
                InitTargetWorlds(sourceGroup.World);
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            else
                if (_targetWorld != sourceGroup.World) throw new ArgumentException();//TODO составить текст исключения. это проверка на то что пользователь использует правильный мир
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

            var iterator = new EcsSubjectIterator<TSubject>(_subject, sourceGroup);
            foreach (var attachID in iterator)
            {
                entlong attachTarget = _targetPool.Read(attachID).Target;
                if (!attachTarget.IsAlive)
                {
                    //_targetPool.Del(attachID);
                    continue;
                }
                int attachTargetID = attachTarget.id;
                //if (!CheckMaskInternal(targetWorldWhereQuery.query.mask, attachTargetID)) continue; //TODO проверить что все работает //исчключить все аттачи, цели которых не входят в targetWorldWhereQuery 

                ref int nodeIndex = ref _mapping[attachTargetID];
                if (nodeIndex <= 0)
                    nodeIndex = _linkedBasket.Add(attachID);
                else
                    _linkedBasket.Insert(nodeIndex, attachID);
                _counts[attachTargetID]++;
            }

            _executeVersion++;
            _executeJoin.End();

            return new EcsJoinHierarchyResult<TSubject, TAttachComponent>(_subject, this , _executeVersion); 
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
        internal sealed override void Destroy()
        {
            _filteredGroup.Release();
            _targetWorld = null;
            _mapping = null;
            _counts = null;
            _linkedBasket = null;
        }
        #endregion

        #region Internal result methods
        internal bool Has(int attachedEnttiyID) => _filteredGroup.Has(attachedEnttiyID);

        internal EntityLinkedList.EnumerableSpan GetNodes(int entityID) => _linkedBasket.Span(_mapping[entityID], _counts[entityID]);
        internal int GetNode(int entityID) => _counts[entityID] > 0 ? _linkedBasket.Get(_mapping[entityID]) : 0;
        internal int GetNodesCount(int entityID) => _counts[entityID];
        internal bool HasNode(int entityID, int attachedEntityID) => _filteredGroup.Has(attachedEntityID) && _targetPool.Read(attachedEntityID).Target.id == entityID;


        internal EntityLinkedList.EnumerableSpan GetSubNodes(int entityID) => throw new NotImplementedException();
        internal int GetSubNode(int entityID) => throw new NotImplementedException();
        internal bool GetSubNodesCount(int entityID, int attachedEntityID) => throw new NotImplementedException();
        internal bool HasSubNode(int entityID, int attachedEntityID) => throw new NotImplementedException();
        #endregion
    }

    #region JoinAttachExecuter Results
    public readonly ref struct EcsJoinHierarchyResult<TSubject, TAttachComponent>
        where TSubject : EcsSubject
        where TAttachComponent : struct, IEcsAttachComponent
    {
        public readonly TSubject s;
        private readonly EcsJoinHierarchyExecutor<TSubject, TAttachComponent> _executer;
        private readonly long _verison;

        public bool IsRelevant => _verison == _executer.ExecuteVersion;

        public EcsJoinHierarchyResult(TSubject s, EcsJoinHierarchyExecutor<TSubject, TAttachComponent> executer, long version)
        {
            this.s = s;
            _executer = executer;
            _verison = version;
        }

        public bool Has(int attachedEnttiyID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!IsRelevant) throw new InvalidOperationException();//TODO составить текст исключения. 
#endif
            return _executer.Has(attachedEnttiyID);
        }
        public EntityLinkedList.EnumerableSpan GetNodes(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!IsRelevant) throw new InvalidOperationException();//TODO составить текст исключения. 
#endif
            return _executer.GetNodes(entityID);
        }
        public int GetNode(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!IsRelevant) throw new InvalidOperationException();//TODO составить текст исключения. 
#endif
            return _executer.GetNode(entityID);
        }
        public int GetNodesCount(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!IsRelevant) throw new InvalidOperationException();//TODO составить текст исключения. 
#endif
            return _executer.GetNodesCount(entityID);
        }
        public bool HasNode(int entityID, int attachedEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!IsRelevant) throw new InvalidOperationException();//TODO составить текст исключения. 
#endif
            return _executer.HasNode(entityID, attachedEntityID);
        }
    }
    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace DCFApixels.DragonECS.Core
{
    using VertexID = DependencyGraphVertextID;
    public class LayersMap : IDependencyGraph<string>
    {
        #region IDependencyGraph
        VertexID IDependencyGraph<string>.AddVertex(string vertex, bool isLocked) { return _graph.AddVertex(vertex, isLocked); }
        bool IDependencyGraph<string>.ContainsVertex(string vertex) { return _graph.ContainsVertex(vertex); }
        VertexID IDependencyGraph<string>.GetVertexID(string vertex) { return _graph.GetVertexID(vertex); }
        string IDependencyGraph<string>.GetVertexFromID(VertexID vertexID) { return _graph.GetVertexFromID(vertexID); }
        bool IDependencyGraph<string>.RemoveVertex(string vertex) { return _graph.RemoveVertex(vertex); }
        void IDependencyGraph<string>.AddDependency(VertexID fromID, VertexID toID, bool moveToRight) { _graph.AddDependency(fromID, toID, moveToRight); }
        string[] IDependencyGraph<string>.Sort() { return _graph.Sort(); }
        #endregion

        private readonly IDependencyGraph<string> _graph;
        private readonly EcsPipeline.Builder _pipelineBuilder;
        //private readonly string _preBeginLayer;

        #region Properties
        public EcsPipeline.Builder Back
        {
            get { return _pipelineBuilder; }
        }
        public int Count
        {
            get { return _graph.Count; }
        }
        public ReadonlyDependenciesCollection<string> Dependencies
        {
            get { return _graph.Dependencies; }
        }
        #endregion

        #region Constructors
        public LayersMap(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder)
        {
            _graph = graph;
            _pipelineBuilder = pipelineBuilder;
        }
        public LayersMap(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, string preBeginlayer, string beginlayer, string basicLayer, string endLayer, string postEndLayer)
        {
            _graph = graph;
            _pipelineBuilder = pipelineBuilder;

            graph.AddVertex(preBeginlayer, true);
            graph.AddVertex(beginlayer, true);
            graph.AddVertex(basicLayer, true);
            graph.AddVertex(endLayer, true);
            graph.AddVertex(postEndLayer, true);

            Move(preBeginlayer);
            //.Before(beginlayer);
            Move(beginlayer)
                //.Before(basicLayer)
                .After(preBeginlayer);
            Move(basicLayer)
                //.Before(endLayer)
                .After(beginlayer);
            Move(endLayer)
                //.Before(postEndLayer)
                .After(basicLayer);
            Move(postEndLayer)
                .After(endLayer);
        }
        #endregion

        #region Add
        public MoveHandler Add(string layer)
        {
            VertexID id = _graph.AddVertex(layer, false);
            return new MoveHandler(_graph, _pipelineBuilder, id);
        }
        public MoveHandler Add(params string[] layers)
        {
            return Add(layersRange: layers);
        }
        public MoveHandler Add(IEnumerable<string> layersRange)
        {
            foreach (var layer in layersRange)
            {
                Add(layer);
            }
            return new MoveHandler(_graph, _pipelineBuilder, layersRange);
        }
        #endregion

        #region Move
        public MoveHandler Move(string layer)
        {
            VertexID id = _graph.GetVertexID(layer);
            return new MoveHandler(_graph, _pipelineBuilder, id);
        }
        public MoveHandler Move(params string[] layers)
        {
            return new MoveHandler(_graph, _pipelineBuilder, layers);
        }
        public MoveHandler Move(IEnumerable<string> layersRange)
        {
            return new MoveHandler(_graph, _pipelineBuilder, layersRange);
        }
        #endregion

        #region MoveHandler
        public struct MoveHandler
        {
            private readonly IDependencyGraph<string> _graph;
            private readonly EcsPipeline.Builder _pipelineBuilder;
            private readonly VertexID _layerID;
            private readonly IEnumerable<string> _layersRange;

            #region Properties
            public EcsPipeline.Builder Back
            {
                get { return _pipelineBuilder; }
            }
            #endregion

            #region Constructors
            public MoveHandler(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, VertexID id)
            {
                _graph = graph;
                _pipelineBuilder = pipelineBuilder;
                _layerID = id;
                _layersRange = null;
            }
            public MoveHandler(IDependencyGraph<string> graph, EcsPipeline.Builder pipelineBuilder, IEnumerable<string> layersRange)
            {
                _graph = graph;
                _pipelineBuilder = pipelineBuilder;
                _layerID = VertexID.NULL;
                _layersRange = layersRange;
            }
            #endregion

            #region Before
            public MoveHandler Before(params string[] targets)
            {
                return Before(targetsRange: targets);
            }
            public MoveHandler Before(IEnumerable<string> targetsRange)
            {
                foreach (var target in targetsRange)
                {
                    Before(target);
                }
                return this;
            }
            public MoveHandler Before(string targetLayer)
            {
                if (_layerID != VertexID.NULL)
                {
                    _graph.AddDependency(_layerID, _graph.GetVertexID(targetLayer), true);
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _graph.AddDependency(_graph.GetVertexID(layer), _graph.GetVertexID(targetLayer), true);
                    }
                }
                return this;
            }
            #endregion

            #region After
            public MoveHandler After(params string[] targets)
            {
                return After(targetsRange: targets);
            }
            public MoveHandler After(IEnumerable<string> targetsRange)
            {
                foreach (var target in targetsRange)
                {
                    After(target);
                }
                return this;
            }
            public MoveHandler After(string targetLayer)
            {
                if (_layerID != VertexID.NULL)
                {
                    _graph.AddDependency(_graph.GetVertexID(targetLayer), _layerID, false);
                }
                if (_layersRange != null)
                {
                    foreach (var layer in _layersRange)
                    {
                        _graph.AddDependency(_graph.GetVertexID(targetLayer), _graph.GetVertexID(layer), false);
                    }
                }
                return this;
            }
            #endregion
        }
        #endregion

        #region MergeWith
        public void MergeWith(IDependencyGraph<string> other)
        {
            _graph.MergeWith(other);
        }
        //[Obsolete("Use MergeWith(LayersMap)")]
        public void MergeWith(IReadOnlyList<string> other)
        {
            var enumerator = other.GetEnumerator();
            string prev = null;
            //if (_preBeginLayer != null)
            //{
            //    while (enumerator.MoveNext())
            //    {
            //        var layer = enumerator.Current;
            //        if (layer == _preBeginLayer) { break; }
            //
            //        Add(layer);
            //        if (prev != null)
            //        {
            //            Move(prev).Before(layer);
            //        }
            //        prev = layer;
            //    }
            //}
            while (enumerator.MoveNext())
            {
                var layer = enumerator.Current;
                Add(layer);
                if (prev != null)
                {
                    Move(layer).After(prev);
                }
                prev = layer;
            }
        }
        #endregion;

        #region Other
        public bool Contains(string layer)
        {
            return _graph.ContainsVertex(layer);
        }
        public IEnumerator<string> GetEnumerator() { return _graph.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _graph.GetEnumerator(); }
        #endregion

        #region Build
        public string[] Build()
        {
            return _graph.Sort();
        }
        #endregion

        #region Obsolete
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, string newLayer)
        {
            Add(newLayer).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, string newLayer)
        {
            Add(newLayer).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layer).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, string newLayer)
        {
            Move(newLayer).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layer).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, string newLayer)
        {
            Move(newLayer).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Insert(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Add(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder InsertAfter(string targetLayer, params string[] newLayers)
        {
            Add(newLayers).After(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layers).Before(targetLayer).Back;")]
        public EcsPipeline.Builder Move(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).Before(targetLayer);
            return _pipelineBuilder;
        }
        [Obsolete("Use " + nameof(LayersMap) + ".Move(layers).After(targetLayer).Back;")]
        public EcsPipeline.Builder MoveAfter(string targetLayer, params string[] movingLayers)
        {
            Move(movingLayers).After(targetLayer);
            return _pipelineBuilder;
        }

        [Obsolete]
        public object this[int index]
        {
            get
            {
                int i = 0;
                foreach (var item in this)
                {
                    if (i == index)
                    {
                        return item;
                    }
                    i++;
                }
                return null;
            }
        }
        #endregion
    }
}
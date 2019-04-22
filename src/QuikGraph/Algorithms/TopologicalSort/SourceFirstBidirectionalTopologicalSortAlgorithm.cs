﻿#if SUPPORTS_SERIALIZATION
using System;
#endif
using System.Collections.Generic;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
#endif
using QuikGraph.Collections;

namespace QuikGraph.Algorithms.TopologicalSort
{
    public enum TopologicalSortDirection
    {
        Forward,
        Backward
    }

#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class SourceFirstBidirectionalTopologicalSortAlgorithm<TVertex, TEdge> :
        AlgorithmBase<IBidirectionalGraph<TVertex, TEdge>>
        where TEdge : IEdge<TVertex>
    {
        private IDictionary<TVertex, int> predCounts = new Dictionary<TVertex, int>();
        private BinaryQueue<TVertex, int> heap;
        private IList<TVertex> sortedVertices = new List<TVertex>();
        private TopologicalSortDirection direction = TopologicalSortDirection.Forward;

        public SourceFirstBidirectionalTopologicalSortAlgorithm(
            IBidirectionalGraph<TVertex, TEdge> visitedGraph
            )
            : this(visitedGraph, TopologicalSortDirection.Forward)
        {
        }

        public SourceFirstBidirectionalTopologicalSortAlgorithm(
            IBidirectionalGraph<TVertex, TEdge> visitedGraph,
            TopologicalSortDirection direction
            )
            : base(visitedGraph)
        {
            this.direction = direction;
            this.heap = new BinaryQueue<TVertex, int>(e => this.predCounts[e]);
        }

        public ICollection<TVertex> SortedVertices
        {
            get
            {
                return this.sortedVertices;
            }
        }

        public BinaryQueue<TVertex, int> Heap
        {
            get
            {
                return this.heap;
            }
        }

        public IDictionary<TVertex, int> InDegrees
        {
            get
            {
                return this.predCounts;
            }
        }

        public event VertexAction<TVertex> AddVertex;
        private void OnAddVertex(TVertex v)
        {
            var eh = this.AddVertex;
            if (eh != null)
                eh(v);
        }

        public void Compute(IList<TVertex> vertices)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(vertices != null);
#endif

            this.sortedVertices = vertices;
            Compute();
        }


        protected override void InternalCompute()
        {
            var cancelManager = this.Services.CancelManager;
            this.InitializeInDegrees();

            while (this.heap.Count != 0)
            {
                if (cancelManager.IsCancelling) break;

                TVertex v = this.heap.Dequeue();
                if (this.predCounts[v] != 0)
                    throw new NonAcyclicGraphException();

                this.sortedVertices.Add(v);
                this.OnAddVertex(v);

                // update the count of its successor vertices
                var succEdges = (this.direction == TopologicalSortDirection.Forward) ? this.VisitedGraph.OutEdges(v) : this.VisitedGraph.InEdges(v);
                foreach (var e in succEdges)
                {
                    if (e.Source.Equals(e.Target))
                        continue;
                    var succ = (direction == TopologicalSortDirection.Forward) ? e.Target : e.Source;
                    this.predCounts[succ]--;

#if SUPPORTS_CONTRACTS
                    Contract.Assert(this.predCounts[succ] >= 0);
#endif

                    this.heap.Update(succ);
                }
            }
        }

        private void InitializeInDegrees()
        {
            foreach (var v in this.VisitedGraph.Vertices)
            {
                this.predCounts.Add(v, 0);
            }

            foreach (var e in this.VisitedGraph.Edges)
            {
                if (e.Source.Equals(e.Target))
                    continue;
                var succ = (direction == TopologicalSortDirection.Forward) ? e.Target : e.Source;
                this.predCounts[succ]++;
            }

            foreach (var v in this.VisitedGraph.Vertices)
            {
                this.heap.Enqueue(v);
            }
        }
    }
}
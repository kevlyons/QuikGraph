﻿using System;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
#endif
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.Search;
using QuikGraph.Algorithms.Services;
using QuikGraph.Collections;
using QuikGraph.Predicates;

namespace QuikGraph.Algorithms.MaximumFlow
{
    /// <summary>
    /// Edmond and Karp maximum flow algorithm for directed graph with positive capacities and flows.
    /// </summary>
    /// <typeparam name="TVertex">type of a vertex</typeparam>
    /// <typeparam name="TEdge">type of an edge</typeparam>
    /// <remarks>
    /// Will throw an exception in <see cref="ReversedEdgeAugmentorAlgorithm{TVertex, TEdge}.AddReversedEdges"/> if TEdge is a value type,
    /// e.g. <see cref="SEdge{TVertex}"/>.
    /// <seealso href="https://github.com/YaccConstructor/QuickGraph/issues/183#issue-377613647"/>.
    /// </remarks>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class EdmondsKarpMaximumFlowAlgorithm<TVertex, TEdge>
        : MaximumFlowAlgorithm<TVertex,TEdge>
        where TEdge : IEdge<TVertex>
    {
        public EdmondsKarpMaximumFlowAlgorithm(
            IMutableVertexAndEdgeListGraph<TVertex, TEdge> g,
            Func<TEdge, double> capacities,
            EdgeFactory<TVertex, TEdge> edgeFactory,
            ReversedEdgeAugmentorAlgorithm<TVertex, TEdge> reversedEdgeAugmentorAlgorithm
            )
            : this(null, g, capacities, edgeFactory, reversedEdgeAugmentorAlgorithm)
        { }

		public EdmondsKarpMaximumFlowAlgorithm(
            IAlgorithmComponent host,
            IMutableVertexAndEdgeListGraph<TVertex, TEdge> g,
			Func<TEdge,double> capacities,
            EdgeFactory<TVertex, TEdge> edgeFactory,
            ReversedEdgeAugmentorAlgorithm<TVertex, TEdge> reversedEdgeAugmentorAlgorithm

            )
            : base(host, g, capacities, edgeFactory)
		{
		    ReversedEdges = reversedEdgeAugmentorAlgorithm.ReversedEdges;
		}
	
		private IVertexListGraph<TVertex,TEdge> ResidualGraph
		{
			get
			{
				return new FilteredVertexListGraph<
                        TVertex,
                        TEdge,
                        IVertexListGraph<TVertex,TEdge>
                        >(
        					VisitedGraph,
                            v => true,
				        	new ResidualEdgePredicate<TVertex,TEdge>(ResidualCapacities).Test
    					);
			}
		}

	
		private void Augment(
			TVertex source,
			TVertex sink
			)
		{
#if SUPPORTS_CONTRACTS
            Contract.Requires(source != null);
            Contract.Requires(sink != null);
#endif

			TEdge e;
			TVertex u;

			// find minimum residual capacity along the augmenting path
			double delta = double.MaxValue;
            u = sink;
            do
			{
                e = Predecessors[u];
                delta = Math.Min(delta, ResidualCapacities[e]);
                u = e.Source;
			} while (!u.Equals(source));

			// push delta units of flow along the augmenting path
            u = sink;
            do 
			{
                e = Predecessors[u];
                ResidualCapacities[e] -= delta;
                if (ReversedEdges != null && ReversedEdges.ContainsKey(e))
                {
                    ResidualCapacities[ReversedEdges[e]] += delta;
                }
				u = e.Source;
			} while (!u.Equals(source));
		}
    
		/// <summary>
		/// Computes the maximum flow between Source and Sink.
		/// </summary>
		/// <returns></returns>
        protected override void InternalCompute()
        {
            if (this.Source == null)
                throw new InvalidOperationException("Source is not specified");
            if (this.Sink == null)
                throw new InvalidOperationException("Sink is not specified");


            if (this.Services.CancelManager.IsCancelling)
                return;

            var g = this.VisitedGraph;
            foreach (var u in g.Vertices)
                foreach (var e in g.OutEdges(u))
                {
                    var capacity = this.Capacities(e);
                    if (capacity < 0)
                        throw new InvalidOperationException("negative edge capacity");
                    this.ResidualCapacities[e] = capacity;
                }

            this.VertexColors[Sink] = GraphColor.Gray;
            while (this.VertexColors[Sink] != GraphColor.White)
            {
                var vis = new VertexPredecessorRecorderObserver<TVertex, TEdge>(
                    this.Predecessors
                    );
                var queue = new Queue<TVertex>();
                var bfs = new BreadthFirstSearchAlgorithm<TVertex, TEdge>(
                    this.ResidualGraph,
                    queue,
                    this.VertexColors
                    );
                using (vis.Attach(bfs))
                    bfs.Compute(this.Source);

                if (this.VertexColors[this.Sink] != GraphColor.White)
                    this.Augment(this.Source, this.Sink);
            } // while

            this.MaxFlow = 0;
            foreach (var e in g.OutEdges(Source))
                this.MaxFlow += (this.Capacities(e) - this.ResidualCapacities[e]);


           
        }
	}

}
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if SUPPORTS_CONTRACTS
using System.Diagnostics.Contracts;
using System.Linq;
#endif
using QuikGraph.Algorithms.Condensation;
using QuikGraph.Algorithms.ConnectedComponents;
using QuikGraph.Algorithms.MaximumFlow;
using QuikGraph.Algorithms.MinimumSpanningTree;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.RandomWalks;
using QuikGraph.Algorithms.RankedShortestPath;
using QuikGraph.Algorithms.Search;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Algorithms.TopologicalSort;
using QuikGraph.Collections;

#if !SUPPORTS_TYPE_FULL_FEATURES
using QuikGraph.Utils;
#endif

namespace QuikGraph.Algorithms
{
    /// <summary>
    /// Various extension methods to build algorithms
    /// </summary>
    public static class AlgorithmExtensions
    {
        /// <summary>
        /// Returns the method that implement the access indexer.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static Func<TKey, TValue> GetIndexer<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(dictionary != null);
            Contract.Ensures(Contract.Result<Func<TKey, TValue>>() != null);
#endif

#if SUPPORTS_TYPE_FULL_FEATURES
            var method = dictionary.GetType().GetProperty("Item").GetGetMethod();
            return (Func<TKey, TValue>)Delegate.CreateDelegate(typeof(Func<TKey, TValue>), dictionary, method, true);
#else
            return key => dictionary[key];
#endif
        }

        /// <summary>
        /// Gets the vertex identity.
        /// </summary>
        /// <remarks>
        /// Returns more efficient methods for primitive types,
        /// otherwise builds a dictionary
        /// </remarks>
        /// <typeparam name="TVertex">The type of the vertex.</typeparam>
        /// <param name="graph">The graph.</param>
        /// <returns></returns>
        public static VertexIdentity<TVertex> GetVertexIdentity<TVertex>(this IVertexSet<TVertex> graph)
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(graph != null);
#endif

            // Simpler identity for primitive types
#if SUPPORTS_TYPE_FULL_FEATURES
            switch (Type.GetTypeCode(typeof(TVertex)))
#else
            switch (TypeUtils.GetTypeCode(typeof(TVertex)))
#endif
            {
                case TypeCode.String:
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return (v) => v.ToString();
            }

            // Create dictionary
            var ids = new Dictionary<TVertex, string>(graph.VertexCount);
            return v =>
                {
                    string id;
                    if (!ids.TryGetValue(v, out id))
                        ids[v] = id = ids.Count.ToString();
                    return id;
                };
        }

        /// <summary>
        /// Gets the edge identity.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertex.</typeparam>
        /// <typeparam name="TEdge">The type of the edge.</typeparam>
        /// <param name="graph">The graph.</param>
        /// <returns></returns>
        public static EdgeIdentity<TVertex, TEdge> GetEdgeIdentity<TVertex, TEdge>(this IEdgeSet<TVertex, TEdge> graph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(graph != null);
#endif

            // Create dictionary
            var ids = new Dictionary<TEdge, string>(graph.EdgeCount);
            return e =>
            {
                string id;
                if (!ids.TryGetValue(e, out id))
                    ids[e] = id = ids.Count.ToString();
                return id;
            };
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> TreeBreadthFirstSearch<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph,
            TVertex root)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(root != null);
            Contract.Requires(visitedGraph.ContainsVertex(root));
            Contract.Ensures(Contract.Result<TryFunc<TVertex, IEnumerable<TEdge>>>() != null);
#endif

            var algo = new BreadthFirstSearchAlgorithm<TVertex, TEdge>(visitedGraph);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algo))
                algo.Compute(root);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        /// <summary>
        /// Computes a depth first tree.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertex.</typeparam>
        /// <typeparam name="TEdge">The type of the edge.</typeparam>
        /// <param name="visitedGraph">The visited graph.</param>
        /// <param name="root">The root.</param>
        /// <returns></returns>
        public static TryFunc<TVertex, IEnumerable<TEdge>> TreeDepthFirstSearch<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph,
            TVertex root)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(root != null);
            Contract.Requires(visitedGraph.ContainsVertex(root));
            Contract.Ensures(Contract.Result<TryFunc<TVertex, IEnumerable<TEdge>>>() != null);
#endif

            var algo = new DepthFirstSearchAlgorithm<TVertex, TEdge>(visitedGraph);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algo))
                algo.Compute(root);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> TreeCyclePoppingRandom<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph,
            TVertex root)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(root != null);
            Contract.Requires(visitedGraph.ContainsVertex(root));
            Contract.Ensures(Contract.Result<TryFunc<TVertex, IEnumerable<TEdge>>>() != null);
#endif

            return TreeCyclePoppingRandom(visitedGraph, root, new NormalizedMarkovEdgeChain<TVertex, TEdge>());
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> TreeCyclePoppingRandom<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph,
            TVertex root,
            IMarkovEdgeChain<TVertex, TEdge> edgeChain)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(root != null);
            Contract.Requires(visitedGraph.ContainsVertex(root));
            Contract.Ensures(Contract.Result<TryFunc<TVertex, IEnumerable<TEdge>>>() != null);
#endif
            var algo = new CyclePoppingRandomTreeAlgorithm<TVertex, TEdge>(visitedGraph, edgeChain);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algo))
                algo.Compute(root);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        #region shortest paths

        public static TryFunc<TVertex, IEnumerable<TEdge>> ShortestPathsDijkstra<TVertex, TEdge>(
            this IUndirectedGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeWeights,
            TVertex source)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeWeights != null);
            Contract.Requires(source != null);
#endif

            var algorithm = new UndirectedDijkstraShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, edgeWeights);
            var predecessorRecorder = new UndirectedVertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algorithm))
                algorithm.Compute(source);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> ShortestPathsAStar<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeWeights,
            Func<TVertex, double> costHeuristic,
            TVertex source)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeWeights != null);
            Contract.Requires(costHeuristic != null);
            Contract.Requires(source != null);
#endif

            var algorithm = new AStarShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, edgeWeights, costHeuristic);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algorithm))
                algorithm.Compute(source);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> ShortestPathsDijkstra<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeWeights,
            TVertex source)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeWeights != null);
            Contract.Requires(source != null);
#endif

            var algorithm = new DijkstraShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, edgeWeights);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algorithm))
                algorithm.Compute(source);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> ShortestPathsBellmanFord<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeWeights,
            TVertex source)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeWeights != null);
            Contract.Requires(source != null);
#endif

            var algorithm = new BellmanFordShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, edgeWeights);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algorithm))
                algorithm.Compute(source);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        public static TryFunc<TVertex, IEnumerable<TEdge>> ShortestPathsDag<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeWeights,
            TVertex source)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeWeights != null);
            Contract.Requires(source != null);
#endif

            var algorithm = new DagShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, edgeWeights);
            var predecessorRecorder = new VertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (predecessorRecorder.Attach(algorithm))
                algorithm.Compute(source);

            var predecessors = predecessorRecorder.VertexPredecessors;
            return delegate (TVertex v, out IEnumerable<TEdge> edges)
            {
                return EdgeExtensions.TryGetPath(predecessors, v, out edges);
            };
        }

        #endregion

        #region K-Shortest path

        /// <summary>
        /// Computes the k-shortest path from <paramref name="source"/>
        /// <paramref name="target"/> using Hoffman-Pavley algorithm.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <param name="edgeWeights"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="pathCount"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<TEdge>> RankedShortestPathHoffmanPavley<TVertex, TEdge>(
            this IBidirectionalGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeWeights,
            TVertex source,
            TVertex target,
            int pathCount)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeWeights != null);
            Contract.Requires(source != null && visitedGraph.ContainsVertex(source));
            Contract.Requires(target != null && visitedGraph.ContainsVertex(target));
            Contract.Requires(pathCount > 1);
#endif

            var algo = new HoffmanPavleyRankedShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, edgeWeights);
            algo.ShortestPathCount = pathCount;
            algo.Compute(source, target);

            return algo.ComputedShortestPaths;
        }

        #endregion

        /// <summary>
        /// Gets the list of sink vertices
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <returns></returns>
        public static IEnumerable<TVertex> Sinks<TVertex, TEdge>(this IVertexListGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            return SinksIterator<TVertex, TEdge>(visitedGraph);
        }

        [DebuggerHidden]
        private static IEnumerable<TVertex> SinksIterator<TVertex, TEdge>(IVertexListGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
            foreach (var v in visitedGraph.Vertices)
                if (visitedGraph.IsOutEdgesEmpty(v))
                    yield return v;
        }

        /// <summary>
        /// Gets the list of root vertices
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <returns></returns>
        public static IEnumerable<TVertex> Roots<TVertex, TEdge>(this IBidirectionalGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            return RootsIterator(visitedGraph);
        }

        [DebuggerHidden]
        private static IEnumerable<TVertex> RootsIterator<TVertex, TEdge>(
            IBidirectionalGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
            foreach (var v in visitedGraph.Vertices)
                if (visitedGraph.IsInEdgesEmpty(v))
                    yield return v;
        }

        /// <summary>
        /// Gets the list of isolated vertices (no incoming or outcoming vertices)
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <returns></returns>
        public static IEnumerable<TVertex> IsolatedVertices<TVertex, TEdge>(this IBidirectionalGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            return IsolatedVerticesIterator(visitedGraph);
        }

        [DebuggerHidden]
        private static IEnumerable<TVertex> IsolatedVerticesIterator<TVertex, TEdge>(
            IBidirectionalGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
            foreach (var v in visitedGraph.Vertices)
                if (visitedGraph.Degree(v) == 0)
                    yield return v;
        }

        /// <summary>
        /// Gets the list of roots
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <returns></returns>
        public static IEnumerable<TVertex> Roots<TVertex, TEdge>(this IVertexListGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            return RootsIterator<TVertex, TEdge>(visitedGraph);
        }

        [DebuggerHidden]
        private static IEnumerable<TVertex> RootsIterator<TVertex, TEdge>(
            IVertexListGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
            var notRoots = new Dictionary<TVertex, bool>(visitedGraph.VertexCount);
            var dfs = new DepthFirstSearchAlgorithm<TVertex, TEdge>(visitedGraph);
            dfs.ExamineEdge += e => notRoots[e.Target] = false;
            dfs.Compute();

            foreach (var vertex in visitedGraph.Vertices)
            {
                bool value;
                if (!notRoots.TryGetValue(vertex, out value))
                    yield return vertex;
            }
        }

        /// <summary>
        /// Creates a topological sort of a undirected
        /// acyclic graph.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <returns></returns>
        /// <exception cref="NonAcyclicGraphException">the input graph
        /// has a cycle</exception>
        public static IEnumerable<TVertex> TopologicalSort<TVertex, TEdge>(this IUndirectedGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            var vertices = new List<TVertex>(visitedGraph.VertexCount);
            TopologicalSort(visitedGraph, vertices);
            return vertices;
        }


        /// <summary>
        /// Creates a topological sort of a undirected
        /// acyclic graph.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        /// <exception cref="NonAcyclicGraphException">the input graph
        /// has a cycle</exception>
        public static void TopologicalSort<TVertex, TEdge>(
            this IUndirectedGraph<TVertex, TEdge> visitedGraph,
            IList<TVertex> vertices)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(vertices != null);
#endif

            var topo = new UndirectedTopologicalSortAlgorithm<TVertex, TEdge>(visitedGraph);
            topo.Compute(vertices);
        }

        /// <summary>
        /// Creates a topological sort of a directed
        /// acyclic graph.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <returns></returns>
        /// <exception cref="NonAcyclicGraphException">the input graph
        /// has a cycle</exception>
        public static IEnumerable<TVertex> TopologicalSort<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            var vertices = new List<TVertex>(visitedGraph.VertexCount);
            TopologicalSort(visitedGraph, vertices);
            return vertices;
        }

        /// <summary>
        /// Creates a topological sort of a directed
        /// acyclic graph.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        /// <exception cref="NonAcyclicGraphException">the input graph
        /// has a cycle</exception>
        public static void TopologicalSort<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph,
            IList<TVertex> vertices)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(vertices != null);
#endif

            var topo = new TopologicalSortAlgorithm<TVertex, TEdge>(visitedGraph);
            topo.Compute(vertices);
        }

        public static IEnumerable<TVertex> SourceFirstTopologicalSort<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            var vertices = new List<TVertex>(visitedGraph.VertexCount);
            SourceFirstTopologicalSort(visitedGraph, vertices);
            return vertices;
        }

        public static void SourceFirstTopologicalSort<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph,
            IList<TVertex> vertices)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(vertices != null);
#endif

            var topo = new SourceFirstTopologicalSortAlgorithm<TVertex, TEdge>(visitedGraph);
            topo.Compute(vertices);
        }

        public static IEnumerable<TVertex> SourceFirstBidirectionalTopologicalSort<TVertex, TEdge>(
            this IBidirectionalGraph<TVertex, TEdge> visitedGraph,
            TopologicalSortDirection direction)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            var vertices = new List<TVertex>(visitedGraph.VertexCount);
            SourceFirstBidirectionalTopologicalSort(visitedGraph, vertices, direction);
            return vertices;
        }

        public static IEnumerable<TVertex> SourceFirstBidirectionalTopologicalSort<TVertex, TEdge>(
            this IBidirectionalGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
            return SourceFirstBidirectionalTopologicalSort(visitedGraph, TopologicalSortDirection.Forward);
        }

        public static void SourceFirstBidirectionalTopologicalSort<TVertex, TEdge>(
            this IBidirectionalGraph<TVertex, TEdge> visitedGraph,
            IList<TVertex> vertices,
            TopologicalSortDirection direction)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(vertices != null);
#endif

            var topo = new SourceFirstBidirectionalTopologicalSortAlgorithm<TVertex, TEdge>(visitedGraph, direction);
            topo.Compute(vertices);
        }

        public static void SourceFirstBidirectionalTopologicalSort<TVertex, TEdge>(
            this IBidirectionalGraph<TVertex, TEdge> visitedGraph,
            IList<TVertex> vertices)
            where TEdge : IEdge<TVertex>
        {
            SourceFirstBidirectionalTopologicalSort(visitedGraph, vertices, TopologicalSortDirection.Forward);
        }

        /// <summary>
        /// Computes the connected components of a graph
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="g"></param>
        /// <param name="components"></param>
        /// <returns>number of components</returns>
        public static int ConnectedComponents<TVertex, TEdge>(
            this IUndirectedGraph<TVertex, TEdge> g,
            IDictionary<TVertex, int> components)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
            Contract.Requires(components != null);
#endif

            var conn = new ConnectedComponentsAlgorithm<TVertex, TEdge>(g, components);
            conn.Compute();
            return conn.ComponentCount;
        }

        /// <summary>
        /// Computes the incremental connected components for a growing graph (edge added only).
        /// Each call to the delegate re-computes the component dictionary. The returned dictionary
        /// is shared accross multiple calls of the method.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="g"></param>
        /// <returns></returns>
        public static Func<KeyValuePair<int, IDictionary<TVertex, int>>> IncrementalConnectedComponents<TVertex, TEdge>(
            this IMutableVertexAndEdgeSet<TVertex, TEdge> g)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
#endif

            var incrementalComponents = new IncrementalConnectedComponentsAlgorithm<TVertex, TEdge>(g);
            incrementalComponents.Compute();

            return () => incrementalComponents.GetComponents();
        }

        /// <summary>
        /// Computes the weakly connected components of a graph
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="g"></param>
        /// <param name="components"></param>
        /// <returns>number of components</returns>
        public static int WeaklyConnectedComponents<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> g,
            IDictionary<TVertex, int> components)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
            Contract.Requires(components != null);
#endif

            var conn = new WeaklyConnectedComponentsAlgorithm<TVertex, TEdge>(g, components);
            conn.Compute();
            return conn.ComponentCount;
        }

        /// <summary>
        /// Computes the strongly connected components of a graph
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="g"></param>
        /// <param name="components"></param>
        /// <returns>number of components</returns>
        public static int StronglyConnectedComponents<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> g,
            out IDictionary<TVertex, int> components)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
            Contract.Ensures(Contract.ValueAtReturn(out components) != null);
#endif

            components = new Dictionary<TVertex, int>();
            var conn = new StronglyConnectedComponentsAlgorithm<TVertex, TEdge>(g, components);
            conn.Compute();
            return conn.ComponentCount;
        }

        /// <summary>
        /// Clones a graph to another graph
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="g"></param>
        /// <param name="vertexCloner"></param>
        /// <param name="edgeCloner"></param>
        /// <param name="clone"></param>
        public static void Clone<TVertex, TEdge>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> g,
            Func<TVertex, TVertex> vertexCloner,
            Func<TEdge, TVertex, TVertex, TEdge> edgeCloner,
            IMutableVertexAndEdgeSet<TVertex, TEdge> clone)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
            Contract.Requires(vertexCloner != null);
            Contract.Requires(edgeCloner != null);
            Contract.Requires(clone != null);
#endif

            var vertexClones = new Dictionary<TVertex, TVertex>(g.VertexCount);
            foreach (var v in g.Vertices)
            {
                var vc = vertexCloner(v);
                clone.AddVertex(vc);
                vertexClones.Add(v, vc);
            }

            foreach (var edge in g.Edges)
            {
                var ec = edgeCloner(
                    edge,
                    vertexClones[edge.Source],
                    vertexClones[edge.Target]);
                clone.AddEdge(ec);
            }
        }

        /// <summary>
        /// Condensates the strongly connected components of a directed graph
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <typeparam name="TGraph"></typeparam>
        /// <param name="g"></param>
        /// <returns></returns>
        public static IMutableBidirectionalGraph<TGraph, CondensedEdge<TVertex, TEdge, TGraph>> CondensateStronglyConnected<TVertex, TEdge, TGraph>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> g)
            where TEdge : IEdge<TVertex>
            where TGraph : IMutableVertexAndEdgeSet<TVertex, TEdge>, new()
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
#endif

            var condensator = new CondensationGraphAlgorithm<TVertex, TEdge, TGraph>(g);
            condensator.Compute();
            return condensator.CondensedGraph;
        }

        /// <summary>
        /// Condensates the weakly connected components of a graph
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <typeparam name="TGraph"></typeparam>
        /// <param name="g"></param>
        /// <returns></returns>
        public static IMutableBidirectionalGraph<TGraph, CondensedEdge<TVertex, TEdge, TGraph>> CondensateWeaklyConnected<TVertex, TEdge, TGraph>(
            this IVertexAndEdgeListGraph<TVertex, TEdge> g)
            where TEdge : IEdge<TVertex>
            where TGraph : IMutableVertexAndEdgeSet<TVertex, TEdge>, new()
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
#endif

            var condensator = new CondensationGraphAlgorithm<TVertex, TEdge, TGraph>(g);
            condensator.StronglyConnected = false;
            condensator.Compute();
            return condensator.CondensedGraph;
        }

        public static IMutableBidirectionalGraph<TVertex, MergedEdge<TVertex, TEdge>> CondensateEdges<TVertex, TEdge>(
            this IBidirectionalGraph<TVertex, TEdge> visitedGraph, 
            VertexPredicate<TVertex> vertexPredicate) 
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(vertexPredicate != null);
#endif

            var condensated = new BidirectionalGraph<TVertex, MergedEdge<TVertex, TEdge>>();
            var condensator = new EdgeMergeCondensationGraphAlgorithm<TVertex, TEdge>(
                visitedGraph,
                condensated,
                vertexPredicate);
            condensator.Compute();

            return condensated;
        }


        /// <summary>
        /// Create a collection of odd vertices
        /// </summary>
        /// <param name="g">graph to visit</param>
        /// <returns>collection of odd vertices</returns>
        /// <exception cref="ArgumentNullException">g is a null reference</exception>
        public static List<TVertex> OddVertices<TVertex, TEdge>(this IVertexAndEdgeListGraph<TVertex, TEdge> g)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
#endif

            var counts = new Dictionary<TVertex, int>(g.VertexCount);
            foreach (var v in g.Vertices)
                counts.Add(v, 0);

            foreach (var e in g.Edges)
            {
                ++counts[e.Source];
                --counts[e.Target];
            }

            var odds = new List<TVertex>();
            foreach (var de in counts)
            {
                if (de.Value % 2 != 0)
                    odds.Add(de.Key);
            }

            return odds;
        }

        /// <summary>
        /// Gets a value indicating whether the graph is acyclic
        /// </summary>
        /// <remarks>
        /// Performs a depth first search to look for cycles.
        /// </remarks>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="g"></param>
        /// <returns></returns>
        public static bool IsDirectedAcyclicGraph<TVertex, TEdge>(this IVertexListGraph<TVertex, TEdge> g)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(g != null);
#endif

            return new DagTester<TVertex, TEdge>().IsDag(g);
        }

        class DagTester<TVertex, TEdge>
            where TEdge : IEdge<TVertex>
        {
            private bool isDag = true;

            public bool IsDag(IVertexListGraph<TVertex, TEdge> g)
            {
                var dfs = new DepthFirstSearchAlgorithm<TVertex, TEdge>(g);
                try
                {
                    dfs.BackEdge += dfs_BackEdge;
                    isDag = true;
                    dfs.Compute();
                    return isDag;
                }
                finally
                {
                    dfs.BackEdge -= dfs_BackEdge;
                }
            }

            void dfs_BackEdge(TEdge e)
            {
                isDag = false;
            }
        }

        /// <summary>
        /// Given a edge cost map, computes 
        /// the predecessor cost.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="predecessors"></param>
        /// <param name="edgeCosts"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static double ComputePredecessorCost<TVertex, TEdge>(
            IDictionary<TVertex, TEdge> predecessors,
            IDictionary<TEdge, double> edgeCosts,
            TVertex target)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(predecessors != null);
            Contract.Requires(edgeCosts != null);
#endif

            double cost = 0;
            TVertex current = target;
            TEdge edge;

            while (predecessors.TryGetValue(current, out edge))
            {
                cost += edgeCosts[edge];
                current = edge.Source;
            }

            return cost;
        }

        public static IDisjointSet<TVertex> ComputeDisjointSet<TVertex, TEdge>(
            this IUndirectedGraph<TVertex, TEdge> visitedGraph)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
#endif

            var ds = new ForestDisjointSet<TVertex>(visitedGraph.VertexCount);
            foreach (var v in visitedGraph.Vertices)
                ds.MakeSet(v);
            foreach (var e in visitedGraph.Edges)
                ds.Union(e.Source, e.Target);

            return ds;
        }

        /// <summary>
        /// Computes the minimum spanning tree using Prim's algorithm.
        /// Prim's algorithm is simply implemented by calling Dijkstra shortest path.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static IEnumerable<TEdge> MinimumSpanningTreePrim<TVertex, TEdge>(
            this IUndirectedGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> weights)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(weights != null);
#endif

            if (visitedGraph.VertexCount == 0)
                return new TEdge[0];

            var distanceRelaxer = PrimRelaxer.Instance;
            var dijkstra = new UndirectedDijkstraShortestPathAlgorithm<TVertex, TEdge>(visitedGraph, weights, distanceRelaxer);
            var edgeRecorder = new UndirectedVertexPredecessorRecorderObserver<TVertex, TEdge>();
            using (edgeRecorder.Attach(dijkstra))
                dijkstra.Compute();

            return edgeRecorder.VertexPredecessors.Values;
        }

        class PrimRelaxer : IDistanceRelaxer
        {
            public static readonly IDistanceRelaxer Instance = new PrimRelaxer();

            public double InitialDistance
            {
                get { return double.MaxValue; }
            }

            public int Compare(double a, double b)
            {
                return a.CompareTo(b);
            }

            public double Combine(double distance, double weight)
            {
                return weight;
            }
        }

        /// <summary>
        /// Computes the minimum spanning tree using Kruskal's algorithm.
        /// </summary>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static IEnumerable<TEdge> MinimumSpanningTreeKruskal<TVertex, TEdge>(
            this IUndirectedGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> weights)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(weights != null);
#endif

            if (visitedGraph.VertexCount == 0)
                return new TEdge[0];

            var kruskal = new KruskalMinimumSpanningTreeAlgorithm<TVertex, TEdge>(visitedGraph, weights);
            var edgeRecorder = new EdgeRecorderObserver<TVertex, TEdge>();
            using (edgeRecorder.Attach(kruskal))
                kruskal.Compute();

            return edgeRecorder.Edges;
        }

        /// <summary>
        /// Computes the offline least common ancestor between pairs of vertices in a rooted tree
        /// using Tarjan algorithm.
        /// </summary>
        /// <remarks>
        /// Reference:
        /// Gabow, H. N. and Tarjan, R. E. 1983. A linear-time algorithm for a special case of disjoint set union. In Proceedings of the Fifteenth Annual ACM Symposium on theory of Computing STOC '83. ACM, New York, NY, 246-251. DOI= http://doi.acm.org/10.1145/800061.808753 
        /// </remarks>
        /// <typeparam name="TVertex">type of the vertices</typeparam>
        /// <typeparam name="TEdge">type of the edges</typeparam>
        /// <param name="visitedGraph"></param>
        /// <param name="root"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public static TryFunc<SEquatableEdge<TVertex>, TVertex> OfflineLeastCommonAncestorTarjan<TVertex, TEdge>(
            this IVertexListGraph<TVertex, TEdge> visitedGraph,
            TVertex root,
            IEnumerable<SEquatableEdge<TVertex>> pairs)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(root != null);
            Contract.Requires(pairs != null);
            Contract.Requires(visitedGraph.ContainsVertex(root));
            Contract.Requires(Enumerable.All(pairs, p => visitedGraph.ContainsVertex(p.Source)));
            Contract.Requires(Enumerable.All(pairs, p => visitedGraph.ContainsVertex(p.Target)));
#endif

            var algo = new TarjanOfflineLeastCommonAncestorAlgorithm<TVertex, TEdge>(visitedGraph);
            algo.Compute(root, pairs);
            var ancestors = algo.Ancestors;

            return delegate (SEquatableEdge<TVertex> pair, out TVertex value)
            {
                return ancestors.TryGetValue(pair, out value);
            };
        }

        /// <summary>
        /// Computes the Edmonds-Karp maximums flow for a graph with positive capacities and flows.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertex.</typeparam>
        /// <typeparam name="TEdge">The type of the edge.</typeparam>
        /// <param name="visitedGraph">The visited graph.</param>
        /// <param name="edgeCapacities">The edge capacity delegate.</param>
        /// <param name="source">The source.</param>
        /// <param name="sink">The sink.</param>
        /// <param name="flowPredecessors">The flow predecessors.</param>
        /// <returns>The maximum flow.</returns>
        /// <remarks>
        /// Will throw an exception in <see cref="ReversedEdgeAugmentorAlgorithm{TVertex,TEdge}.AddReversedEdges"/> if TEdge is a value type,
        /// e.g. <see cref="SEdge{TVertex}"/>.
        /// <seealso href="https://github.com/YaccConstructor/QuickGraph/issues/183#issue-377613647"/>.
        /// </remarks>
        public static double MaximumFlowEdmondsKarp<TVertex, TEdge>(
            this IMutableVertexAndEdgeListGraph<TVertex, TEdge> visitedGraph,
            Func<TEdge, double> edgeCapacities,
            TVertex source,
            TVertex sink,
            out TryFunc<TVertex, TEdge> flowPredecessors,
            EdgeFactory<TVertex, TEdge> edgeFactory,
            ReversedEdgeAugmentorAlgorithm<TVertex, TEdge> reversedEdgeAugmentorAlgorithm)
            where TEdge : IEdge<TVertex>
        {
#if SUPPORTS_CONTRACTS
            Contract.Requires(visitedGraph != null);
            Contract.Requires(edgeCapacities != null);
            Contract.Requires(source != null);
            Contract.Requires(sink != null);
            Contract.Requires(!source.Equals(sink));
#endif

            // compute maxflow
            var flow = new EdmondsKarpMaximumFlowAlgorithm<TVertex, TEdge>(
                visitedGraph,
                edgeCapacities,
                edgeFactory,
                reversedEdgeAugmentorAlgorithm
                );
            flow.Compute(source, sink);
            flowPredecessors = flow.Predecessors.TryGetValue;
            return flow.MaxFlow;
        }


        /*
         * Code by Yoad Snapir <yoadsn@gmail.com>
         * Taken from https://github.com/yoadsn/ArrowDiagramGenerator because PR was not opened
         * 
         * */

        public static BidirectionalGraph<TVertex, TEdge> ComputeTransitiveReduction<TVertex, TEdge>(
            this BidirectionalGraph<TVertex, TEdge> visitedGraph
            ) where TEdge : IEdge<TVertex>
        {
            var algo = new TransitiveReductionAlgorithm<TVertex, TEdge>(visitedGraph);
            algo.Compute();
            return algo.TransitiveReduction;
        }

        public static BidirectionalGraph<TVertex, TEdge> ComputeTransitiveClosure<TVertex, TEdge>(
            this BidirectionalGraph<TVertex, TEdge> visitedGraph,
            Func<TVertex, TVertex, TEdge> createEdge
            ) where TEdge : IEdge<TVertex>
        {
            var algo = new TransitiveClosureAlgorithm<TVertex, TEdge>(visitedGraph, createEdge);
            algo.Compute();
            return algo.TransitiveClosure;
        }
    }
}
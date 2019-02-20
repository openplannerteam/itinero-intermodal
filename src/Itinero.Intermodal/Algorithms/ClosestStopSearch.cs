using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Itinero.Algorithms;
using Itinero.Algorithms.Default.EdgeBased;
using Itinero.Algorithms.Routes;
using Itinero.Algorithms.Weights;
using Itinero.Data.Network;
using Itinero.LocalGeo;
using Itinero.Profiles;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data;

namespace Itinero.Intermodal.Algorithms
{
    /// <summary>
    /// An algorithm to search for closest stops to a departure/arrival location.
    /// </summary>
    public class ClosestStopSearch : AlgorithmBase
    {
        private readonly RouterPoint _routerPoint;
        private readonly Profile _profile;
        private readonly float _max;
        private readonly bool _backward;
        private readonly Router _router;
        private readonly TransitDb _transitDb;
        
        public ClosestStopSearch(Router router, TransitDb transitDb, Profile profile, RouterPoint routerPoint,
            float max, bool backward = false)
        {
            _router = router;
            _transitDb = transitDb;
            _max = max;
            _profile = profile;
            _routerPoint = routerPoint;
            _backward = backward;
        }

        private HashSet<uint> _stopTiles;
        private Dictionary<uint, LinkedStopRouterPoint> _stopsPerEdge;
        private Dictionary<(uint tileId, uint localId), RouterPoint> _routerPointPerStop;
        private Dykstra _dykstra;
        private DefaultWeightHandler _weightHandler;
        private TransitDb.TransitDbSnapShot _snapshot;
        
        /// <inheritdoc/>
        protected override void DoRun(CancellationToken cancellationToken)
        {
            _stopTiles = new HashSet<uint>();
            _stopsPerEdge = new Dictionary<uint, LinkedStopRouterPoint>();
            _routerPointPerStop = new Dictionary<(uint tileId, uint localId), RouterPoint>();
            _weightHandler = _router.GetDefaultWeightHandler(_profile);
            _snapshot = _transitDb.Latest;
            
            // resolve stops around source location.
            LoadStopsAround(_router, _profile, _snapshot, _routerPoint.Longitude, _routerPoint.Latitude);
            
            // build search.
            _dykstra = new Dykstra(_router.Db.Network.GeometricGraph.Graph, _weightHandler, null, 
                _routerPoint.ToEdgePaths(_router.Db, _weightHandler, !_backward), _max, _backward);
            _dykstra.Visit = this.VisitInternal;
            //_dykstra. = this.WasEdgeFoundInternal;
            _dykstra.Run(cancellationToken);

//            var vertices = Enumerable.Range(0, (int)_router.Db.Network.VertexCount)
//                .Where(v => _dykstra.TryGetVisit((uint)v, out _)).Select(v => (uint)v);
//            var json = _router.Db.GetGeoJsonVertices(false, vertices);

            this.HasSucceeded = true;
        }

        private void LoadStopsAround(Router router, Profile profile, TransitDb.TransitDbSnapShot snapshot, double longitude, double latitude)
        {
            var tile = Tile.WorldToTile(longitude,latitude, 14);
            if (_stopTiles.Contains(tile.LocalId)) return;
            _stopTiles.Add(tile.LocalId);
            
            var stops = snapshot.StopsDb.SearchInBox((tile.Left, tile.Bottom, tile.Right, tile.Top));
            foreach (var stop in stops)
            {
                var resolvedResult = router.TryResolve(profile, new Coordinate((float)stop.Latitude, (float)stop.Longitude));
                if (resolvedResult.IsError) continue;

                var resolved = resolvedResult.Value;
                _routerPointPerStop[stop.Id] = resolved;
                var linkedStopRouterPoint = new LinkedStopRouterPoint()
                {
                    RouterPoint = resolved,
                    StopId = stop.Id
                };
                if (!_stopsPerEdge.TryGetValue(resolved.EdgeId, out var value))
                {
                    _stopsPerEdge[resolved.EdgeId] = linkedStopRouterPoint;
                }
                else
                {
                    value.Next = linkedStopRouterPoint;
                }

                var json = resolved.ToGeoJson(_router.Db);

//                var edge = _router.Db.Network.GetEdge(resolved.EdgeId);
//                if (_dykstra.TryGetVisit(edge.From, out var visit))
//                {
//                    Console.WriteLine(string.Empty);
//                }
//                if (_dykstra.TryGetVisit(edge.To, out visit))
//                {
//                    Console.WriteLine(string.Empty);
//                }
            }
        }

        private bool VisitInternal(EdgePath<float> path)
        {
            var vertex = path.Vertex;
            
            // make sure stops are loaded.
            var locations = _router.Db.Network.GetVertex(vertex);
            LoadStopsAround(_router, _profile, _snapshot, locations.Longitude, locations.Latitude);

            var visit = path;
//            if (!_dykstra.TryGetVisit(path.Edge, out var visit))
//            { // could not get visit, can't act on it.
//                return false;
//            }
            if (visit.Edge == Itinero.Constants.NO_EDGE)
            { // no edge information.
                return false;
            }

            var edgeId = Itinero.Constants.NO_EDGE;
            if (visit.Edge > 0)
            {
                edgeId = (uint)(visit.Edge - 1);
            }
            else
            {
                edgeId = (uint)(-visit.Edge - 1);
            }

            var edge = _router.Db.Network.GetEdge(visit.Edge);
            var edgeJson = edge.ToGeoJson(_router.Db);
            var vertex1 = edge.From;
            if (visit.Edge < 0)
            {
                vertex1 = edge.To;
            }

            if (!_stopsPerEdge.TryGetValue(edgeId, out var stopRouterPoint)) return false; // ok, there is a link, get the router points and calculate weights.
            
            if (_dykstra == null)
            { // no dykstra search just yet, this is the source-edge.
                throw new Exception("Could not get visit of other vertex for settled edge.");
            }

            var vertex1Visit = visit.From;
//            if (!_dykstra.TryGetVisit(vertex1, out var vertex1Visit))
//            {
//                throw new Exception("Could not get visit of other vertex for settled edge.");
//            }

            // move the stop links enumerator.
            while (stopRouterPoint != null)
            {
                var stopId = stopRouterPoint.StopId;
                var routerPoint = stopRouterPoint.RouterPoint;

                var paths = routerPoint.ToEdgePaths(_router.Db, _weightHandler, _backward);
                if (paths[0].Vertex == vertex1)
                { // report on the time.
                    if (this.StopFound !=null &&
                        this.StopFound(stopId, vertex1Visit.Weight + paths[0].Weight))
                    {
                        return false;
                    }
                }
                else if (paths.Length > 1 && paths[1].Vertex == vertex1)
                { // report on the time.
                    if (this.StopFound != null &&
                        this.StopFound(stopId, vertex1Visit.Weight + paths[1].Weight))
                    {
                        return false;
                    }
                }

                stopRouterPoint = stopRouterPoint.Next;
            }
            return false;
        }

        /// <summary>
        /// A function to report that a stop was found and the number of seconds to travel to/from.
        /// </summary>
        public delegate bool StopFoundFunc((uint tileId, uint localId) stop, float time);

        /// <summary>
        /// Gets or sets the stop found function.
        /// </summary>
        public virtual StopFoundFunc StopFound
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the path to the given stop.
        /// </summary>
        public (EdgePath<float> path, RouterPoint point) GetPath((uint tileId, uint localId) stop)
        {
            if (!_routerPointPerStop.TryGetValue(stop, out var point)) return (null, null);
            
            EdgePath<float> best = null;
            var bestWeight = float.MaxValue;
            if (point.EdgeId == _routerPoint.EdgeId)
            { // on the same edge.
                EdgePath<float> path;
                if (_backward)
                { // from stop -> source.
                    path = point.EdgePathTo(_router.Db, _weightHandler, _routerPoint);
                }
                else
                { // from source -> stop.
                    path = _routerPoint.EdgePathTo(_router.Db, _weightHandler, point);
                }
                if (path.Weight < bestWeight)
                { // set as best because improvement.
                    best = path;
                    bestWeight = path.Weight;
                }
            }
            else
            { // on different edge, to the usual.
                var paths = point.ToEdgePaths(_router.Db, _weightHandler, _backward);
                if (_dykstra.TryGetVisit(paths[0].Edge, out var visit))
                { // check if this one is better.
                    if (visit.Weight + paths[0].Weight < bestWeight)
                    { // concatenate paths and set best.
                        if (paths[0].Weight == 0)
                        { // just use the visit.
                            best = visit;
                        }
                        else
                        { // there is a distance/weight.
                            best = new EdgePath<float>(Itinero.Constants.NO_VERTEX,
                                paths[0].Weight + visit.Weight, visit);
                        }
                        bestWeight = best.Weight;
                    }
                }
                if (paths.Length > 1 && _dykstra.TryGetVisit(paths[1].Edge, out visit))
                { // check if this one is better.
                    if (visit.Weight + paths[1].Weight < bestWeight)
                    { // concatenate paths and set best.
                        if (paths[1].Weight == 0)
                        { // just use the visit.
                            best = visit;
                        }
                        else
                        { // there is a distance/weight.
                            best = new EdgePath<float>(Itinero.Constants.NO_VERTEX,
                                paths[1].Weight + visit.Weight, visit);
                        }
                        bestWeight = best.Weight;
                    }
                }
            }
            return (best, point);
        }

        /// <summary>
        /// Gets the route to the given stop.
        /// </summary>
        public Route GetRoute((uint tileId, uint localId) stop)
        {
            var (path, point) = this.GetPath(stop);

            if (_backward)
            {
                var reverse = new EdgePath<float>(path.Vertex);
                path = reverse.Append(path);
                return CompleteRouteBuilder.Build(_router.Db, _profile, point, _routerPoint, path);
            }
            return CompleteRouteBuilder.Build(_router.Db, _profile, _routerPoint, point, path);
        }

        /// <summary>
        /// A linked stop router point.
        /// </summary>
        private class LinkedStopRouterPoint
        {
            /// <summary>
            /// Gets or sets the router point.
            /// </summary>
            public RouterPoint RouterPoint { get; set; }

            /// <summary>
            /// Gets or sets the stop id.
            /// </summary>
            public (uint tileId, uint localId) StopId { get; set; }

            /// <summary>
            /// Gets or sets the next link.
            /// </summary>
            public LinkedStopRouterPoint Next { get; set; }
        }
    }
}
using System;
using Itinero.LocalGeo;
using Itinero.Profiles;
using Itinero.Transit.Data;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Intermodal.Algorithms
{
    public static class IntermodalCSA
    {
        public static Result<Route> TryCalculateIntermodal(this Router router, TransitDb db, Profile profile,
            Coordinate source, Coordinate target)
        {
            var sourceResolved = router.Resolve(Itinero.Osm.Vehicles.Vehicle.Pedestrian.Fastest(),
                source.Latitude, source.Longitude);
            var targetResolved = router.ResolveConnected(Itinero.Osm.Vehicles.Vehicle.Pedestrian.Fastest(),
                target.Latitude, target.Longitude);

            return router.TryCalculateIntermodal(db, profile, sourceResolved, targetResolved);
        }

        public static Result<Route> TryCalculateIntermodal(this Router router, TransitDb db, Profile profile,
            RouterPoint source, RouterPoint target)
        {
            // find closest stops to source.
            var sources = new List<(uint tileId, uint localId, ulong time)>();
            var sourceStopSearch = new ClosestStopSearch(router, db, profile,
                source, 3600);
            sourceStopSearch.StopFound = (stop, time) =>
            {
                if (sources.Any(t => (t.tileId, t.localId) == stop))
                {
                    return true;
                }
                sources.Add((stop.tileId, stop.localId, (ulong)time));
                return true;
            };
            sourceStopSearch.Run();
            if (sources.Count == 0) return new Result<Route>("No source stop found nearby.");
            
            // find closest stops to targets.
            var targets = new List<(uint tileId, uint localId, ulong time)>();
            var targetStopSearch = new ClosestStopSearch(router, db, profile,
                target, 3600, true);
            targetStopSearch.StopFound = (stop, time) =>
            {
                if (targets.Any(t => (t.tileId, t.localId) == stop))
                {
                    return true;
                }
                targets.Add((stop.tileId, stop.localId, (ulong)time));
                return true;
            };
            targetStopSearch.Run();
            if (targets.Count == 0) return new Result<Route>("No target stop found nearby.");
            
            // find best connection between source and target stops.
            var latest = db.Latest;
            
            // calculate journey.
            var p = new Profile<TransferStats>(
                latest,
                new InternalTransferGenerator(),
                new BirdsEyeInterWalkTransferGenerator(latest.StopsDb.GetReader()),
                new TransferStats(), TransferStats.ProfileTransferCompare);
            
            // instantiate and run EAS.
            var depTime = DateTime.Now;
            var eas = new EarliestConnectionScan<TransferStats>(
                sources, targets,
                depTime.ToUnixTime(), depTime.AddHours(24).ToUnixTime(), p);
            var journey = eas.CalculateJourney();
            var journeyRoute = journey.ToRoute(latest);

            var sourceRoute = sourceStopSearch.GetRoute((sources[0].tileId, sources[0].localId));
            var targetRoute = targetStopSearch.GetRoute((targets[0].tileId, targets[0].localId));

            var route = sourceRoute.Concatenate(journeyRoute);
            route = route.Concatenate(targetRoute);
            
            return new Result<Route>(route);
        }
    }
}
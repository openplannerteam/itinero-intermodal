using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Itinero.Algorithms.Networks;
using Itinero.Algorithms.Search.Hilbert;
using Itinero.Attributes;
using Itinero.Intermodal.Test.Functional.Temp;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Streams;
using Itinero.LocalGeo;
using Itinero.Profiles;
using NetTopologySuite.Features;
using OsmSharp.Streams;
using OsmSharp.Streams.Filters;
using Serilog;
using Vehicle = Itinero.Osm.Vehicles.Vehicle;

namespace Itinero.Intermodal.Test.Functional.Staging
{
    /// <summary>
    /// Builds test routerdb's.
    /// </summary>
    public static class BuildRouterDb
    {
        /// <summary>
        /// The local path of the routerdb.
        /// </summary>
        public static string LocalBelgiumRouterDb = "belgium.routerdb";
        
        /// <summary>
        /// Builds or loads a routerdb.
        /// </summary>
        /// <returns>The loaded routerdb.</returns>
        public static RouterDb BuildOrLoad()
        {
            RouterDb routerDb = null;
            try
            {
                if (File.Exists(LocalBelgiumRouterDb))
                {
                    using (var stream = File.OpenRead(LocalBelgiumRouterDb))
                    {
                        routerDb = RouterDb.Deserialize(stream);
                    }

                    if (routerDb != null && 
                        routerDb.AddVelo())
                    {
                        using (var outputStream = File.Open(LocalBelgiumRouterDb, FileMode.Create))
                        {
                            routerDb.Serialize(outputStream);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Existing RouterDb failed to load.", e);
            }

            if (routerDb != null) return routerDb;
            
            Download.DownloadBelgiumAll();

            var pedestrian = Itinero.Profiles.DynamicVehicle.Load(File.ReadAllText("./data/pedestrian.lua"));
            
            Log.Information("RouterDb doesn't exist yet or failed to load, building...");
            routerDb = new RouterDb();
            using (var stream = File.OpenRead(Download.BelgiumLocal))
            using (var outputStream = File.Open(LocalBelgiumRouterDb, FileMode.Create))
            {
                var source = new PBFOsmStreamSource(stream);
                routerDb.LoadOsmDataAndShortcuts(source, new LoadSettings(), Vehicle.Bicycle, pedestrian);
                
                routerDb.Serialize(outputStream);
            }

            return routerDb;
        }

        private static bool AddVelo(this RouterDb routerDb)
        {
            if (routerDb.TryGetShortcuts("velo", out _)) return false;
            
            var veloFeatures =
                (new NetTopologySuite.IO.GeoJsonReader()).Read<FeatureCollection>(
                    File.ReadAllText("./data/velo.geojson"));
            var veloLocations = new List<Coordinate>();
            var veloLocationsAttributes = new List<IAttributeCollection>();
            foreach (var feature in veloFeatures.Features)
            {
                if (!(feature.Geometry is NetTopologySuite.Geometries.Point p)) continue;

                var attributeCollection = new AttributeCollection();

                veloLocations.Add(new Coordinate((float) p.Coordinate.Y, (float) p.Coordinate.X));
                veloLocationsAttributes.Add(attributeCollection);
            }

            var veloSpecs = new ShortcutSpecs()
            {
                Profile = routerDb.GetSupportedProfile("bicycle"),
                Locations = veloLocations.ToArray(),
                LocationsMeta = veloLocationsAttributes.ToArray(),
                Name = "velo",
                MaxShortcutDuration = 3600,
                MinTravelTime = 120,
                TransferTime = 60,
                TransitionProfiles = new Profile[]
                {
                    routerDb.GetSupportedProfile("bicycle"),
                    routerDb.GetSupportedProfile("pedestrian"),
                    routerDb.GetSupportedProfile("pedestrian.shortcut")
                }
            };
            
            
            var villoFeatures =
                (new NetTopologySuite.IO.GeoJsonReader()).Read<FeatureCollection>(
                    File.ReadAllText("./data/villo.geojson"));
            var villoLocations = new List<Coordinate>();
            var villoLocationsAttributes = new List<IAttributeCollection>();
            foreach (var feature in veloFeatures.Features)
            {
                if (!(feature.Geometry is NetTopologySuite.Geometries.Point p)) continue;

                var attributeCollection = new AttributeCollection();

                villoLocations.Add(new Coordinate((float) p.Coordinate.Y, (float) p.Coordinate.X));
                villoLocationsAttributes.Add(attributeCollection);
            }

            var villoSpecs = new ShortcutSpecs()
            {
                Profile = routerDb.GetSupportedProfile("bicycle"),
                Locations = villoLocations.ToArray(),
                LocationsMeta = villoLocationsAttributes.ToArray(),
                Name = "villo",
                MaxShortcutDuration = 3600,
                MinTravelTime = 120,
                TransferTime = 60,
                TransitionProfiles = new Profile[]
                {
                    routerDb.GetSupportedProfile("bicycle"),
                    routerDb.GetSupportedProfile("pedestrian"),
                    routerDb.GetSupportedProfile("pedestrian.shortcut")
                }
            };

            routerDb.AddShortcuts1(veloSpecs, villoSpecs);
            return true;
        }

        /// <summary>
        /// Loads a routing network created from OSM data.
        /// </summary>
        public static void LoadOsmDataAndShortcuts(this RouterDb db, OsmStreamSource source, LoadSettings settings,
            params Itinero.Profiles.Vehicle[] vehicles)
        {
            if (!db.IsEmpty)
            {
                throw new ArgumentException(
                    "Can only load a new routing network into an empty router db, add multiple streams at once to load multiple files.");
            }

            if (vehicles == null || vehicles.Length == 0)
            {
                throw new ArgumentNullException("vehicles", "A least one vehicle is needed to load OSM data.");
            }

            if (settings == null)
            {
                settings = new LoadSettings();
            }

            // merge sources if needed.
            var progress = new OsmStreamFilterProgress();
            progress.RegisterSource(source);
            source = progress;

            // make sure the routerdb can handle multiple edges.
            db.Network.GeometricGraph.Graph.MarkAsMulti();

            // load the data.
            var target = new RouterDbStreamTarget(db,
                vehicles, settings.AllCore, processRestrictions: settings.ProcessRestrictions,
                processors: settings.Processors,
                simplifyEpsilonInMeter: settings.NetworkSimplificationEpsilon);
            target.KeepNodeIds = settings.KeepNodeIds;
            target.KeepWayIds = settings.KeepWayIds;
            target.RegisterSource(source);
            target.Pull();

            foreach (var profile in db.GetSupportedProfiles())
            {
                db.AddIslandData(profile);
            }

            foreach (var vehicleType in db.GetRestrictedVehicleTypes().ToList())
            {
                db.RemoveRestrictions(vehicleType);
            }
            
            db.Sort();

//            // optimize the network.
            db.RemoveDuplicateEdges();
//            db.SplitLongEdges();
            db.ConvertToSimple();

            AddVelo(db);

//            // optimize the network if requested.
//            if (settings.NetworkSimplificationEpsilon > 0)
//            {
//                db.OptimizeNetwork(settings.NetworkSimplificationEpsilon);
//            }

//            // compress the network.
//            db.Compress();
        }
    }
}
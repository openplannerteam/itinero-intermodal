using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Intermodal.Algorithms;
using Itinero.Intermodal.Test.Functional.Staging;
using Itinero.Intermodal.Test.Functional.Temp;
using Itinero.LocalGeo;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;
using Serilog;
using Serilog.Events;
using Log = Serilog.Log;

namespace Itinero.Intermodal.Test.Functional
{
    class Program
    {
        private const string Gent = "http://irail.be/stations/NMBS/008892007";
        private const string Brugge = "http://irail.be/stations/NMBS/008891009";
        private const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        private const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        private const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        private const string Kortrijk = "http://irail.be/stations/NMBS/008896008";
        
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            
            
#if DEBUG
            var loggingBlacklist = new HashSet<string>();
#else
            var loggingBlacklist = new HashSet<string>();
#endif
            Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(o))
                {
                    message = $"[{o}] {message}";
                }

                if (level == Logging.TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug(message);
                }
                else if (level == Logging.TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information(message);
                }
                else if (level == Logging.TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning(message);
                }
                else if (level == Logging.TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal(message);
                }
                else if (level == Logging.TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error(message);
                }
                else
                {
                    Log.Debug(message);
                }
            };
            
            // load data.
            var routerDb = BuildRouterDb.BuildOrLoad();
            
            // load transit db.
            var transitDb = BuildTransitDb.BuildOrLoad();

            var router = new Router(routerDb)
            {
                VerifyAllStoppable = true, 
                CustomRouteBuilder = new Temp.Temp()
            };

            var antwerpen1 = new Coordinate(51.21880619138497f, 4.397792816162109f);
            var antwerpen2 = new Coordinate(51.21888683113129f, 4.432253837585449f);
            var brusselHermanTeir = new Coordinate(50.865696744357294f, 4.3497008085250854f);
            var brusselCentraal = new Coordinate(50.83144119255431f, 4.339964389801025f);
            var lille = new Coordinate(51.25979327802935f, 4.875869750976562f);
            var turnhout = new Coordinate(51.3202332109125f, 4.9339234828948975f);
            var tourEnTaxis = new Coordinate(50.86439661723841f, 4.348719120025635f);
            var marcheEnFamenne = new Coordinate(50.23142236000259f, 5.333776473999023f);
            var ieper = new Coordinate(50.85532180383167f, 2.860565185546875f);
//
//            var route = router.Calculate(router.Db.GetSupportedProfile("pedestrian.shortcut"),
//                antwerpen1, antwerpen2);
//            File.WriteAllText("route-antwerpen.json", route.ToGeoJson());
//
//            route = router.Calculate(router.Db.GetSupportedProfile("pedestrian.shortcut"),
//                brusselHermanTeir, brusselCentraal);
//            File.WriteAllText("route-brussel.json", route.ToGeoJson());
//            

            var sourceLocation = antwerpen2;
            var targetLocation = brusselHermanTeir;

            var routeResult = router.TryCalculateIntermodal(transitDb, router.Db.GetSupportedProfile("pedestrian"),
                sourceLocation, targetLocation);
            File.WriteAllText("intermodal-route1.json", routeResult.Value.ToGeoJson());

            routeResult = router.TryCalculateIntermodal(transitDb, router.Db.GetSupportedProfile("pedestrian"),
                antwerpen2, lille);
            File.WriteAllText("intermodal-route2.json", routeResult.Value.ToGeoJson());

            routeResult = router.TryCalculateIntermodal(transitDb, router.Db.GetSupportedProfile("pedestrian"),
                turnhout, lille);
            File.WriteAllText("intermodal-route3.json", routeResult.Value.ToGeoJson());

            routeResult = router.TryCalculateIntermodal(transitDb, router.Db.GetSupportedProfile("pedestrian"),
                turnhout, marcheEnFamenne);
            File.WriteAllText("intermodal-route4.json", routeResult.Value.ToGeoJson());

            routeResult = router.TryCalculateIntermodal(transitDb, router.Db.GetSupportedProfile("pedestrian"),
                turnhout, ieper);
            File.WriteAllText("intermodal-route5.json", routeResult.Value.ToGeoJson());
        }
    }
}

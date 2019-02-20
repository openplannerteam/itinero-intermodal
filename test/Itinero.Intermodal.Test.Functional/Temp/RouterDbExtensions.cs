using System;
using System.Collections.Generic;
using System.Threading;
using Itinero.Algorithms.Search.Hilbert;
using Itinero.Profiles;

namespace Itinero.Intermodal.Test.Functional.Temp
{
    /// <summary>
    /// Contains extensions for the router db related to shortcut db's.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Adds multiple shortcut dbs at the same time.
        /// </summary>
        public static void AddShortcuts1(this RouterDb routerDb, params ShortcutSpecs[] shortcutSpecs)
        {
            routerDb.AddShortcuts1(shortcutSpecs, CancellationToken.None);
        }

        /// <summary>
        /// Adds multiple shortcut dbs at the same time.
        /// </summary>
        public static void AddShortcuts1(this RouterDb routerDb, ShortcutSpecs[] shortcutSpecs, CancellationToken cancellationToken)
        {
            // check all specs.
            if (shortcutSpecs == null) { throw new ArgumentNullException(); }
            for(var i = 0; i < shortcutSpecs.Length; i++)
            {
                if (shortcutSpecs[i] == null ||
                    shortcutSpecs[i].Locations == null)
                {
                    throw new ArgumentException(string.Format("Shortcut specs at index {0} not set or locations not set.", 
                        i));
                }
                if (shortcutSpecs[i].LocationsMeta != null &&
                    shortcutSpecs[i].LocationsMeta.Length != shortcutSpecs[i].Locations.Length)
                {
                    throw new ArgumentException(string.Format("Shortcut specs at index {0} has a different dimensions for locations and meta-data.",
                        i));
                }
            }
            
            for(var i = 0; i < shortcutSpecs.Length; i++)
            {
                var specs = shortcutSpecs[i];

                var profiles = new List<Profile>();
                profiles.Add(specs.Profile);
                profiles.AddRange(specs.TransitionProfiles);

                var routerPointEmbedder = new Itinero.Algorithms.Networks.RouterPointEmbedder(routerDb, profiles.ToArray(), specs.Locations);
                routerPointEmbedder.Run(cancellationToken);
            }

            // sort the network.
            routerDb.Sort();

            for(var i = 0; i < shortcutSpecs.Length; i++)
            {
                var specs = shortcutSpecs[i];
                Itinero.Logging.Logger.Log("RouterDbExtensions", Logging.TraceEventType.Information,
                    "Building shortcuts for {0}...", specs.Name);

                var shortcutBuilder = new ShortcutBuilder(routerDb, specs.Profile, specs.Name, specs.Locations,
                    specs.LocationsMeta, specs.TransferTime, specs.MinTravelTime, specs.MaxShortcutDuration);
                shortcutBuilder.Run(cancellationToken);

                routerDb.AddShortcuts(specs.Name, shortcutBuilder.ShortcutsDb);
            }
        }
    }
}
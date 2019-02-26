using System;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;

namespace Itinero.Intermodal.API.Staging
{
    public static class BuildTransitDb
    {
        public static TransitDb BuildOrLoad()
        {
            // load data.
            TransitDb db = null;
            if (!File.Exists("data.transitdb"))
            {
                var profile = Belgium.Sncb();
                
                // create a stops db and connections db.
                db = new TransitDb();
                
                // load connections for the current day.
                var w = db.GetWriter();
                profile.AddAllLocationsTo(w, Serilog.Log.Warning);
                profile.AddAllConnectionsTo(w, DateTime.Now, DateTime.Now.AddHours(8),
                    Serilog.Log.Warning);
                w.Close();
            
                // store to disk.
                using (var stream = File.Open("data.transitdb", FileMode.Create))
                {
                    db.Latest.WriteTo(stream);
                }
            }
            else
            {
                using (var stream = File.OpenRead("data.transitdb"))
                {
                    db = TransitDb.ReadFrom(stream);
                }
            }
            return db;
        }
    }
}
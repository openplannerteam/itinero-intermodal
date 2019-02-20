using System.IO;
using System.Net;

namespace Itinero.Intermodal.Test.Functional.Staging
{
    /// <summary>
    /// Downloads all data needed for testing.
    /// </summary>
    public static class Download
    {
        private static string BelgiumPBF = "http://files.itinero.tech/data/OSM/planet/europe/belgium-latest.osm.pbf";
        public static string BelgiumLocal = "belgium-latest.osm.pbf";
        
        /// <summary>
        /// Downloads the luxembourg data.
        /// </summary>
        public static void DownloadBelgiumAll()
        {
            if (File.Exists(BelgiumLocal)) return;
            
            var client = new WebClient();
            client.DownloadFile(BelgiumPBF,
                BelgiumLocal);
        }
    }
}
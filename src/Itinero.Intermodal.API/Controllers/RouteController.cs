using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Intermodal.Algorithms;
using Itinero.LocalGeo;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Intermodal.API.Controllers
{
    [Route("[controller]")]
    public class RouteController : ControllerBase
    {
        /// <summary>
        /// Calculates a route on the network.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="location">The locations to route along.</param>
        /// <returns></returns>
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<string> Get(string profile, string[] location)
        {         
            // parse locations.
            var parsedLocations = new List<Coordinate>(location.Length);
            foreach (var l in location)
            {
                if (!ParsingHelpers.TryParse(l, out var parsedLocation))
                {
                    return new BadRequestResult();
                }

                parsedLocations.Add(parsedLocation);
            }
            if (parsedLocations.Count < 2) return NotFound();
            
            // get router and transitdb.
            var router = Startup.Router;
            var transitDb = Startup.TransitDb;

            // get profile.
            if (!router.Db.SupportProfile(profile)) return NotFound();
            var profileInstance = router.Db.GetSupportedProfile(profile);

            var route = router.TryCalculateIntermodal(transitDb, profileInstance,
                parsedLocations[0], parsedLocations[1]);
            if (route.IsError)
            {
                return NotFound();
            }
            return route.Value.ToGeoJson();
        }
    }
}
using System.Collections.Generic;
using Itinero.Attributes;
using Itinero.LocalGeo;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Intermodal
{
    public static class TransitDbExtensions
    {
        public static Route ToRoute(this Journey<TransferStats> journey, TransitDb.TransitDbSnapShot snapshot)
        {
            var stopsReader = snapshot.StopsDb.GetReader();
            
            var shape = new List<Coordinate>();
            var stops = new List<Route.Stop>();
            while (journey != null)
            {
                if (stopsReader.MoveTo(journey.Location))
                {
                    var attributes = new AttributeCollection();
                    if (stopsReader.Attributes != null)
                    {
                        foreach (var a in stopsReader.Attributes)
                        {
                            attributes.AddOrReplace(a.Key, a.Value);
                        }
                    }
                    attributes.AddOrReplace("id", stopsReader.GlobalId);
                    stops.Add(new Route.Stop()
                    {
                        Shape = shape.Count,
                        Attributes = attributes,
                        Coordinate = new Coordinate((float)stopsReader.Latitude, (float)stopsReader.Longitude)
                    });
                    shape.Add(new Coordinate((float)stopsReader.Latitude, (float)stopsReader.Longitude));  
                }

                journey = journey.PreviousLink;
            }
            
            shape.Reverse();
            stops.Reverse();
            
            return new Route()
            {
                Shape = shape.ToArray(),
                ShapeMeta = new []
                {
                    new Route.Meta()
                    {
                        Shape = 0
                    }, 
                    new Route.Meta()
                    {
                        Shape = shape.Count - 1
                    }
                },
                Stops = stops.ToArray()
            };
        }
    }
}
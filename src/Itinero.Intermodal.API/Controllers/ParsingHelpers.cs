using Itinero.LocalGeo;

namespace Itinero.Intermodal.API.Controllers
{
    public static class ParsingHelpers
    {
        public static bool TryParse(string coordinateString, out Coordinate location)
        {
            var locs = coordinateString.Split(',');
            if (locs.Length < 2)
            {
                // less than two loc parameters.
                location = default(Coordinate);
                return false;
            }

            if (!float.TryParse(locs[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon) ||
                !float.TryParse(locs[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat))
            {
                location = default(Coordinate);
                return false;
            }

            location = new Coordinate(lat, lon);
            return true;
        }
    }
}
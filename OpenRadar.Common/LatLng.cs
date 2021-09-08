using System;
using System.Text;

namespace FreeRadar.Common
{
    /// <summary>
    /// A readonly latitude-longitude pair, both in decimal degrees.
    /// </summary>
    /// <param name="Latitude">The latitude of the represented coordinates</param>
    /// <param name="Longitude">The longitude of the represented coordinates</param>
    public readonly record struct LatLng(double Latitude, double Longitude)
    {
        /// <summary>
        /// Computes the bearing and distance from one coordinate to another using the haversine formula
        /// </summary>
        /// <param name="origin">The origin coordinate</param>
        /// <param name="dest">The destination coordinate</param>
        /// <param name="bearing">The (true) bearing of a great circle segment from the origin to the destination in degrees</param>
        /// <param name="distance">The great circle distance between the two points, in nautical miles</param>
        public static void Haversine(LatLng origin, LatLng dest, out double bearing,
            out double distance) {
            const double diameter = 6880.14D; // nmi
            double lat1 = origin.Latitude * Math.PI / 180;
            double lat2 = dest.Latitude * Math.PI / 180;
            double long1 = origin.Longitude * Math.PI / 180;
            double long2 = dest.Longitude * Math.PI / 180;

            double sdLat = Math.Sin((lat2 - lat1) / 2);
            double sdLong = Math.Sin((long2 - long1) / 2);
            double q = sdLat * sdLat + Math.Cos(lat1) * Math.Cos(lat2) * sdLong * sdLong;
            double brgRad = Math.Asin(Math.Sqrt(q));
            distance = diameter * brgRad;
            bearing = (brgRad * 180 / Math.PI + 360) % 360;
        }

        private static void FormatCoordinate(StringBuilder builder, double rawCoord) {
            double absCoord = Math.Abs(rawCoord);
            int degrees = (int)Math.Floor(absCoord);
            builder.Append(degrees)
                .Append('\u00B0');
            double rawMinutes = (absCoord - degrees) * 60;
            double minutes = Math.Floor(rawMinutes);
            builder.AppendFormat("{0:00}", minutes).Append('\u2032');
            double seconds = (rawMinutes - minutes) * 60;
            builder.AppendFormat("{0:00.000}", seconds).Append('\u2033');
        }
        
        /// <inheritdoc />
        public override string ToString() {
            StringBuilder builder = new();
            builder.Append(Latitude >= 0 ? 'N' : 'S');
            FormatCoordinate(builder, Latitude);
            builder.Append(' ')
                .Append(Longitude >= 0 ? 'E' : 'W');
            FormatCoordinate(builder, Longitude);
            return builder.ToString();
        }
    }
}
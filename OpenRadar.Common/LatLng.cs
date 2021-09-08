using System;
using System.Text;

namespace FreeRadar.Common
{
    public readonly record struct LatLng(double Latitude, double Longitude)
    {
        public static void Haversine(LatLng first, LatLng second, out double bearing,
            out double distance) {
            const double diameter = 6880.14D; // nmi
            double lat1 = first.Latitude * Math.PI / 180;
            double lat2 = second.Latitude * Math.PI / 180;
            double long1 = first.Longitude * Math.PI / 180;
            double long2 = second.Longitude * Math.PI / 180;

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
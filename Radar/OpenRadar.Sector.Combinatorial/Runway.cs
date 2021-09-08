using FreeRadar.Common;

namespace OpenRadar.Sector
{
    public readonly record struct Runway(string RunwayNumber, string RunwayNumberOpposite, int MagHeading,
        int MagHeadingOpposite, LatLng Start, LatLng End)
    {
        public static Runway Create(string num, string numOpp, int magHead, int magOpp, LatLng start, LatLng end) =>
            new(num, numOpp, magHead, magOpp, start, end);
    }
}
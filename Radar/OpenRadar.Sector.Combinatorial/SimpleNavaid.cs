using FreeRadar.Common;

namespace OpenRadar.Sector
{
    public readonly struct SimpleNavaid : INavaid
    {
        /// <summary>
        /// NAVAID Ident.
        /// Three characters for VOR, two for NDB, four for an airport, five for a fix.
        /// </summary>
        public string Id { get; private init; }
        /// <summary>
        /// Navaid radio nav frequency, expressed in KHz instead of MHz for generality and to avoid floating point error
        /// </summary>
        public int Freq { get; private init; }
        
        /// <summary>
        /// The geographic coordinates of the navaid on the surface of earth, expressed as a latitude/longitude pair
        /// </summary>
        public LatLng Coordinates { get; private init; }

        public static SimpleNavaid Create(string id, int freq, LatLng coordinates) => new() {
            Id = id,
            Coordinates = coordinates,
            Freq = freq,
        };
    }
}
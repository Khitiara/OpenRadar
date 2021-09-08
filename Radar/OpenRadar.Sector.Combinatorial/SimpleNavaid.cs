using FreeRadar.Common;

namespace OpenRadar.Sector
{
    public readonly struct SimpleNavaid : INavaid
    {
        public string Id { get; private init; }
        public int Freq { get; private init; }
        public LatLng Coordinates { get; private init; }

        public static SimpleNavaid Create(string id, int freq, LatLng coordinates) => new() {
            Id = id,
            Coordinates = coordinates,
            Freq = freq,
        };
    }
}
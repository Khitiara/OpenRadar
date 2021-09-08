using FreeRadar.Common;

namespace OpenRadar.Sector
{
    public readonly record struct Fix(string Id, LatLng Coord)
    {
        public static Fix Create(string id, LatLng coord) => new(id, coord);
    }
}
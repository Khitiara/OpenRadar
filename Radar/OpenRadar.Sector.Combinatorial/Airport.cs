using FreeRadar.Common;

namespace OpenRadar.Sector
{
    public enum AirportClass
    {
        A = 'A',
        B,
        C,
        D,
        E
    }

    public readonly record struct Airport(string Id, int Frequency, LatLng Coordinates, AirportClass Class) : INavaid
    {
        internal static Airport Create(string id, int freq, LatLng coords, char clazz) =>
            new(id, freq, coords, (AirportClass)clazz);
    }
}
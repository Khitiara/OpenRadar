using FreeRadar.Common;

namespace OpenRadar.Sector
{
    public interface INavaid
    {
        public string Id { get; }
        public LatLng Coordinates { get; }
    }
}
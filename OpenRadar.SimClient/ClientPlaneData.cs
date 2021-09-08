using System.Runtime.InteropServices;
using SimConnectUtils;

namespace OpenRadar.SimClient
{
    /// <summary>
    /// Measured aircraft data as a marshallable structure which may be returned by SimConnect api calls.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct ClientPlaneData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        [SimConnectDataField("ATC Flight Number", null)]
        public readonly string FlightNum;

        [SimConnectDataField("Plane Latitude", "Degrees")]
        public readonly double Latitude;

        [SimConnectDataField("Plane Longitude", "Degrees")]
        public readonly double Longitude;

        [SimConnectDataField("Plane Altitude", "Feet")]
        public readonly double Altitude;

        [SimConnectDataField("Com Active Frequency:1", "KHz")]
        public readonly int ComFrequency;

        [SimConnectDataField("Transponder Code:1", "Bco16")]
        public readonly int TransponderSquawk;

        [SimConnectDataField("Gps Ground True Track", "Degrees")]
        public readonly double GroundTrack;
        
        [SimConnectDataField("Gps Ground Speed", "Knots")]
        public readonly double GroundSpeed;

        public override string ToString() {
            return $"{nameof(FlightNum)}: {FlightNum}, {nameof(Latitude)}: {Latitude}, {nameof(Longitude)}: {Longitude}, {nameof(Altitude)}: {Altitude}, {nameof(TransponderSquawk)}: {TransponderSquawk:X}, {nameof(GroundTrack)}: {GroundTrack}, {nameof(GroundSpeed)}: {GroundSpeed}, {nameof(ComFrequency)}: {ComFrequency}";
        }
    };
}
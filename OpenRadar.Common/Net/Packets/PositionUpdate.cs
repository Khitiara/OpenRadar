using System.Runtime.InteropServices;

namespace FreeRadar.Common.Net.Packets
{
    /// <summary>
    /// A position update packet, sent from the sim client to the radar client to indicate changes to the position of the simulated aircraft
    /// </summary>
    [Packet(PacketTag.PositionUpdate)]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly struct PositionUpdate : ISimToRadarPacket
    {
        /// <summary>
        /// Aircraft call sign, as retrieved from the ATC flight number specified in MSFS
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public readonly string CallSign;

        /// <summary>
        /// Transponder code, specified in Binary-Coded Octal
        /// The lower nibble of each byte of the value encodes a single value of the squawk code
        /// E.g. If the transponder is set to <c>"1234"</c>,
        /// then <c><see cref="Squawk"/> == 0x1234</c>
        /// </summary>
        public readonly ushort Squawk;

        /// <summary>
        /// Current active communications radio frequency, specified in kilohertz.
        /// As VHF communications radio generally uses megahertz to three decimal places,
        /// a frequency of <c>121.250</c> results in <c><see cref="Freq"/> == 121250</c>
        /// This encoding is done to prevent floating-point inaccuracies that would arise from
        /// storing a frequency in MHz directly.
        /// </summary>
        public readonly int Freq;

        /// <summary>
        /// The current position of the aircraft on the surface of the earth
        /// </summary>
        public readonly LatLng Coords;

        /// <summary>
        /// The current true altitude of the aircraft above mean sea level
        /// </summary>
        public readonly double Altitude;

        /// <summary>
        /// The current ground speed of the aircraft in Knots
        /// </summary>
        public readonly double GroundSpeed;

        /// <summary>
        /// The current TRUE ground track of the aircraft, in degrees
        /// </summary>
        /// <remarks>
        /// This field does NOT contain the magnetic ground track of the aircraft
        /// </remarks>
        public readonly double GroundTrack;

        public PositionUpdate(string callSign, ushort squawk, int freq, LatLng coords, double altitude, double groundSpeed,
            double groundTrack) {
            CallSign = callSign;
            Squawk = squawk;
            Freq = freq;
            Coords = coords;
            Altitude = altitude;
            GroundSpeed = groundSpeed;
            GroundTrack = groundTrack;
        }
    }
}
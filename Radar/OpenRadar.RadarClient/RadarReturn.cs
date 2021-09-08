using System;
using FreeRadar.Common.Net.Packets;

namespace OpenRadar.RadarClient
{
    /// <summary>
    /// Represents a radar return from an aircraft.
    /// </summary>
    /// <param name="Return">The raw data obtained by the aircraft</param>
    /// <param name="Received">The time the data was observed</param>
    /// <param name="WeakClient">A weak reference to the network client sentinel that sent this data</param>
    public sealed record RadarReturn(PositionUpdate Return, DateTime Received, WeakReference<AtcClient.AtcPlane> WeakClient)
    {
        /// <summary>
        /// The network client sentinel that sent this data, or null if the connection is closed and the sentinel destroyed.
        /// </summary>
        public AtcClient.AtcPlane? Client => WeakClient.TryGetTarget(out AtcClient.AtcPlane? client) ? client : null;
    }
}
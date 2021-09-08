using System;
using FreeRadar.Common.Net.Packets;

namespace OpenRadar.RadarClient
{
    public sealed record RadarReturn(PositionUpdate Return, DateTime Received, WeakReference<AtcClient.AtcPlane> WeakClient)
    {
        public AtcClient.AtcPlane? Client => WeakClient.TryGetTarget(out AtcClient.AtcPlane? client) ? client : null;
    }
}
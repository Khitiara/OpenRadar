namespace FreeRadar.Common.Net.Packets
{
    /// <summary>
    /// The base interface for all packets sent from the simulator client to the radar client.
    /// </summary>
    public interface ISimToRadarPacket
    { }

    /// <summary>
    /// The base interface for all packets sent from the radar client to the simulator client.
    /// </summary>
    public interface IRadarToSimPacket
    { }
}
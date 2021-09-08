using System;
using FreeRadar.Common.Net.Packets;

namespace FreeRadar.Common.Net
{
    /// <summary>
    /// Used to mark a struct as being a packet for use with the scheme represented by <see cref="PacketMarshaller"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class PacketAttribute : Attribute
    {
        // wanted to use System.Enum here but it wont let me
        public PacketAttribute(PacketTag tagEnum) : this((ushort)tagEnum) { }

        public PacketAttribute(ushort packetTag) {
            PacketTag = packetTag;
        }

        /// <summary>
        /// An unsigned two-byte value which will be prepended along with size to every packet sent,
        /// and used to determine the type of incoming packets
        /// </summary>
        public ushort PacketTag { get; }
    }
}
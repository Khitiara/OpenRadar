using System;
using FreeRadar.Common.Net.Packets;

namespace FreeRadar.Common.Net
{
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class PacketAttribute : Attribute
    {
        // wanted to use System.Enum here but it wont let me
        public PacketAttribute(PacketTag tagEnum) : this((ushort)tagEnum) { }

        public PacketAttribute(uint packetTag) {
            PacketTag = (ushort)packetTag;
        }

        public ushort PacketTag { get; }
    }
}
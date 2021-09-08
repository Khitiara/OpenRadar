using System.Runtime.InteropServices;

namespace OpenRadar.Sector
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Color
    {
        [FieldOffset(0x0)]
        public readonly uint Value;

        [FieldOffset(0x0)]
        public readonly byte Red;

        [FieldOffset(0x1)]
        public readonly byte Green;

        [FieldOffset(0x2)]
        public readonly byte Blue;

        public Color(uint value) : this() {
            Value = value;
        }

        public Color(byte red, byte green, byte blue) : this() {
            Red = red;
            Green = green;
            Blue = blue;
        }
    }
}
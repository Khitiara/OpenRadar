using System;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SimConnectDataFieldAttribute : Attribute
    {
        public SimConnectDataFieldAttribute(string datum, string? units) {
            Datum = datum;
            Units = units;
        }

        public string Datum { get; }
        public string? Units { get; }
        public SIMCONNECT_DATATYPE Datatype { get; set; } = SIMCONNECT_DATATYPE.INVALID;
        public float Epsilon { get; set; } = 0f;
    }
}
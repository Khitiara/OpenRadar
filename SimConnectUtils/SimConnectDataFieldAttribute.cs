using System;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    /// <summary>
    /// Marks a field as being used in a SimConnect data definition, for communicating with the simulator
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SimConnectDataFieldAttribute : Attribute
    {
        public SimConnectDataFieldAttribute(string datum, string? units) {
            Datum = datum;
            Units = units;
        }

        /// <summary>
        /// The simulation variable the marked field will store.
        /// </summary>
        public string Datum { get; }

        /// <summary>
        /// The units the simulation variable will be returned in.
        /// </summary>
        public string? Units { get; }

        /// <summary>
        /// A <see cref="Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE"/> corresponding to the managed type
        /// of the marked field. Leave as <see cref="Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INVALID"/>
        /// for automatic detection.
        /// </summary>
        public SIMCONNECT_DATATYPE Datatype { get; set; } = SIMCONNECT_DATATYPE.INVALID;

        /// <summary>
        /// An epsilon-value used for detecting if a simulation variable has changed, for periodic on-change data retrieval.
        /// </summary>
        public float Epsilon { get; set; } = 0f;
    }
}
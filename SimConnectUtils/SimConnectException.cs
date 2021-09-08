using System;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    public class SimConnectException : Exception
    {
        public readonly SIMCONNECT_EXCEPTION Error;
        public readonly uint                 ParamIndex;

        public SimConnectException(SIMCONNECT_EXCEPTION error, uint paramIndex) : base(
            $"Simconnect error: {error} at parameter index {paramIndex}") {
            Error = error;
            ParamIndex = paramIndex;
        }
    }
}
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    public static class SimConnectEx
    {
        private static SIMCONNECT_DATATYPE GetStringDatumType(FieldInfo field) {
            MarshalAsAttribute marshalAsAttribute =
                field.GetCustomAttribute<MarshalAsAttribute>() ??
                throw new InvalidOperationException();
            if (marshalAsAttribute.Value != UnmanagedType.ByValTStr) {
                throw new InvalidOperationException();
            }

            return marshalAsAttribute.SizeConst switch {
                < 8 => throw new InvalidOperationException(),
                < 32 => SIMCONNECT_DATATYPE.STRING8,
                < 64 => SIMCONNECT_DATATYPE.STRING32,
                < 128 => SIMCONNECT_DATATYPE.STRING64,
                < 256 => SIMCONNECT_DATATYPE.STRING128,
                < 260 => SIMCONNECT_DATATYPE.STRING256,
                _ => SIMCONNECT_DATATYPE.STRING260
            };
        }

        public static void RegisterReflectedDataType<T>(this SimConnect self, Enum dwId, bool setDatumId = false,
            bool inferDataType = true) {
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields();
            for (uint i = 0; i < fields.Length; i++) {
                FieldInfo field = fields[i];
                SimConnectDataFieldAttribute attribute =
                    field.GetCustomAttribute<SimConnectDataFieldAttribute>() ??
                    throw new InvalidOperationException(
                        "All fields must be annotated with SimConnectDataFieldAttribute to use RegisterReflectedDataType");
                SIMCONNECT_DATATYPE datumType = attribute.Datatype;
                if (datumType == SIMCONNECT_DATATYPE.INVALID) {
                    if (inferDataType) {
                        Type fieldType = field.FieldType;
                        TypeCode code = Type.GetTypeCode(fieldType);
                        datumType = code switch {
                            TypeCode.Boolean =>
                                // TODO figure out how bools are marshalled
                                throw new NotImplementedException(),
                            TypeCode.Int32 => SIMCONNECT_DATATYPE.INT32,
                            TypeCode.Int64 => SIMCONNECT_DATATYPE.INT64,
                            TypeCode.Single => SIMCONNECT_DATATYPE.FLOAT32,
                            TypeCode.Double => SIMCONNECT_DATATYPE.FLOAT64,
                            TypeCode.String => GetStringDatumType(field),
                            _ => throw new NotSupportedException()
                        };
                    } else {
                        throw new InvalidOperationException(
                            "Invalid data type in SimConnectDataFieldAttribute with inferDataType false");
                    }
                }

                self.AddToDataDefinition(dwId, attribute.Datum, attribute.Units, datumType,
                    attribute.Epsilon, setDatumId ? i : SimConnect.SIMCONNECT_UNUSED);
            }

            self.RegisterDataDefineStruct<T>(dwId);
        }

        public delegate IntPtr HWndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
        
        public static void CreateSimConnect(string name, IntPtr hWnd, uint win32UserEvent, WaitHandle? eventWaitHandle,
            uint configIdx, out SimConnect simConnect, out HWndProc hWndHook) {
            SimConnect connect = simConnect = new SimConnect(name, hWnd, win32UserEvent, eventWaitHandle, configIdx);
            hWndHook = (IntPtr _, int msg, IntPtr _, IntPtr _, ref bool handled) => {
                if (msg != win32UserEvent) return IntPtr.Zero;
                connect.ReceiveMessage();
                handled = true;

                return IntPtr.Zero;
            };
        }
    }
}
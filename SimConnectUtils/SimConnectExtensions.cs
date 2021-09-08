using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    /// <summary>
    /// Extensions to the <see cref="SimConnect"/> type.
    /// </summary>
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

        /// <summary>
        /// Registers a data definition with the provided <see cref="SimConnect"/> instance, using reflection and
        /// <see cref="SimConnectDataFieldAttribute"/> to determine data definition structure.
        /// </summary>
        /// <param name="self">The simulator connection</param>
        /// <param name="dwId">The Enum data definition ID which will be used to refer to this definition in future requests</param>
        /// <param name="setDatumId">If true, then the datum id (field index) will be preserved in the registered definition</param>
        /// <param name="inferDataType">Whether to infer the data type when <see cref="SimConnectDataFieldAttribute.Datatype"/>
        /// equals <see cref="Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INVALID"/></param>
        /// <typeparam name="T">The type of the managed struct to reflect</typeparam>
        /// <exception cref="InvalidOperationException">If <see cref="SimConnectDataFieldAttribute.Datatype"/>
        /// equals <see cref="Microsoft.FlightSimulator.SimConnect.SIMCONNECT_DATATYPE.INVALID"/> and <paramref name="inferDataType"/> is false</exception>
        /// <exception cref="NotSupportedException">When inferring a <see cref="TypeCode"/> that is not supported by SimConnect interop</exception>
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

        /// <summary>
        /// The same as <see cref="System.Windows.Interop.HwndSourceHook"/>
        /// </summary>
        public delegate IntPtr HWndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        /// <summary>
        /// Create and open a simulator connection
        /// </summary>
        /// <param name="name">The client name</param>
        /// <param name="hWnd">A pointer to the window that will host this connection</param>
        /// <param name="win32UserEvent">A win32 event loop user event, used to pump data reception on the win32 event loop</param>
        /// <param name="eventWaitHandle">A waithandle that will be signalled by the native SimConnect library when data is available to receive</param>
        /// <param name="configIdx">A configuration file index for SimConnect. Retained for legacy reasons</param>
        /// <param name="simConnect">The created simulator connection</param>
        /// <param name="hWndHook">A delegate which may be registered to the win32 event loop to pump message reception by the simulator connection</param>
        internal static void CreateSimConnect(string name, IntPtr hWnd, uint win32UserEvent,
            WaitHandle? eventWaitHandle,
            uint configIdx, out SimConnect simConnect, out HWndProc hWndHook) {
            SimConnect connect = simConnect = new SimConnect(name, hWnd, win32UserEvent, eventWaitHandle, configIdx);
            hWndHook = (IntPtr _, int msg, IntPtr _, IntPtr _, ref bool handled) => {
                if (msg != win32UserEvent) return IntPtr.Zero;
                connect.ReceiveMessage();
                handled = true;

                return IntPtr.Zero;
            };
        }

        /// <summary>
        /// Utility to asynchronously dispose of nullable values
        /// </summary>
        public static ValueTask TryDisposeAsync(this IAsyncDisposable? disposable) =>
            disposable?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
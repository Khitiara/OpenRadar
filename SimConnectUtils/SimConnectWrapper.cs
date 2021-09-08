using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    /// <summary>
    /// Extern functions not provided by the managed SimConnect wrapper in the flight simulator SDK
    /// </summary>
    internal static class SimConnectWrapper
    {
        [DllImport("SimConnect.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern int SimConnect_GetLastSentPacketID(IntPtr hSimConnect, out uint /* DWORD */ dwSendId);
    }

    /// <summary>
    /// A wrapper around <see cref="SimConnect"/> providing asynchronous exception support, typed request and data definition ids, and various helpers.
    /// </summary>
    /// <typeparam name="TRequestId">The enum type used for request ids.</typeparam>
    /// <typeparam name="TDataDefineId">The enum type used for data definition ids.</typeparam>
    public sealed class SimConnectWrapper<TRequestId, TDataDefineId> : IDisposable
        where TRequestId : Enum
        where TDataDefineId : Enum
    {
        /// <summary>
        /// The wrapped simulator connection
        /// </summary>
        public SimConnect Connection { get; }

        private readonly Dictionary<uint, TRequestId>               _packets;
        private readonly Dictionary<TRequestId, uint>               _requests;
        private readonly IntPtr                                     _hSimConnect;
        private readonly TaskCompletionSource<SIMCONNECT_RECV_OPEN> _openTcs;

        private IDisposable ProcRegistration { get; }

        public SIMCONNECT_RECV_OPEN Open => _openTcs.Task.Result;

        private SimConnectWrapper(SimConnect connection, SimConnectEx.HWndProc hWndProc,
            Func<SimConnectEx.HWndProc, IDisposable> registerProc) {
            Connection = connection;
            ProcRegistration = registerProc(hWndProc);
            _openTcs = new TaskCompletionSource<SIMCONNECT_RECV_OPEN>();
            Connection.OnRecvOpen += Connection_OnRecvOpen;
            _hSimConnect = (IntPtr)typeof(SimConnect)
                .GetField("hSimConnect", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(connection)!;
            _packets = new Dictionary<uint, TRequestId>();
            _requests = new Dictionary<TRequestId, uint>();
            Connection.OnRecvException += Connection_OnRecvException;
            Connection.OnRecvSimobjectData += Connection_OnRecvSimobjectData;
            Connection.OnRecvQuit += (_, data) => Quit?.Invoke(this, data);
            Requests = new RequestsImpl();
        }

        /// <summary>
        /// Creates and asynchronously opens a simulator connection.
        /// </summary>
        /// <param name="name">The name of the client</param>
        /// <param name="hWnd">A native window handle used for message pumping</param>
        /// <param name="win32UserEvent">The win32 user event id for SimConnect pumping</param>
        /// <param name="eventWaitHandle">A wait handle which will be signalled whenever new messages are available from the simulator</param>
        /// <param name="registerProc">A callback to register the win32 message loop handler for message pumping</param>
        /// <returns>The created connection</returns>
        public static async Task<SimConnectWrapper<TRequestId, TDataDefineId>> OpenAsync(string name, IntPtr hWnd,
            uint win32UserEvent, WaitHandle? eventWaitHandle, Func<SimConnectEx.HWndProc, IDisposable> registerProc) {
            SimConnectEx.CreateSimConnect(name, hWnd, win32UserEvent, eventWaitHandle, 0,
                out SimConnect connection, out SimConnectEx.HWndProc proc);
            SimConnectWrapper<TRequestId, TDataDefineId> simConnect = new(connection, proc, registerProc);
            await simConnect._openTcs.Task;
            return simConnect;
        }

        private void Connection_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            _openTcs.TrySetResult(data);
        }

        private void Connection_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
            if (!_packets.TryGetValue(data.dwSendID, out TRequestId? requestId)) return;
            Requests[requestId].OnRequestException(this,
                new SimConnectException((SIMCONNECT_EXCEPTION)data.dwException, data.dwIndex));
        }

        /// <summary>
        /// Event handler map for data received after requests.
        /// </summary>
        public readonly RequestsImpl Requests;

        private void Connection_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            TRequestId requestId = (TRequestId)Enum.ToObject(typeof(TRequestId), data.dwRequestID);
            UnregisterPending(requestId);

            Requests[requestId].OnSimObjectData(this, data);
        }

        /// <summary>
        /// Forwards a call to <see cref="SimConnect.RequestDataOnSimObject"/> and sets up asynchronous exception receipt.
        /// </summary>
        public void RequestDataOnSimObject(TRequestId requestId, TDataDefineId defineId, uint objectId,
            SIMCONNECT_PERIOD period, SIMCONNECT_DATA_REQUEST_FLAG flags, uint origin, uint interval, uint limit) {
            Connection.RequestDataOnSimObject(requestId, defineId, objectId, period, flags, origin, interval, limit);
            RegisterPendingReq(requestId);
        }

        /// <summary>
        /// Asynchronously requests a single return of data from the simulator, propagating delayed exceptions as needed.
        /// </summary>
        /// <param name="requestId">The request id to use for communication</param>
        /// <param name="defineId">The data definition id of the struct expected to be returned</param>
        /// <param name="objectId">The id of the object on which data is being requested.</param>
        /// <param name="cancellationToken">Cancels the asynchronous operation</param>
        /// <returns>The resulting data on a successful request</returns>
        public async Task<T> RequestDataOnSimObjectOnceAsync<T>(TRequestId requestId, TDataDefineId defineId,
            uint objectId,
            CancellationToken cancellationToken = default) {
            TaskCompletionSource<T> output = new();

            void OnRequestException(object? sender, Exception e) {
                output.TrySetException(e);
            }

            void OnData(object? sender, SIMCONNECT_RECV_SIMOBJECT_DATA e) {
                output.TrySetResult((T)e.dwData[0]);
            }

            CancellationTokenRegistration? registration = null;
            try {
                registration = cancellationToken.Register(() => output.TrySetCanceled(cancellationToken));
                Requests[requestId].RequestException += OnRequestException;
                Requests[requestId].SimObjectData += OnData;


                Connection.RequestDataOnSimObject(requestId, defineId, objectId, SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                uint pendingReq = RegisterPendingReq(requestId);
                try {
                    return await output.Task;
                }
                finally {
                    UnregisterPending(pendingReq);
                }
            }
            finally {
                Requests[requestId].RequestException -= OnRequestException;
                Requests[requestId].SimObjectData -= OnData;
                await registration.TryDisposeAsync();
            }
        }

        private void UnregisterPending(uint pendingReq) {
            if (_packets.Remove(pendingReq, out TRequestId? requestId))
                _requests.Remove(requestId);
        }


        /// <summary>
        /// Invoked when the simulator connection ends, whether because of disposal of the connection or because the simulator
        /// instance is closed.
        /// </summary>
        public event Action<SimConnectWrapper<TRequestId, TDataDefineId>, SIMCONNECT_RECV>? Quit;

        public sealed class RequestsImpl
        {
            private readonly Dictionary<TRequestId, RequestEvents> _dictionary = new();

            /// <summary>
            /// Get the event set for a given request id, or creates it if not present.
            /// </summary>
            public RequestEvents this[TRequestId requestId] =>
                _dictionary.TryGetValue(requestId, out RequestEvents? events)
                    ? events
                    : _dictionary[requestId] = new RequestEvents();
        }

        /// <summary>
        /// Wrapper on events for a particular request, for returning asynchronous exceptions or returned data.
        /// </summary>
        public sealed class RequestEvents
        {
            /// <summary>
            /// Invoked when data is received matching the request id for this event set.
            /// </summary>
            public event EventHandler<SIMCONNECT_RECV_SIMOBJECT_DATA>? SimObjectData;

            /// <summary>
            /// Invoked when an exception is noted for a packet corresponding to the request id for this event set.
            /// </summary>
            public event EventHandler<Exception>? RequestException;

            internal void OnSimObjectData(SimConnectWrapper<TRequestId, TDataDefineId> sender,
                SIMCONNECT_RECV_SIMOBJECT_DATA e) {
                SimObjectData?.Invoke(sender, e);
            }

            internal void OnRequestException(SimConnectWrapper<TRequestId, TDataDefineId> sender, Exception e) {
                RequestException?.Invoke(sender, e);
            }
        }

        private uint RegisterPendingReq(TRequestId requestId) {
            Marshal.ThrowExceptionForHR(
                SimConnectWrapper.SimConnect_GetLastSentPacketID(_hSimConnect, out uint sendId));
            _packets[sendId] = requestId;
            _requests[requestId] = sendId;
            return sendId;
        }

        private void UnregisterPending(TRequestId requestId) {
            if (_requests.Remove(requestId, out uint sendId))
                _packets.Remove(sendId);
        }

        /// <summary>
        /// Wrapper around <see cref="SimConnectEx.RegisterReflectedDataType{T}"/> with typed data definition id
        /// </summary>
        public void RegisterReflectedDataType<T>(TDataDefineId definitionId) {
            Connection.RegisterReflectedDataType<T>(definitionId);
        }

        /// <inheritdoc />
        public void Dispose() {
            ProcRegistration.Dispose();
            Connection.Dispose();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;

namespace SimConnectUtils
{
    internal static class AsyncSimConnect
    {
        [DllImport("SimConnect.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern int SimConnect_GetLastSentPacketID(IntPtr hSimConnect, out uint /* DWORD */ dwSendId);
    }

    public sealed class AsyncSimConnect<TRequestId, TDataDefineId> : IDisposable
        where TRequestId : Enum
        where TDataDefineId : Enum
    {
        public SimConnect Connection { get; }

        private readonly Dictionary<uint, TRequestId>               _packets;
        private readonly Dictionary<TRequestId, PendingReq>         _requests;
        private readonly IntPtr                                     _hSimConnect;
        private readonly TaskCompletionSource<SIMCONNECT_RECV_OPEN> _openTcs;

        public SimConnectEx.HWndProc HWndProc { get; }

        private AsyncSimConnect(SimConnect connection, SimConnectEx.HWndProc hWndProc) {
            Connection = connection;
            HWndProc = hWndProc;
            _openTcs = new TaskCompletionSource<SIMCONNECT_RECV_OPEN>();
            Connection.OnRecvOpen += Connection_OnRecvOpen;
            _hSimConnect = (IntPtr)typeof(SimConnect)
                .GetField("hSimConnect", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(connection)!;
            _packets = new Dictionary<uint, TRequestId>();
            _requests = new Dictionary<TRequestId, PendingReq>();
            _dataRequests = new Dictionary<TRequestId, TaskCompletionSource<SIMCONNECT_RECV_SIMOBJECT_DATA>>();
            Connection.OnRecvException += Connection_OnRecvException;
            Connection.OnRecvSimobjectData += Connection_OnRecvSimobjectData;
            Connection.OnRecvAssignedObjectId += Connection_OnRecvAssignedObjectId;
            Connection.OnRecvQuit += (_, data) => OnQuit(data);
            _assignedObjectRequests =
                new Dictionary<TRequestId, TaskCompletionSource<SIMCONNECT_RECV_ASSIGNED_OBJECT_ID>>();
            Requests = new RequestsImpl();
        }

        public static (AsyncSimConnect<TRequestId, TDataDefineId> simConnect, Task<SIMCONNECT_RECV_OPEN> Task) Open(string name,
            IntPtr hWnd,
            uint win32UserEvent, WaitHandle? eventWaitHandle, uint configIdx) {
            SimConnectEx.CreateSimConnect(name, hWnd, win32UserEvent, eventWaitHandle, configIdx,
                out SimConnect connection, out SimConnectEx.HWndProc proc);
            AsyncSimConnect<TRequestId, TDataDefineId> simConnect = new(connection, proc);
            return (simConnect, simConnect._openTcs.Task);
        }

        private void Connection_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            _openTcs.TrySetResult(data);
        }

        private void Connection_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
            if (!_packets.TryGetValue(data.dwSendID, out TRequestId? requestId)) return;
            if (!_requests.TryGetValue(requestId, out PendingReq? pendingReq)) return;
            pendingReq.TrySetException(
                new SimConnectException((SIMCONNECT_EXCEPTION)data.dwException, data.dwIndex));
        }

        private readonly Dictionary<TRequestId, TaskCompletionSource<SIMCONNECT_RECV_SIMOBJECT_DATA>> _dataRequests;

        public readonly RequestsImpl Requests;

        private void Connection_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            TRequestId requestId = (TRequestId)Enum.ToObject(typeof(TRequestId), data.dwRequestID);
            if (_dataRequests.TryGetValue(requestId,
                out TaskCompletionSource<SIMCONNECT_RECV_SIMOBJECT_DATA>? tcs)) {
                tcs.TrySetResult(data);
            }

            Requests[requestId].OnSimObjectData(this, data);
        }

        public async IAsyncEnumerable<T> RequestDataOnSimObjectAsync<T>(TRequestId requestId, TDataDefineId defineId,
            uint objectId, SIMCONNECT_PERIOD period, SIMCONNECT_DATA_REQUEST_FLAG flags, uint origin, uint interval,
            uint limit, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            Connection.RequestDataOnSimObject(requestId, defineId, objectId, period, flags, origin, interval, limit);
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    TaskCompletionSource<SIMCONNECT_RECV_SIMOBJECT_DATA> taskCompletionSource = new();
                    _dataRequests[requestId] = taskCompletionSource;
                    uint sendId = RegisterPendingReq(requestId,
                        new TypedPendingReq<SIMCONNECT_RECV_SIMOBJECT_DATA>(taskCompletionSource));
                    try {
                        await using (
                            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken))) {
                            SIMCONNECT_RECV_SIMOBJECT_DATA data = await taskCompletionSource.Task;
                            yield return (T)data.dwData[0];
                        }
                    }
                    finally {
                        UnregisterPending(requestId, sendId);
                    }
                }
            }
            finally {
                Connection.RequestDataOnSimObject(requestId, defineId, objectId, SIMCONNECT_PERIOD.NEVER, flags, origin,
                    interval, limit);
            }
        }

        public void RequestDataOnSimObject(TRequestId requestId, TDataDefineId defineId, uint objectId,
            SIMCONNECT_PERIOD period, SIMCONNECT_DATA_REQUEST_FLAG flags, uint origin, uint interval, uint limit) =>
            Connection.RequestDataOnSimObject(requestId, defineId, objectId, period, flags, origin, interval, limit);

        private readonly Dictionary<TRequestId, TaskCompletionSource<SIMCONNECT_RECV_ASSIGNED_OBJECT_ID>>
            _assignedObjectRequests;

        private void Connection_OnRecvAssignedObjectId(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data) {
            TRequestId requestId = (TRequestId)Enum.ToObject(typeof(TRequestId), data.dwRequestID);
            if (_assignedObjectRequests.TryGetValue(requestId,
                out TaskCompletionSource<SIMCONNECT_RECV_ASSIGNED_OBJECT_ID>? tcs)) {
                tcs.TrySetResult(data);
            }

            Requests[requestId].OnAssignedObjectId(this, data);
        }

        public event Action<AsyncSimConnect<TRequestId, TDataDefineId>, SIMCONNECT_RECV>? Quit;

        public sealed class RequestsImpl
        {
            private Dictionary<TRequestId, RequestEvents> _dictionary = new();

            public RequestEvents this[TRequestId requestId] =>
                _dictionary.TryGetValue(requestId, out RequestEvents? events)
                    ? events
                    : _dictionary[requestId] = new RequestEvents();
        }

        public sealed class RequestEvents
        {
            public event EventHandler<SIMCONNECT_RECV_ASSIGNED_OBJECT_ID>? AssignedObjectId;
            public event EventHandler<SIMCONNECT_RECV_SIMOBJECT_DATA>? SimObjectData;
            public event EventHandler<SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE>? SimObjectDataByType;

            internal void OnAssignedObjectId(AsyncSimConnect<TRequestId, TDataDefineId> sender,
                SIMCONNECT_RECV_ASSIGNED_OBJECT_ID e) {
                AssignedObjectId?.Invoke(sender, e);
            }

            internal void OnSimObjectData(AsyncSimConnect<TRequestId, TDataDefineId> sender,
                SIMCONNECT_RECV_SIMOBJECT_DATA e) {
                SimObjectData?.Invoke(sender, e);
            }

            internal void OnSimObjectDataByType(AsyncSimConnect<TRequestId, TDataDefineId> sender,
                SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE e) {
                SimObjectDataByType?.Invoke(sender, e);
            }
        }

        #region Boilerplate

        private uint RegisterPendingReq(TRequestId requestId, PendingReq pendingReq) {
            Marshal.ThrowExceptionForHR(AsyncSimConnect.SimConnect_GetLastSentPacketID(_hSimConnect, out uint sendId));
            _packets[sendId] = requestId;
            _requests[requestId] = pendingReq;
            return sendId;
        }

        private void UnregisterPending(TRequestId requestId, uint sendId) {
            _packets.Remove(sendId);
            _requests.Remove(requestId);
        }

        private abstract class PendingReq
        {
            public abstract bool TrySetException(Exception ex);
        }

        private class TypedPendingReq<T> : PendingReq
        {
            private readonly TaskCompletionSource<T> _tcs;

            public TypedPendingReq(TaskCompletionSource<T> tcs) {
                _tcs = tcs;
            }

            public override bool TrySetException(Exception ex) => _tcs.TrySetException(ex);
        }

        #endregion

        public void RegisterReflectedDataType<T>(TDataDefineId definitionId) {
            Connection.RegisterReflectedDataType<T>(definitionId);
        }

        public void Dispose() {
            Connection.Dispose();
        }

        private void OnQuit(SIMCONNECT_RECV arg2) {
            Quit?.Invoke(this, arg2);
        }
    }
}
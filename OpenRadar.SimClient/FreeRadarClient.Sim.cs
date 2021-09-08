using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using OpenRadar.SimClient.Properties;
using Serilog;
using SimConnectUtils;

namespace OpenRadar.SimClient
{
    public sealed partial class FreeRadarClient : IDisposable
    {
        private const uint WmUserSimConnect = 0x0402;

        public ClientPlaneObservable Plane { get; }
        private SimConnectWrapper<RequestId, DefinitionId>? _simConnection;

        public event Action? OnDisconnected;

        public async Task OpenSim(string name) {
            _simConnection = await SimConnectWrapper<RequestId, DefinitionId>.OpenAsync(name,
                _hwnd?.Handle ?? IntPtr.Zero, WmUserSimConnect, null,
                proc => _hwnd?.Register(new HwndSourceHook(proc))!);
            _simConnection.Quit += (_, _) => {
                Task unused = DisconnectSimAsync();
            };
            _simConnection.Requests[RequestId.UserDataUpdate].SimObjectData += OnClientPlaneData;
            SIMCONNECT_RECV_OPEN open = _simConnection.Open;
            Log.Information("SimConnect Opened (Size: {Size}, Version: {Version}, Id: {Id})", open.dwSize,
                open.dwVersion,
                open.dwID);

            _simConnection.RegisterReflectedDataType<ClientPlaneData>(DefinitionId.ClientPlaneData);

            _clientCert = new X509Certificate2(Resources.ClientCert, "freeradar");

            _simConnection.RequestDataOnSimObject(RequestId.UserDataUpdate, DefinitionId.ClientPlaneData,
                SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0,
                1, 0);
            _logger.LogDebug("Position updates started");
        }

        private void OnClientPlaneData(object? sender, SIMCONNECT_RECV_SIMOBJECT_DATA e) {
            ClientPlaneData clientPlaneData = (ClientPlaneData)e.dwData[0];
            Window!.Dispatcher.Invoke(() => {
                // _logger.LogDebug("Got sim data: {Data}", clientPlaneData);
                Plane.PlaneData = clientPlaneData;
            });
            Outgoing.Writer.TryWrite(CreatePositionUpdatePacket(clientPlaneData));
        }

        public async Task StopPositionUpdatesAsync() {
            _simConnection?.RequestDataOnSimObject(RequestId.UserDataUpdate, DefinitionId.ClientPlaneData,
                SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.NEVER, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0,
                1, 0);
            if (IsConnected) await DisconnectRadarServicesAsync();
        }

        public async Task DisconnectSimAsync() {
            await StopPositionUpdatesAsync();
            _simConnection?.Dispose();
            _simConnection = null;
            OnOnDisconnected();
        }

        private void OnOnDisconnected() {
            OnDisconnected?.Invoke();
        }
    }
}
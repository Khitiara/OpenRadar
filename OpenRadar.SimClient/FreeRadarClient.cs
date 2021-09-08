using System.Threading.Channels;
using System.Windows;
using System.Windows.Interop;
using FreeRadar.Common.Net.Packets;
using Microsoft.Extensions.Logging;

namespace OpenRadar.SimClient
{
    public sealed partial class FreeRadarClient
    {
        private readonly ILogger<FreeRadarClient> _logger;
        private          HwndSource?              _hwnd;

        public FreeRadarClient(ILogger<FreeRadarClient> logger) {
            _logger = logger;
            Outgoing = Channel.CreateBounded<ISimToRadarPacket>(
                new BoundedChannelOptions(16) {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });
            Incoming = Channel.CreateBounded<IRadarToSimPacket>(
                new BoundedChannelOptions(16) {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleWriter = true
                });
            Plane = new ClientPlaneObservable();
        }

        public MainWindow? Window { get; private set; }

        public void Dispose() {
            _clientCert?.Dispose();
            _simConnection?.Dispose();
        }

        public void Ready(MainWindow window) {
            Window = window;
            _hwnd = PresentationSource.FromVisual(Window) as HwndSource;
        }
    }
}
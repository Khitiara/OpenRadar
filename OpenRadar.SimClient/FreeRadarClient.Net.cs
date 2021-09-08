using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FreeRadar.Common;
using FreeRadar.Common.Net;
using FreeRadar.Common.Net.Packets;
using OpenRadar.SimClient.Properties;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace OpenRadar.SimClient
{
    public sealed partial class FreeRadarClient
    {
        private int  _isConnected;
        private Task _networkMonitorTask = null!;

        public TcpClient? Client { get; private set; }
        public SslStream? Connection { get; private set; }

        private X509Certificate2? _clientCert;
        public bool IsConnected => _isConnected != 0;

        public Channel<IRadarToSimPacket> Incoming { get; }

        public Channel<ISimToRadarPacket> Outgoing { get; }

        private static EndPoint CreateEndpoint(string host, int port) =>
            IPAddress.TryParse(host, out IPAddress? address)
                ? new IPEndPoint(address, port)
                : new DnsEndPoint(host, port);

        public Task ConnectToRadarServerAsync(string host, int port, CancellationToken cancellationToken = default) =>
            ConnectToRadarServerAsync(CreateEndpoint(host, port), cancellationToken);

        public async Task ConnectToRadarServerAsync(EndPoint endPoint, CancellationToken cancellationToken = default) {
            if (Interlocked.Exchange(ref _isConnected, 1) != 0) throw new InvalidOperationException();

            Client = new TcpClient();
            switch (endPoint) {
                case IPEndPoint ipEndPoint:
                    await Client.ConnectAsync(ipEndPoint, cancellationToken);
                    break;
                case DnsEndPoint dnsEndPoint:
                    await Client.ConnectAsync(await Dns.GetHostAddressesAsync(dnsEndPoint.Host,
                        dnsEndPoint.AddressFamily,
                        cancellationToken), dnsEndPoint.Port, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(endPoint), endPoint,
                        string.Format(Resources.FreeRadarClient_ConnectToRadarServerAsync_Unexpected_endpoint_type,
                            endPoint.GetType()));
            }

            Connection = new SslStream(Client.GetStream(), false, Crypt.VerifyRemoteCert);
            await Connection.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() {
                ApplicationProtocols = new List<SslApplicationProtocol> { new("radar") },
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ClientCertificates =
                    new X509Certificate2Collection(_clientCert ?? throw new InvalidOperationException()),
                TargetHost = "FreeRadar Server"
            }, cancellationToken);
            _networkMonitorTask = Task.WhenAll(Task.Run(DoTcpReadAsync, CancellationToken.None),
                Task.Run(DoTcpWriteAsync, CancellationToken.None));
        }

        private async Task DoTcpWriteAsync() {
            using AsyncBinaryWriter writer = new(Connection, Encoding.Unicode, true);
            await Outgoing.Reader.ReadAllAsync()
                .ForEachAwaitAsync(it => PacketMarshaller.WritePacketAsync(writer, it));
        }

        private async Task DoTcpReadAsync() {
            ChannelWriter<IRadarToSimPacket> incomingWriter = Incoming.Writer;
            using AsyncBinaryReader reader = new(Connection, Encoding.Unicode, true);
            while (IsConnected) {
                await incomingWriter.WriteAsync(await PacketMarshaller.ReadPacketAsync<IRadarToSimPacket>(reader));
            }
        }

        public async Task DisconnectRadarServicesAsync() {
            if (Interlocked.Exchange(ref _isConnected, 0) != 1) return;
            await _networkMonitorTask;
            Client?.Close();
        }

        private static ISimToRadarPacket CreatePositionUpdatePacket(ClientPlaneData arg) => new PositionUpdate(
            arg.FlightNum, (ushort)(arg.TransponderSquawk & 0xFFFF), arg.ComFrequency,
            new LatLng(arg.Latitude, arg.Longitude),
            arg.Altitude, arg.GroundSpeed, arg.GroundTrack);
    }
}
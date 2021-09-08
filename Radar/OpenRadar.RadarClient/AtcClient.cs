using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using FreeRadar.Common;
using FreeRadar.Common.Net;
using FreeRadar.Common.Net.Packets;
using OpenRadar.RadarClient.Properties;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace OpenRadar.RadarClient
{
    public sealed class AtcClient : IDisposable
    {
        private          TcpListener?     _listener;
        private          int              _connected;
        private readonly List<AtcPlane>   _planes;
        private          Task             _listenerSentinel;
        private readonly X509Certificate2 _serverCert;
        private          Dispatcher?      _dispatcher;

        private readonly ObservableCollection<RadarReturn> _returns;
        private          DispatcherTimer?  _sweep;
        public ReadOnlyObservableCollection<RadarReturn> Returns { get; }

        public bool IsConnected => _connected != 0;

        public AtcClient() {
            _planes = new List<AtcPlane>();
            _listenerSentinel = Task.CompletedTask;
            _serverCert = new X509Certificate2(Resources.ServerCert, "freeradar");
            _returns = new ObservableCollection<RadarReturn>();
            Returns = new ReadOnlyObservableCollection<RadarReturn>(_returns);
        }

        public void Dispose() {
            CloseAsync().Wait();
            _serverCert.Dispose();
            _connected = 0;
        }

        public void Bind(MainWindow window) {
            if (Interlocked.Exchange(ref _connected, 1) != 0) return;

            _listener = TcpListener.Create(6898);
            _listener.Start();
            _listenerSentinel = Task.Run(ListenAsync);
            _dispatcher = window.Dispatcher;
            _sweep = new DispatcherTimer(DispatcherPriority.Background, _dispatcher);
            _sweep.Tick += SweepOnTick;
            _sweep.Interval = TimeSpan.FromSeconds(0.25);
        }

        private void SweepOnTick(object? sender, EventArgs e) {
            // the ToList here forces the lazy where-expression to be evaluated, preventing issues with read-during-enumeration
            lock (_returns) {
                _returns.Where(item => (DateTime.UtcNow - item.Received).TotalSeconds > 5)
                    .ToList()
                    .ForEach(item => _returns.Remove(item));
            }
        }

        private async Task ListenAsync() {
            while (IsConnected) {
                _planes.Add(new AtcPlane(this, await _listener!.AcceptTcpClientAsync(), IncomingHandler));
            }
        }

        private Task IncomingHandler(AtcPlane client, ISimToRadarPacket packet) {
            switch (packet) {
                case PositionUpdate positionUpdate:
                    if (positionUpdate.Squawk == 7500) {
                        // Kick with that squawk
#pragma warning disable 4014
                        // Awaiting this would be a deadlock
                        client.CloseAsync();
#pragma warning restore 4014
                        break;
                    }

                    lock (_returns) {
                        _returns.Add(new RadarReturn(positionUpdate, DateTime.UtcNow,
                            new WeakReference<AtcPlane>(client)));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(packet));
            }
            return Task.CompletedTask;
        }

        public async Task CloseAsync() {
            if (Interlocked.Exchange(ref _connected, 0) != 1) return;
            _listener!.Stop();
            await _listenerSentinel;
            await Task.WhenAll(_planes.Select(p => p.CloseAsync()));
            _planes.Clear();
            _sweep?.Stop();
            _sweep = null;
        }

        public class AtcPlane
        {
            private readonly Task      _sentinel;
            private readonly AtcClient _parent;
            private readonly TcpClient _client;

            public readonly Func<AtcPlane, ISimToRadarPacket, Task> IncomingHandler;

            public readonly Channel<IRadarToSimPacket> Outgoing = Channel.CreateBounded<IRadarToSimPacket>(
                new BoundedChannelOptions(16) {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });

            private readonly SslStream _stream;

            public AtcPlane(AtcClient parent, TcpClient client,
                Func<AtcPlane, ISimToRadarPacket, Task> incomingHandler) {
                _parent = parent;
                _client = client;
                IncomingHandler = incomingHandler;
                _stream = new SslStream(_client.GetStream(), false, Crypt.VerifyRemoteCert);
                _sentinel = Open();
            }

            private async Task Open() {
                try {
                    await _stream.AuthenticateAsServerAsync(_parent._serverCert, true, false);
                    await Task.WhenAll(Task.Run(Read), Task.Run(Write));
                    _client.Close();
                    Outgoing.Writer.TryComplete();
                }
                finally {
                    _parent._planes.Remove(this);
                }
            }

            private async Task Read() {
                try {
                    using AsyncBinaryReader reader = new(_stream, Encoding.Unicode, true);
                    while (_client.Connected) {
                        await IncomingHandler(this, await PacketMarshaller.ReadPacketAsync<ISimToRadarPacket>(reader));
                    }
                }
                finally {
                    _client.Close();
                    Outgoing.Writer.TryComplete();
                }
            }

            private async Task? Write() {
                ChannelReader<IRadarToSimPacket> outgoingReader = Outgoing.Reader;
                using AsyncBinaryWriter writer = new(_stream, Encoding.Unicode, true);
                try {
                    while (_client.Connected) {
                        await PacketMarshaller.WritePacketAsync(writer, await outgoingReader.ReadAsync());
                    }
                }
                catch (ChannelClosedException) {
                    // Someone else closed the channel, cleanly exit
                }
                finally {
                    _client.Close();
                    Outgoing.Writer.TryComplete();
                }
            }

            public async Task CloseAsync() {
                _client.Close();
                Outgoing.Writer.TryComplete();
                await _sentinel;
            }
        }
    }
}
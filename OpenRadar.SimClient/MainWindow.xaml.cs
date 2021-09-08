using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dapplo.Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using Serilog;
using SimConnectUtils;

namespace OpenRadar.SimClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWpfShell
    {
        private readonly FreeRadarClient     _client;
        private readonly ILogger<MainWindow> _logger;
        private          bool                _isConnected;

        public MainWindow(FreeRadarClient client, ILogger<MainWindow> logger) {
            _logger = logger;
            _client = client;
            _client.OnDisconnected += Client_OnDisconnected;
            InitializeComponent();
        }

        private void Client_OnDisconnected() {
            _isConnected = false;
            ConnectBtn.IsEnabled = true;
            ConnectBtn.Content = "Connect";
            StatusBox.Text = "Disconnected from simulator";
        }

        private void MainWindow_OnSourceInitialized(object? sender, EventArgs e) {
            _logger.LogDebug("Main window win32 hooks ready, initializing client logic");
            _client.Ready(this);
            _logger.LogDebug("Setting current sim data context");
            CurrentDataBox.DataContext = _client.Plane;
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e) {
            _logger.LogDebug("Stopping position updates");
            _client.StopPositionUpdatesAsync().Wait();
            _logger.LogDebug("Disposing client");
            _client.Dispose();
        }

        private async void ConnectButton_OnClick(object sender, RoutedEventArgs e) {
            if (!_isConnected) {
                StatusBox.Text = "Connecting to simulator...";
                ConnectBtn.Content = "Connecting...";
                ConnectBtn.IsEnabled = false;
                try {
                    await _client.OpenSim("OpenRadar");
                    ConnectBtn.Content = "Disconnect";
                    _isConnected = true;
                    StatusBox.Text = "Connected";
                }
                catch (Exception exception) {
                    _logger.LogError(exception, "Failed to connect to simulator");
                    ConnectBtn.Content = "Connect";
                    StatusBox.Text = "Failed to connect to simulator";
                }

                ConnectBtn.IsEnabled = true;
            } else {
                ConnectBtn.IsEnabled = false;
                ConnectBtn.Content = "Disconnecting...";
                await _client.DisconnectSimAsync();
            }
        }
    }
}
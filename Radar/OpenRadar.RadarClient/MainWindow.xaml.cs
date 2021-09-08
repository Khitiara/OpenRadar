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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FreeRadar.Common;
using Microsoft.Extensions.Logging;

namespace OpenRadar.RadarClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly AtcClient           _client;

        public MainWindow(AtcClient client, ILogger<MainWindow> logger) {
            _logger = logger;
            _client = client;
            InitializeComponent();
        }

        private void MainWindow_OnSourceInitialized(object? sender, EventArgs e) {
            _logger.LogDebug("Main window win32 hooks ready, starting atc server");
            _client.Bind(this);
        }

        private async void MainWindow_OnClosed(object? sender, EventArgs e) {
            _logger.LogDebug("Stopping atc server");
            await _client.CloseAsync();
            _logger.LogDebug("Disposing client");
            _client.Dispose();
        }
    }
}
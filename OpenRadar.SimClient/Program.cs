using System;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Microsoft.Extensions.Hosting.Wpf;
using FreeRadar.Common;
using FreeRadar.Common.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace OpenRadar.SimClient
{
    public static class Program
    {
        [STAThread]
        public static async Task<int> Main() {
            ConsoleFixer.Run();
            PacketMarshaller.LoadSupportedPacketTypes();
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
            try {
                await new HostBuilder()
                    .UseSerilog(Log.Logger, true)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureWpf(wpfBuilder =>
                        wpfBuilder.UseApplication<App>()
                            .UseWindow<MainWindow>())
                    .UseWpfLifetime(ShutdownMode.OnMainWindowClose)
                    .UseConsoleLifetime()
                    .Build().RunAsync();
                return 0;
            }
            catch (Exception exception) {
                Log.Fatal(exception, "Unhandled exception in host processing");
                return exception.HResult;
            }
            finally {
                Crypt.DisposeRootCert();
            }
        }

        private static void ConfigureServices(IServiceCollection services) {
            services.AddSingleton<FreeRadarClient>()
                .AddSingleton(_ => IsolatedStorageFile.GetUserStoreForApplication());
        }
    }
}
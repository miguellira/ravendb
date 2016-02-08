using System;
using System.Diagnostics;
using Microsoft.AspNet.Hosting;
using Raven.Abstractions.Logging;
using Raven.Server.Config;
using Raven.Server.ServerWide;

namespace Raven.Server
{
    public class 
        RavenServer : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RavenServer));

        public readonly RavenConfiguration Configuration;

        public readonly ServerStore ServerStore;
        private IWebHost _webHost;

        public RavenServer(RavenConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            Configuration = configuration;
            if (Configuration.Initialized == false)
                throw new InvalidOperationException("Configuration must be initialized");

            ServerStore = new ServerStore(Configuration);
        }

        public void Initialize()
        {
            var sp = Stopwatch.StartNew();
            try
            {
                ServerStore.Initialize();
            }
            catch (Exception e)
            {
                Log.FatalException("Could not open the server store", e);
                throw;
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Server store started took {0:#,#;;0} ms", sp.ElapsedMilliseconds);
            }
            sp.Restart();

            try
            {
                _webHost = new WebHostBuilder()
                    .UseServer("Microsoft.AspNet.Server.Kestrel")
                    .UseUrls(Configuration.Core.ServerUrl)
                    .UseEnvironment(EnvironmentName.Production)
                    .UseCaptureStartupErrors(true)
                    .UseStartup<RavenServerStartup>()
                    .Build();
            }
            catch (Exception e)
            {
                Log.FatalException("Could not configure server", e);
                throw;
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("Configuring HTTP server took {0:#,#;;0} ms", sp.ElapsedMilliseconds);
            }

            try
            {
                _webHost.Start();
            }
            catch (Exception e)
            {
                Log.FatalException("Could not start server", e);
                throw;
            }
        }

        public void Dispose()
        {
            _webHost?.Dispose();
            ServerStore?.Dispose();
        }
    }
}
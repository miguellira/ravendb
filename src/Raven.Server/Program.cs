using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Raven.Abstractions.Logging;
using Raven.Server.Config;
using Raven.Server.Utils;

namespace Raven.Server
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        public static int Main(string[] args)
        {
            WelcomeMessage.Print();

            var configuration = new RavenConfiguration();
            if (args != null)
            {
                configuration.AddCommandLine(args);
            }
            configuration.Initialize();

            using (var server = new RavenServer(configuration))
            {
                try
                {
                    server.Initialize();

                    Console.WriteLine($"Listening to: {string.Join(", ", configuration.Core.ServerUrl)}");
                    Console.WriteLine("Server started, listening to requests...");

                    //TODO: Move the command line options to here
                    while (true)
                    {
                        if (Console.ReadLine() == "q")
                            break;

                        // Console.ForegroundColor++;
                    }
                    Log.Info("Server is shutting down");
                    return 0;
                }
                catch (ReflectionTypeLoadException ftle)
                {
                    Log.FatalException("Failed to initialize the server because of missing types", ftle);
                    Console.WriteLine("Failed to load types!");
                    for (int i = 0; i < ftle.Types.Length; i++)
                    {
                        Console.WriteLine(ftle.Types[i].FullName + " because " + ftle.LoaderExceptions[i]);

                        Log.FatalException("Could not load " + ftle.Types[i].FullName, ftle.LoaderExceptions[i]);
                    }
                    return -2;
                }
                catch (Exception e)
                {
                    Log.FatalException("Failed to initialize the server", e);
                    Console.WriteLine(e);
                    return -1;
                }
            }
        }
    }
}
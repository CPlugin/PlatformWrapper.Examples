using System;
using System.Reflection;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using Examples.ManagerDemos;
using Serilog;

namespace Examples
{
    public class App
    {
        static void Main(string[] args)
        {
            var cfg = new LoggerConfiguration().MinimumLevel.Verbose()
                                               .Enrich.FromLogContext()
                                               .Enrich.WithProperty("Application", Assembly.GetExecutingAssembly().GetName().Name)
                                                //.Enrich.WithProperty("Version", ThisAssembly.AssemblyInformationalVersion)
                                               .WriteTo.Console();

            Log.Logger = cfg.CreateLogger();

            try
            {
                Log.Information("--=[ Started ]=--");

                var res = ResultCode.OkNone;

                // Examples:

                //Pool.Basic100Threads.GetUsers();

                //var d = new Pool.LongRunning();
                //var d = new Dealer.Basic();
                var d = new Basic();

                d.Go();


                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var kki = Console.ReadKey(true);
                        if (kki.Key == ConsoleKey.Q)
                        {
                            d.Stop();
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, e.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}

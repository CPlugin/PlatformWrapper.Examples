using System;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using NLog;

namespace Examples
{
    public class App
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            Log.Info("--=[ Started ]=--");

            var res = ResultCode.OkNone;
            
            //Pool.Basic100Threads.GetUsers();

            //new Pool.LongRunning().Go();

            var d = new Dealer.Basic();
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

            return (int)res;
        }
    }
}
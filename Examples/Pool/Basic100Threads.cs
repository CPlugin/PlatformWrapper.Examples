using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NLog;

namespace Examples.Pool
{
    public class Basic100Threads
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void GetUsers()
        {
            var pool = new CPlugin.PlatformWrapper.MetaTrader4.ManagerPool(Constants.Server,
                                                                           Constants.Login,
                                                                           Constants.Password,
                                                                           (ctx, type, message, exception) =>
                                                                           {
                                                                               if (exception != null)
                                                                                   Log.Error(exception);
                                                                               else
                                                                                   Log.Info($"[{type}] {message}");
                                                                           },
                                                                           maxPollSize: 8) { };

            pool.Run();

            Parallel.For(0,
                         100,
                         (l, state) =>
                         {
                             using (var poolElement = pool.Get(TimeSpan.FromSeconds(3)))
                             {
                                 var sw = new Stopwatch();
                                 sw.Start();
                                 Log.Debug($"[{l}] requesting users...");
                                 var allUsers = poolElement.Data.Value.UsersRequest();
                                 sw.Stop();
                                 Log.Debug($"[{l}] {allUsers.Count} users requested within {sw.Elapsed}");
                             }
                         });
        }
    }
}

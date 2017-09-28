using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CPlugin.PlatformWrapper.MetaTrader4;
using NLog;

namespace Examples
{
    public static class ManagerPollExamples
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void GetUsers()
        {
            var pool = new ManagerPool(Constants.Server, Constants.Login, Constants.Password, maxPollSize: 8)
            {
                Logger = (ctx, type, message, exception) =>
                {
                    if (exception != null)
                        Log.Error(exception);
                    else
                        Log.Info($"[{type}] {message}");
                }
            };

            pool.Run();

            Parallel.For(0, 100, (l, state) =>
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

        public class LongRunninTest
        {
            const int ThreadsCount = 2;
            readonly ManualResetEvent _eventStop = new ManualResetEvent(false);

            public void Go()
            {
                var pool = new ManagerPool("127.0.0.1:1443", 1, "Manager")
                {
                    Logger = Extensions.LogToConsole
                };
                pool.Run();

                /*
                 * 64th
                 * 1: 1.08
                 * 2: 0.98
                 * 4: 1.11
                 * 8: 1.30
                 * 64: 1.15
                 */

                var tasks = new Task[ThreadsCount];

                //var symbols = new Queue<ConSymbol>();
                //var logins = new Queue<int>();

                var log = NLog.LogManager.GetCurrentClassLogger();
                log.Info("Started");


                var started = DateTime.Now;
                for (int i = 0; i < ThreadsCount; ++i)
                {
                    tasks[i] = new Task(a =>
                    {
                        do
                        {
                            var _log = LogManager.GetLogger("Thread_" + a);
                            try
                            {
                                _log.Debug("Started");

                                try
                                {
                                    using (var v = pool.Get(TimeSpan.FromSeconds(5)))
                                    {
                                        _log.Trace("Trying to get data");
                                        var mgr = v.Data.Value;
                                        var allUsers = mgr.UsersRequest();
                                        var allTrades = mgr.TradesRequest();
                                        var symbols = mgr.SymbolsGetAll();

                                        _log.Info("Fetched {0} users", allUsers.Count);
                                        _log.Info("Fetched {0} trades", allTrades.Count);
                                        _log.Info("Fetched {0} symbols", symbols.Count);

                                        if (false)
                                        {
                                            // v.Recreate(); // init reconnect routine
                                            mgr = v.Data.Value;
                                            allUsers = mgr.UsersRequest();
                                            foreach (var userRecord in allUsers)
                                            {
                                                mgr.TradesUserHistory(userRecord.Key, DateTime.MinValue, DateTime.MaxValue);
                                            }
                                        }
                                    }
                                }
                                catch (TimeoutException to)
                                {
                                    _log.Warn(to.Message);
                                }

                                _log.Debug("Complete");
                            }
                            catch (Exception ex)
                            {
                                _log.Error(ex);
                            }
                        } while (false == _eventStop.WaitOne(TimeSpan.FromMinutes(1)));
                    }, i);
                    tasks[i].Start();
                }
                log.Info("All tasks has started");

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var kki = Console.ReadKey(true);
                        if (kki.Key == ConsoleKey.Q)
                        {
                            _eventStop.Set();
                            break;
                        }
                    }
                }

                Task.WaitAll(tasks, TimeSpan.FromHours(1));
                log.Info("All tasks has completed");

                log.Info("Total time : {0}", DateTime.Now - started);
            }

        }
    }

    public static class ManagerDealerExamples
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void GetUsers()
        {
            var pump = new ManagerPump(Constants.Server, Constants.Login, Constants.Password)
            {
                Logger = (ctx, type, message, exception) =>
                {
                    if (exception != null)
                        Log.Error(exception);
                    else
                        Log.Info($"[{type}] {message}");
                }
            };

            var dealer = new ManagerDealer(pump);

            dealer.Connect();

            dealer.ChangeDealingStatus += (sender, activate) =>
            {
                Log.Debug($"ChangeDealingStatus: {activate}");
            };

            dealer.DealingRequest += (sender, req) =>
            {
                Log.Debug($"DealingRequest: #{req.Id} '{req.Login}' {req.Trade}");

                //depends on logic do:
                Log.Debug($"DealerSend: {sender.reject(req)}");
                //or
                //Log.Debug($"DealerSend: {sender.DealerReject()}");
                //etc
            };
        }
    }
}
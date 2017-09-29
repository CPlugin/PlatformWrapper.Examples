using System;
using System.Threading;
using System.Threading.Tasks;
using CPlugin.PlatformWrapper.MetaTrader4;
using NLog;

namespace Examples.Pool
{
    public class LongRunning
    {
        const int ThreadsCount = 2;
        readonly ManualResetEvent _eventStop = new ManualResetEvent(false);

        public void Go()
        {
            var pool = new ManagerPool(Constants.Server, Constants.Login, Constants.Password)
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

            var log = LogManager.GetCurrentClassLogger();
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

            Task.WaitAll(tasks, TimeSpan.FromHours(1));
            log.Info("All tasks has completed");

            log.Info("Total time : {0}", DateTime.Now - started);
        }
    }
}
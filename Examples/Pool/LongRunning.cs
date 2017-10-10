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
        readonly Task[] _tasks = new Task[ThreadsCount];
        readonly ManualResetEvent _eventStop = new ManualResetEvent(false);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void Go()
        {
            Log.Info("Started");

            var poolLog = LogManager.GetLogger("Pool");
            var pool = new ManagerPool(Constants.Server, Constants.Login, Constants.Password)
            {
                Logger = (ctx, type, message, exception) => poolLog.Trace($"{type} {message} {exception}")
            };

            var lastOpDateTime = DateTime.MinValue;
            var rnd = new Random();

            // very important step to run pool
            pool.Run();

            for (int i = 0; i < ThreadsCount; ++i)
            {
                _tasks[i] = new Task(a =>
                {
                    do
                    {
                        var _log = LogManager.GetLogger("Thread_" + a);
                        try
                        {

                            try
                            {
                                using (var v = pool.Get(TimeSpan.FromSeconds(5)))
                                {
                                    // randomly await from 1 to 5 minutes
                                    if (DateTime.Now - lastOpDateTime > TimeSpan.FromMinutes(rnd.Next(1, 5)))
                                    {
                                        lastOpDateTime = DateTime.Now;

                                        _log.Trace("Trying to get data");
                                        var mgr = v.Data.Value;
                                        var allUsers = mgr.UsersRequest();
                                        var allTrades = mgr.TradesRequest();
                                        var symbols = mgr.SymbolsGetAll();

                                        _log.Info("Fetched {0} users", allUsers.Count);
                                        _log.Info("Fetched {0} trades", allTrades.Count);
                                        _log.Info("Fetched {0} symbols", symbols.Count);
                                    }
                                }
                            }
                            catch (TimeoutException to)
                            {
                                _log.Warn(to.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }
                    } while (false == _eventStop.WaitOne(TimeSpan.FromSeconds(1)));
                }, i);
                _tasks[i].Start();
            }
            Log.Info("All tasks started");
        }

        public void Stop()
        {
            _eventStop.Set();

            Task.WaitAll(_tasks, TimeSpan.FromSeconds(10));
            Log.Info("All tasks completed");
        }
    }
}
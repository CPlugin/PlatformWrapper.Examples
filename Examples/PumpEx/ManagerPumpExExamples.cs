using System;
using System.Collections.Generic;
using System.Threading;
using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Classes;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using Serilog;

namespace Examples.PumpEx
{
    public class Basic
    {
        protected readonly ILogger Log = Serilog.Log.Logger.ForContext<Basic>();

        /// <summary>
        /// Simple demonstration of connection process
        /// </summary>
        /// <returns></returns>
        public ResultCode Connect()
        {
            ResultCode result;

            // create instance
            var mgr = new ManagerPumpEx(Constants.Server,
                                        Constants.Login,
                                        Constants.Password,
                                        (ctx, type, message, exception) =>
                                        {
                                            if (exception != null)
                                                Log.Error(exception, exception.Message);
                                            else
                                                Log.Information($"[{type}] {message}");
                                        })
            {
                PumpingFlags = PumpingFlags.HideNews | PumpingFlags.HideMail,
            };

            mgr.Start += sender =>
            {
                Log.Information("Pumping started");
            };

            mgr.Stop += sender =>
            {
                Log.Information("Pumping stopped");
            };


            Log.Debug("Connect...");
            result = mgr.Connect();
            Log.Debug($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return result;

            // (optional) wait until pumping connection is up to step forward with 'Get*' functions
            if (mgr.Await(PumpingCode.StartPumping, TimeSpan.FromSeconds(5)) == null)
            {
                Log.Error($"There is not connection to MT4");
                return ResultCode.NoConnect;
            }

            // be sure pumping mode activated and all data get synchronized ebfore call any functions

            if ((result = mgr.SymbolGet("EURUSD", out var cs)) != ResultCode.Ok)
                Log.Error($"Error gettings symnbol info: {result}");

            // simulate long running
            Thread.Sleep(5000);

            Log.Debug("Disconnect...");
            result = mgr.Disconnect();
            Log.Debug($"Disconnect result: {result}");

            return result;
        }

        /// <summary>
        /// Demonstration of new order opening process using regular and pumping connection simultaneously
        /// </summary>
        /// <returns></returns>
        public ResultCode OpenTrade()
        {
            ResultCode result;

            var prices = new Dictionary<string, SymbolInfo>();

            var mgr = new Manager(Constants.Server,
                                  Constants.Login,
                                  Constants.Password,
                                  (ctx, type, message, exception) =>
                                  {
                                      if (exception != null)
                                          Log.Error(exception, exception.Message);
                                      else
                                          Log.Information($"[{type}] {message}");
                                  });

            Log.Debug("Connect...");
            result = mgr.Connect();
            Log.Debug($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return result;

            // create instance
            var pump = new ManagerPumpEx(Constants.Server,
                                         Constants.Login,
                                         Constants.Password,
                                         (ctx, type, message, exception) =>
                                         {
                                             if (exception != null)
                                                 Log.Error(exception, exception.Message);
                                             else
                                                 Log.Information($"[{type}] {message}");
                                         }) { };

            pump.Start += sender =>
            {
                Log.Information("Pumping started");
            };

            pump.Stop += sender =>
            {
                Log.Information("Pumping stopped");
            };

            pump.BidAsk += sender =>
            {
                lock (prices)
                {
                    // get last received quotes
                    foreach (var pi in sender.SymbolInfoUpdated())
                    {
                        // Debug.Print($"{DateTime.Now} - {pi.Key}, {pi.Value.Bid}");

                        prices[pi.Key] = pi.Value;
                    }
                }
            };


            Log.Debug("Connect...");
            result = pump.Connect();
            Log.Debug($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return result;

            try
            {
                // await for prices
                do
                {
                    Thread.Sleep(100);

                    lock (prices)
                    {
                        SymbolInfo si;
                        if (!prices.TryGetValue("EURUSD", out si))
                            continue;

                        break;
                    }
                } while (true);

                var tti = new TradeTransInfo()
                {
                    Price = prices["EURUSD"].Ask,
                    Comment = "opened by demo app",
                    OrderBy = 1000, // put there client account number
                    Symbol = "EURUSD",
                    Volume = 10, // 0.1 lots
                    TradeCommand = TradeCommand.Buy,
                    TradeTransactionType = TradeTransactionType.BrOpen
                };

                Log.Debug("TradeTransaction...");
                result = mgr.TradeTransaction(ref tti);
                Log.Debug($"TradeTransaction result: {result}");

                if (result != ResultCode.Ok)
                    return result;
            }
            finally
            {
                Log.Debug("Disconnect...");
                result = pump.Disconnect();
                Log.Debug($"Disconnect result: {result}");

                Log.Debug("Disconnect...");
                result = mgr.Disconnect();
                Log.Debug($"Disconnect result: {result}");
            }

            return result;
        }
    }
}

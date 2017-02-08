﻿using System.Collections.Generic;
using System.Threading;
using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Classes;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using NLog;

namespace Examples
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string Server = "127.0.0.1:1443";
        private const int Login = 1;
        private const string Password = "Manager";

        static int Main(string[] args)
        {
            Log.Info("--=[ Started ]=--");

            //return (int)Connect();
            return (int) OpenTrade();
        }

        /// <summary>
        /// Simple demonstration of connection process
        /// </summary>
        /// <returns></returns>
        private static ResultCode Connect()
        {
            ResultCode result;

            // create instance
            var mgr = new ManagerPumpEx(Server, Login, Password)
            {
                Logger = (ctx, type, message, exception) =>
                {
                    if (exception != null)
                        Log.Error(exception);
                    else
                        Log.Info($"[{type}] {message}");
                },
                OnStart = sender => { Log.Info("Pumping started"); },
                OnStop = sender => { Log.Info("Pumping stopped"); }
            };

            Log.Trace("Connect...");
            result = mgr.Connect();
            Log.Trace($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return result;

            // simulate long running
            Thread.Sleep(5000);

            Log.Trace("Disconnect...");
            result = mgr.Disconnect();
            Log.Trace($"Disconnect result: {result}");

            return result;
        }

        /// <summary>
        /// Demonstration of new order opening process using regular and pumping connection simultaneously
        /// </summary>
        /// <returns></returns>
        private static ResultCode OpenTrade()
        {
            ResultCode result;

            var prices = new Dictionary<string, SymbolInfo>();

            var mgr = new Manager(Server, Login, Password);
            Log.Trace("Connect...");
            result = mgr.Connect();
            Log.Trace($"Connect result: {result}");

            mgr.KeepAlive();

            if (result != ResultCode.Ok)
                return result;

            // create instance
            var pump = new ManagerPumpEx(Server, Login, Password)
            {
                Logger = (ctx, type, message, exception) =>
                {
                    if (exception != null)
                        Log.Error(exception);
                    else
                        Log.Info($"[{type}] {message}");
                },
                OnStart = sender => { Log.Info("Pumping started"); },
                OnStop = sender => { Log.Info("Pumping stopped"); },
                OnBidAsk = sender =>
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
                }
            };

            Log.Trace("Connect...");
            result = pump.Connect();
            Log.Trace($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return result;

            try
            {
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

                Log.Trace("TradeTransaction...");
                result = mgr.TradeTransaction(ref tti);
                Log.Trace($"TradeTransaction result: {result}");

                if (result != ResultCode.Ok)
                    return result;
            }
            finally
            {
                Log.Trace("Disconnect...");
                result = pump.Disconnect();
                Log.Trace($"Disconnect result: {result}");

                Log.Trace("Disconnect...");
                result = mgr.Disconnect();
                Log.Trace($"Disconnect result: {result}");
            }

            return result;
        }
    }
}
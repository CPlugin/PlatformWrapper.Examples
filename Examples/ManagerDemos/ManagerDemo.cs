using System;
using System.Linq;
using CPlugin.Common;
using CPlugin.Common.Extensions;
using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using Serilog;
using CPlugin.PlatformWrapper.MetaTrader4.Classes;

namespace Examples.ManagerDemos
{
    public class Basic
    {
        private          Manager mgr;
        private readonly ILogger Log = Serilog.Log.Logger.ForContext<Basic>();

        public void Go()
        {
            mgr = new Manager(Constants.Server,
                              Constants.Login,
                              Constants.Password,
                              (ctx, type, message, exception) =>
                              {
                                  if (exception != null)
                                      Log.Error(exception, exception.Message);
                                  else
                                      Log.Information($"[{type}] {message}");
                              });

            Log.Information("Connect...");
            var result = mgr.Connect();
            Log.Information($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return;

            Log.Information("ManagerCommon...");
            result = mgr.ManagerCommon(out var conCommon);
            Log.Information($"ManagerCommon result: {result}");

            if (result != ResultCode.Ok)
                return;

            var bResult = mgr.UsersRequest(out var users);
            Log.Information($"UsersRequest result: {bResult}, {users.Count} users found");
            // etc

            var serverTime = mgr.ServerTime();
            Log.Information("ServerTime: {DateTime}", serverTime);

            foreach (var user in users.Values)
            {
                if (!mgr.TradesUserHistory(1000, 0.FromUnixtime(), serverTime, out var orders))
                {
                    Log.Error($"TradesUserHistory({user.Login} failed");
                    continue;
                }

                // get all balance orders
                var balanceOrders = orders.Where(o => o.Value.TradeCommand == TradeCommand.Balance);

                break;
            }

            mgr.MarginLevelRequest(1000, out var ml);
            Log.Information("MarginLevel of '{Login}': {Level}", ml.Login, ml.Level);

        }

        public void Stop()
        {
            if (mgr != null && mgr.IsConnected())
                mgr.Disconnect();
        }
    }
}

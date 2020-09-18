using System;
using System.Linq;
using CPlugin.Common;
using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using NLog;
using CPlugin.PlatformWrapper.MetaTrader4.Classes;

namespace Examples.ManagerDemos
{
    public class Basic
    {
        private                   Manager mgr;
        protected static readonly Logger  Log = LogManager.GetCurrentClassLogger();

        public void Go()
        {
            mgr = new Manager(Constants.Server,
                              Constants.Login,
                              Constants.Password,
                              (ctx, type, message, exception) =>
                              {
                                  if (exception != null)
                                      Log.Error(exception);
                                  else
                                      Log.Info($"[{type}] {message}");
                              }) { };

            Log.Info("Connect...");
            var result = mgr.Connect();
            Log.Info($"Connect result: {result}");

            if (result != ResultCode.Ok)
                return;

            Log.Info("ManagerCommon...");
            result = mgr.ManagerCommon(out var conCommon);
            Log.Info($"ManagerCommon result: {result}");

            if (result != ResultCode.Ok)
                return;

            var bResult = mgr.UsersRequest(out var users);
            Log.Info($"UsersRequest result: {bResult}, {users.Count} users found");
            // etc

            var serverTime = mgr.ServerTime();

            foreach (var user in users.Values)
            {
                if (!mgr.TradesUserHistory(1000, TimeConverter.FromUnixtime(0), serverTime, out var orders))
                {
                    Log.Error($"TradesUserHistory({user.Login} failed");
                    continue;
                }

                // get all balance orders
                var balanceOrders = orders.Where(o => o.Value.TradeCommand == TradeCommand.Balance);
            }
        }

        public void Stop()
        {
            if (mgr != null && mgr.IsConnected())
                mgr.Disconnect();
        }
    }
}

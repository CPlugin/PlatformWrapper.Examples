using System;
using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using NLog;

namespace Examples.Dealer
{
    public class Basic
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        ManagerPump pump;
        ManagerDealer dealer;

        public void Go()
        {
            pump = new ManagerPump(Constants.Server, Constants.Login, Constants.Password)
            {
                Logger = Extensions.LogToConsole
            };

            dealer = new ManagerDealer(pump)
            {
                Logger = Extensions.LogToConsole
            };

            dealer.Connect();

            dealer.Status += (sender, activate) => { Log.Debug($"ChangeDealingStatus: {activate}"); };

            dealer.Request += (sender, req) =>
            {
                Log.Debug($"Request: #{req.Id} '{req.Login}' {req.Trade}");

                switch (new Random().Next(0, 4))
                {
                    case 0:
                        //depends on logic do:
                        Log.Debug($"Confirm: {sender.Confirm(req)}");
                        break;

                    case 1:
                        // or reject
                        Log.Debug($"Reject: {sender.Reject(req)}");
                        break;

                    case 2:
                        // or requote
                        req.Prices[0] -= 0.0001;
                        req.Prices[1] += 0.0001;
                        Log.Debug($"Requote: {sender.Requote(req, DealerConfirmMode.None)}");
                        break;

                    case 3:

                        // bypass request back to queue
                        Log.Debug($"Reset: {sender.Reset(req)}");
                        break;
                }
            };
        }

        public void Stop()
        {
            dealer.Dispose();
            pump.Dispose();
        }
    }
}
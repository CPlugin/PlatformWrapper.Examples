using System;
using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using Serilog;

namespace Examples.Dealer
{
    public class Basic
    {
        private readonly ILogger Log = Serilog.Log.Logger.ForContext<Basic>();
        ManagerPump                     pump;
        ManagerDealer                   dealer;

        public void Go()
        {
            pump = new ManagerPump(Constants.Server,
                                   Constants.Login,
                                   Constants.Password,
                                   (ctx, type, message, exception) =>
                                   {
                                       if (exception != null)
                                           Log.Error(exception, exception.Message);
                                       else
                                           Log.Information($"[{type}] {message}");
                                   }) { };

            dealer = new ManagerDealer(Constants.Server,
                                       Constants.Login,
                                       Constants.Password,
                                       (ctx, type, message, exception) =>
                                       {
                                           if (exception != null)
                                               Log.Error(exception, exception.Message);
                                           else
                                               Log.Information($"[{type}] {message}");
                                       }) { };

            dealer.Connect();

            dealer.Status += (sender, activate) =>
            {
                Log.Debug($"ChangeDealingStatus: {activate}");
            };

            dealer.NewRequest += () =>
            {
                ResultCode res;
                var        dealerRequestGet = dealer.DealerRequestGet(out var req);

                Log.Debug($"Request: #{req.Id} '{req.Login}' {req.Trade}");

                switch (new Random().Next(0, 4))
                {
                    case 0:
                        //depends on logic do:
                        Log.Debug($"Confirm: {dealer.Confirm(ref req)}");
                        break;

                    case 1:
                        // or reject
                        Log.Debug($"Reject: {dealer.Reject(req)}");
                        break;

                    case 2:
                        // or requote
                        req.Prices[0] -= 0.0001;
                        req.Prices[1] += 0.0001;
                        Log.Debug($"Requote: {dealer.Requote(ref req)}");
                        break;

                    case 3:

                        // bypass request back to queue
                        Log.Debug($"Reset: {dealer.Reset(req)}");
                        break;
                }

                return true;
            };
        }

        public void Stop()
        {
            dealer.Dispose();
            pump.Dispose();
        }
    }
}

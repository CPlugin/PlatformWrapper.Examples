using CPlugin.PlatformWrapper.MetaTrader4;
using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using NLog;

namespace Examples.ManagerDemos
{
    public class Basic
    {
        private                   Manager mgr;
        protected static readonly Logger  Log = LogManager.GetCurrentClassLogger();

        public void Go()
        {
            mgr = new Manager(Constants.Server, Constants.Login, Constants.Password)
            {
                Logger = (ctx, type, message, exception) =>
                {
                    if (exception != null)
                        Log.Error(exception);
                    else
                        Log.Info($"[{type}] {message}");
                }
            };

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

            // etc
        }

        public void Stop()
        {
            if (mgr != null && mgr.IsConnected())
                mgr.Disconnect();
        }
    }
}

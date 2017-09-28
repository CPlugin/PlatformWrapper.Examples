using CPlugin.PlatformWrapper.MetaTrader4.Enums;
using NLog;

namespace Examples
{
    public class App
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            Log.Info("--=[ Started ]=--");

            var res = ResultCode.OkNone;

            //res = BasicExamples.OpenTrade();

            ManagerPollExamples.GetUsers();

            return (int)res;
        }
    }
}
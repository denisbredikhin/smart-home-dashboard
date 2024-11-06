using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThermostatSetpointsWatcher.Core;
using ViessmannControl;

namespace ThermostatSetpointsWatcher
{
    class Program
    {
        private static void Log(string format, params object[] arg)
        {
            Console.WriteLine(DateTime.Now + ": " + format, arg);
        }

        static async Task Main(string[] args)
        {
            var comPort = args[0];
            var synchronizer = new TadoViessmanSynchronizer(comPort);
            while (true)
            {
                try
                {
                    await synchronizer.SynchronizeHouseTemperature();
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.Message}");
                    Log($"Stacktrace: {ex.StackTrace}");
                }
            }
        }
    }
}

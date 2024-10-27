using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViessmannControl;

namespace ThermostatSetpointsWatcher
{
    class Program
    {
        private static double currentMaxValue = -1;
        //private const string ComPort = "COM1";

        private static void Log(string format, params object[] arg)
        {
            Console.WriteLine(DateTime.Now + ": " + format, arg);
        }

        static async Task Main(string[] args)
        {
            var comPort = args[0];
            while (true)
            {
                try
                {
                    var roomsStatus = await Tado.GetRooms();
                    var roomWithMaxTemp = roomsStatus.Where(s => s.Setting.Power == "ON")
                        .MaxBy(s => s.Setting.Temperature);

                    double currentValue;
                    if (roomWithMaxTemp == null)
                    {
                        Log("Heating is off in all the rooms");
                        currentValue = 5;
                    }
                    else
                    {
                        currentValue = roomWithMaxTemp.Setting.Temperature.Value.Value;
                        Log("Current max value is: {0} in the room {0}", currentValue, roomWithMaxTemp.Name);
                    }

                    if (currentValue > 40)
                    {
                        Log("Current max value is more than 40, so it will be ignored");
                        Thread.Sleep(TimeSpan.FromMinutes(5));
                        continue;
                    }

                    if (Math.Abs(currentValue - currentMaxValue) < 0.1)
                    {
                        Thread.Sleep(TimeSpan.FromMinutes(5));
                        continue;
                    }

                    Log("Current max value {0} is different from old value {1}, so current value will be sent to boiler", currentValue,
                        currentMaxValue);
                    currentMaxValue = currentValue;

                    var boiler = new Protocol300Boiler(new Protocol300BoilerConnectionConfiguration()
                    {
                        PortName = comPort,
                        BaudRate = 4800,
                        DataBits = 8,
                        Parity = Parity.Even,
                        StopBits = StopBits.Two
                    });
                    boiler.Connect(() => {
                        var currentValueByte = (byte)Math.Ceiling(currentValue);
                        Log("Connected to boiler.");
                        Thread.Sleep(1000);
                        Log("About to set room temperature: {0}", currentValueByte);
                        boiler.SetRoomTemperatureStandard(currentValueByte);
                        Log("Room temperature is set, about to disconnect from boiler");
                        boiler.Disconnect();
                        Log("Disconnected from boiler");
                    });
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

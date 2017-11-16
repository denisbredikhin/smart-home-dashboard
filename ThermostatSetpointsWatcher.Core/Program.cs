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
        private static float currentMaxValue = -1;
        //private const string ComPort = "COM1";

        private static void Log(string format, params object[] arg)
        {
            Console.WriteLine(DateTime.Now + ": " + format, arg);
        }

        static void Main(string[] args)
        {
            var comPort = args[0];
            while (true)
            {
                try
                {
                    var commandText = "SELECT Max(CONVERT(float, REPLACE([Value], ',', '.')))" +
                                      " FROM [DeviceCurrentValues]" +
                                      " where ValueName = 'Heating 1'";

                    float currentValue = -1;
                    using (var connection = new SqlConnection(Constants.ConnectionString))
                    {
                        try
                        {
                            connection.Open();
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = commandText;
                                currentValue = Convert.ToSingle(command.ExecuteScalar());
                            }
                        }
                        finally
                        {
                            if (connection.State == ConnectionState.Open)
                                connection.Close();
                            connection.Dispose();
                        }
                    }

                    Log("Current max value is: {0}", currentValue);
                    if (currentValue > 40)
                    {
                        Log("Current max value is more than 40, so it will be ignored");
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                        continue;
                    }

                    if (Math.Abs(currentValue - currentMaxValue) < 0.1)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(30));
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
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    Log(string.Format("Error: {0}", ex.Message));
                    Log(string.Format("Stacktrace: {0}", ex.StackTrace));
                }
            }
        }
    }
}

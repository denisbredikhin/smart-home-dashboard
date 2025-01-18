using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViessmannControl;

namespace ThermostatSetpointsWatcher.Core
{
    public class TadoViessmanSynchronizer(string comPort, ILogger<TadoViessmanSynchronizer> logger)
    {
        private static double currentMaxValue = -1;

        public async Task SynchronizeHouseTemperature() 
        {
            var roomsStatus = await Tado.GetRooms();
            var roomWithMaxTemp = roomsStatus.Where(s => s.Setting.Power == "ON")
                .MaxBy(s => s.Setting.Temperature.Value);

            double currentValue;
            if (roomWithMaxTemp == null)
            {
                logger.LogInformation("Heating is off in all the rooms");
                currentValue = 5;
            }
            else
            {
                currentValue = roomWithMaxTemp.Setting.Temperature.Value.Value;
                logger.LogInformation("Current max value is: {CurrentValue} in the room {RoomName}", currentValue, roomWithMaxTemp.Name);
            }

            if (currentValue > 40)
            {
                logger.LogInformation("Current max value is more than 40, so it will be ignored");
                return;
            }

            if (Math.Abs(currentValue - currentMaxValue) < 0.1)
                return;

            logger.LogInformation("Current max value {currentValue} is different from old value {currentMaxValue}, so current value will be sent to boiler", currentValue,
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
            var tcs = new TaskCompletionSource();
            boiler.Connect(() => {
                var currentValueByte = (byte)Math.Ceiling(currentValue);
                logger.LogInformation("Connected to boiler.");
                Thread.Sleep(1000);
                logger.LogInformation("About to set room temperature: {currentValueByte}", currentValueByte);
                boiler.SetRoomTemperatureStandard(currentValueByte);
                logger.LogInformation("Room temperature is set, about to disconnect from boiler");
                boiler.Disconnect();
                logger.LogInformation("Disconnected from boiler");
                tcs.SetResult();
            }, e => {
                logger.LogError(e, "Error during connecting to boiler");
                tcs.SetResult();
            });
            await tcs.Task;
        }
    }
}

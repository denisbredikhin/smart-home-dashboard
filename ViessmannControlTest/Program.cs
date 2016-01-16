using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using ViessmannControl;
using V_comm_DLL;

namespace ViessmannControlTest
{
	class Program
	{
		private const string ComPort = "COM42";
		
		private static void Main(string[] args)
		{
			/*var comm = new v_comm();
			comm.V_Port = 1;
			comm.V_Open();
			comm.VCommEvent += (ref V_Event id) => Console.WriteLine("Event: "+id.ToString());
			if (!comm.V_IsOpen)
				Console.WriteLine("Error open port.");
			else {
				Console.WriteLine("Version: "+comm.V_Get_Version);
				foreach (Vito_Query vq in Enum.GetValues(typeof (Vito_Query))) {
					var v = comm.V_Get_Reading(vq);
					Console.WriteLine(vq.ToString()+": "+v);
				}

			}
			Console.ReadKey();*/

			ChangeCurrentTemp();
				return;

			//var task = Task.Factory.StartNew(UsbIpWatchDog);

			//var task2 = Task.Factory.StartNew(UsbIpWatchDog);

			var form = new MainForm(new Protocol300BoilerConnectionConfiguration() {
				PortName = ComPort,
				BaudRate = 4800,
				DataBits = 8,
				Parity = Parity.Even,
				StopBits = StopBits.Two
			});
			form.ShowDialog();

			/*var usbipProcess = Process.GetProcessesByName("usbip").FirstOrDefault();
			if (usbipProcess != null)
				usbipProcess.Kill();

			while (Process.GetProcessesByName("usbip").Length > 0)
				Thread.Sleep(100);*/
		}

		private static void ChangeCurrentTemp()
		{
			var boiler = new Protocol300Boiler(new Protocol300BoilerConnectionConfiguration() {
				PortName = ComPort,
				BaudRate = 4800,
				DataBits = 8,
				Parity = Parity.Even,
				StopBits = StopBits.Two
			});
			boiler.Connect(() => {
				Thread.Sleep(1000);
				boiler.SetRoomTemperatureStandard(21);
				boiler.Disconnect();
			});
			Thread.Sleep(TimeSpan.FromMinutes(5));
		}

		private static void UsbIpWatchDog()
		{
			while (true) {
				Thread.Sleep(TimeSpan.FromSeconds(3));
				// check that com port exits
				if (SerialPort.GetPortNames().Contains(ComPort)) {
					continue;
				}

				// check if usbip is started
				var usbipProcess = Process.GetProcessesByName("usbip").FirstOrDefault();
				if (usbipProcess!=null)
					usbipProcess.Kill();
				while (Process.GetProcessesByName("usbip").Length>0)
					Task.Delay(10);

				Process.Start(new ProcessStartInfo(@"D:\Soft\usbip_windows_v0.2.0.0_signed\usbip.exe", "-a 192.168.0.157 1-1") {
					WorkingDirectory = @"D:\Soft\usbip_windows_v0.2.0.0_signed"
				});
				Thread.Sleep(TimeSpan.FromSeconds(10));
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViessmannControl;
using V_comm_DLL;

namespace ViessmannControlTest
{
	class Program
	{
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

			var form = new MainForm(new Protocol300BoilerConnectionConfiguration() {
				PortName = "COM1",
				BaudRate = 4800,
				DataBits = 8,
				Parity = Parity.Even,
				StopBits = StopBits.Two
			});
			form.ShowDialog();
		}
	}
}

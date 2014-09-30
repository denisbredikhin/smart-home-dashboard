using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViessmannControl
{
	public class Protocol300BoilerConnectionConfiguration
	{
		public string PortName { get; set; }

		public int BaudRate { get; set; }

		public Parity Parity { get; set; }

		public int DataBits { get; set; }

		public StopBits StopBits { get; set; }
	}
}

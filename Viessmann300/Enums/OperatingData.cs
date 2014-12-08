using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViessmannControl
{
	public enum OperatingData
	{
		StandbyMode = 0x00,
		DhwOnly = 0x01,
		CentralHeatingAndDhw = 0x02,
		PermanentlyReducedOperation = 0x03,
		PermanentlyNormalOperation = 0x04
	}
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HomeDashboard
{
	public partial class Thermostats : System.Web.UI.Page
	{
		protected string tableHtml;

		private string batteryHtml = "<div class=\"container\">"+
    "<div id=\"batteryBody\">"+
        "<div id=\"indicator\" style=\"width:{0}%\">"+
				"{0}%"+
         "</div>"+
    "</div>"+
    "<div class=\"batteryEnd\">"+
        "<div>"+
         
        "</div>"+
    "</div>"+

"</div>";

		protected void Page_Load(object sender, EventArgs e)
		{
			var now = DateTime.Today;
			var commandText = string.Format(
				"SELECT [DeviceName], [ValueName], [Value], [TimeStamp] FROM [DeviceCurrentValues]");
			var tempByDevice = new Dictionary<string, double>();
			var batteryByDevice = new Dictionary<string, double>();
			var devices = new HashSet<string>();
			using (var connection = new SqlConnection("Server=localhost;Database=HomeData;Trusted_Connection=True;")) {
				try {
					connection.Open();
					using (var command = connection.CreateCommand()) {
						command.CommandText = commandText;
						using (var reader = command.ExecuteReader()) {
							while (reader.Read()) {
								var deviceName = reader.GetString(0);
								var valuesName = reader.GetString(1);
								var timeStamp = reader.GetDateTime(3);

								devices.Add(deviceName);
								switch (valuesName) {
									case "Heating 1":
										tempByDevice.Add(deviceName, double.Parse(reader.GetString(2)));
										break;
									case "Battery Level":
										batteryByDevice.Add(deviceName, double.Parse(reader.GetString(2)));
										break;
								}
							}
							reader.Close();

						}
					}
				}
				finally {
					if (connection.State==ConnectionState.Open)
						connection.Close();
					connection.Dispose();
				}
			}
			var sortedDevices = devices.ToList();
			sortedDevices.Sort();

			var sb = new StringBuilder();

			foreach (var device in sortedDevices) {
				sb.Append("<tr>");
				sb.AppendFormat("<td>{0}</td>", device);
				sb.AppendFormat("<td>{0}</td>", tempByDevice.ContainsKey(device) ? tempByDevice[device].ToString() + "&deg;C" : "?");
				sb.AppendFormat("<td>{0}</td>", string.Format(batteryHtml, (batteryByDevice.ContainsKey(device) ? batteryByDevice[device].ToString() : "0")));
				sb.Append("</tr>");

			}
			tableHtml = sb.ToString();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HomeDashboard
{
	public partial class MetersRegistration : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			//ScriptManager.ScriptResourceMapping.AddDefinition("jquery",
			//new ScriptResourceDefinition {
			//	Path = "~/scripts/jquery-2.1.0.min.js",
			//	DebugPath = "~/scripts/jquery-2.1.0.js",
			//	CdnPath = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-2.1.0.min.js",
			//	CdnDebugPath = "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-2.1.0.js"
			//});

		}

		protected void okButton_Click(object sender, EventArgs e)
		{
			var now = DateTime.Today;
			var commandText = string.Format(
				"INSERT INTO [Meters]" +
					" ([Date], [Gas], [ElectricityDay], [ElectricityNight], [Water])" +
				" VALUES" +
				" (@date, @gas, @electrDay, @electrNight, @water)");
			var maxDateCommandText = "SELECT MAX([Date]) FROM [Meters]";

			using (var connection = new SqlConnection("Server=localhost;Database=HomeData;Trusted_Connection=True;")) {
				try {
					connection.Open();
					DateTime lastDate = now;
					using (var command = connection.CreateCommand()) {
						command.CommandText = maxDateCommandText;

						lastDate = (DateTime)command.ExecuteScalar();
					}

					if ((now.Date-lastDate.Date).TotalDays>1) {
						for (var d = lastDate.Date.AddDays(1); d.Date<now.Date;d = d.AddDays(1)) {
							using (var command = connection.CreateCommand()) {
								command.CommandText = commandText;
								command.Parameters.AddWithValue("@date", d);
								command.Parameters.AddWithValue("@gas", DBNull.Value);
								command.Parameters.AddWithValue("@electrDay", DBNull.Value);
								command.Parameters.AddWithValue("@electrNight", DBNull.Value);
								command.Parameters.AddWithValue("@water", DBNull.Value);
								command.ExecuteNonQuery();
							}
						}
					}

					using (var command = connection.CreateCommand()) {
						command.CommandText = commandText;
						command.Parameters.AddWithValue("@date", now);
						command.Parameters.AddWithValue("@gas", double.Parse(gas.Text));
						command.Parameters.AddWithValue("@electrDay", double.Parse(electrDay.Text));
						command.Parameters.AddWithValue("@electrNight", double.Parse(electrNight.Text));
						command.Parameters.AddWithValue("@water", double.Parse(water.Text));
						command.ExecuteNonQuery();
					}
				}
				finally {
 					if (connection.State==ConnectionState.Open)
						connection.Close(); 
					connection.Dispose();
				}
			}
		}
	}
}
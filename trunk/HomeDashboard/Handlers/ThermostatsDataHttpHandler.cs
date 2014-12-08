using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace HomeDashboard
{
	public class ThermostatsDataHttpHandler : IHttpHandler
	{
		public bool IsReusable {
			get { return true; }
		}

		private string DataTableToJSON(DataTable table)
		{
			var list = new List<Dictionary<string, object>>();

			foreach (DataRow row in table.Rows) {
				var dict = new Dictionary<string, object>();

				foreach (DataColumn col in table.Columns) {
					var obj = row[col];
					if (col.ColumnName=="Value")
						obj = double.Parse(obj.ToString());
					dict[col.ColumnName] = obj;
				}
				list.Add(dict);
			}
			var serializer = new JavaScriptSerializer();
			return serializer.Serialize(list);
		}

		public void ProcessRequest(HttpContext context)
		{
			var response = context.Response;
			response.ContentType = "text/json";

			var commandText = string.Format(
				"SELECT [DeviceName], [ValueName], [Value] FROM [DeviceCurrentValues] where [ValueName] in ('Heating 1', 'Battery Level')");
			var dt = new DataTable();

			using (var connection = new SqlConnection(Constants.ConnectionString)) {
				try {
					connection.Open();
					using (var da = new SqlDataAdapter(commandText, connection)) {
						da.Fill(dt);
					}
				}
				finally {
					if (connection.State == ConnectionState.Open)
						connection.Close();
					connection.Dispose();
				}
				response.Write(DataTableToJSON(dt));
			}
		}
	}
}
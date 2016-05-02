using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HomeDashboard
{
	public partial class PriceAllDaily : System.Web.UI.Page
	{
		protected string chartDataGas;
		protected string chartDataElectricity;
		protected string chartDataWater;
		protected string flagsData;
		protected string minYear;
		protected string minMonth;
		protected string minDay;

		private const double priceGas = 0.726;
		private const double priceElectricity = 0.23;
		private const double priceWater = 3.2;

		protected void Page_Load(object sender, EventArgs e)
		{
			var now = DateTime.Today;

			var commandText = "SELECT [Date], [GasDiff], [ElectricityDayDiff], [WaterDiff] FROM [MetersDailyDiffs] order by [Date]";

			var commandText2 = string.Format(
				"SELECT [EventDate],[EventDescription] FROM [SpecialEvent] order by [EventDate]");

			var valuesGas = new List<double>();
			var valuesElectricity = new List<double>();
			var valuesWater = new List<double>();
			var minDate = now;
			var flagsDataBuilder = new StringBuilder();
			using (var connection = new SqlConnection(Constants.ConnectionString)) {
				try {
					connection.Open();
					using (var command = connection.CreateCommand()) {
						command.CommandText = commandText;
						using (var reader = command.ExecuteReader()) {
							while (reader.Read()) {
								var date = reader.GetDateTime(0);
								var valueGas = reader.GetDouble(1)*priceGas;
								var valueElectricity = reader.GetDouble(2)*priceElectricity;
								var valueWater = reader.GetDouble(3)*priceWater;

								if (date < minDate)
									minDate = date;
								valuesGas.Add(valueGas);
								valuesElectricity.Add(valueElectricity);
								valuesWater.Add(valueWater);
							}
							reader.Close();

						}
					}

					using (var command = connection.CreateCommand()) {
						command.CommandText = commandText2;
						using (var reader = command.ExecuteReader()) {
							while (reader.Read()) {
								var date = reader.GetDateTime(0);
								var description = reader.GetString(1);
								flagsDataBuilder.AppendFormat("{{x : Date.UTC({0}, {1}, {2}), title : '{3}', text : '{4}'}}, ",
									date.Year, date.Month - 1, date.Day, description.Substring(0, 2), description);

							}
							reader.Close();

						}
					}

				}
				finally {
					if (connection.State == ConnectionState.Open)
						connection.Close();
					connection.Dispose();
				}
			}

			if (flagsDataBuilder.Length > 0)
				flagsDataBuilder.Length = flagsDataBuilder.Length - 2;

			minYear = minDate.Year.ToString();
			minMonth = (minDate.Month - 1).ToString();
			minDay = minDate.Day.ToString();

			chartDataGas = string.Join(", ", valuesGas.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));
			chartDataElectricity = string.Join(", ", valuesElectricity.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));
			chartDataWater = string.Join(", ", valuesWater.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));
			flagsData = flagsDataBuilder.ToString();
		}
	}
}
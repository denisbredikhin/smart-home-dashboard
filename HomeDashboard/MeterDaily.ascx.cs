﻿using System;
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
	public partial class MeterDaily : System.Web.UI.UserControl
	{
		public string ColumnName { get; set; }

		public string Title { get; set; }

		public string TitleYAxis { get; set; }

		public string SeriesName { get; set; }

		protected string chartData;
		protected string flagsData;
		protected string minYear;
		protected string minMonth;
		protected string minDay;

		protected void Page_Load(object sender, EventArgs e)
		{
			var now = DateTime.Today;

			var commandText = string.Format(
				"SELECT [Date], [{0}] FROM [MetersDailyDiffs] order by [Date]", ColumnName);

			var commandText2 = string.Format(
				"SELECT [EventDate],[EventDescription] FROM [SpecialEvent] order by [EventDate]");

			var values = new List<double>();
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
								var value = reader.GetDouble(1);

								if (date<minDate)
									minDate = date;
								values.Add(value);
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
									date.Year, date.Month-1, date.Day, description.Substring(0, 2), description);

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

			if (flagsDataBuilder.Length>0)
				flagsDataBuilder.Length = flagsDataBuilder.Length-2;

			minYear = minDate.Year.ToString();
			minMonth = (minDate.Month-1).ToString();
			minDay = minDate.Day.ToString();

			chartData = string.Join(", ", values.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));
			flagsData = flagsDataBuilder.ToString();
		}
	}
}
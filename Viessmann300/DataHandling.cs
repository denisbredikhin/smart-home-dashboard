using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ViessmannControl
{
	class myDatei_handling
	{
		public static string Get_Applikation_Path()
		{
			string Dateipfad = "";
			try {
				Dateipfad = System.IO.Directory.GetCurrentDirectory();
				if (Dateipfad[Dateipfad.Length - 1] != '\\') {
					Dateipfad = Dateipfad + "\\";
				}
			}
			catch (System.InvalidOperationException ex) {
				MessageBox.Show("Fehler 99", "Problem aktuelles Verzeichnis festzustellen!", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			return Dateipfad;
		}

		public static void Write_Error_Message()
		{

		}


		public static void Append_Data_to_File()
		{
			// oeffnet oder legt an, wenn nicht vorhanden und haengt am Ende an
			FileStream fs = new FileStream("c:\\Variables.txt", FileMode.Append, FileAccess.Write, FileShare.Write);
			fs.Close();
			StreamWriter ST_WR = new StreamWriter("c:\\Variables.txt", true, Encoding.ASCII);
			ST_WR.WriteLine("This is an example of using file handling concepts in VB .NET.");
			ST_WR.WriteLine("This concept is interesting.");
			ST_WR.Write(Environment.NewLine);
			ST_WR.Close();
		}

		public static void Write_Data(Byte[] My_Serial_Input_Buffer, int offset, int count)
		{
			//    ByVal My_Serial_Input_Buffer As Byte(), ByVal offset As Byte, ByVal count As Integer
			FileStream fs = new FileStream("c:\\Variables.txt", FileMode.Append, FileAccess.Write, FileShare.Write);
			fs.Write(My_Serial_Input_Buffer, offset, count);
			fs.Close();
		}

		public static void Write_Data_to_File()
		{
			// oeffnet oder legt an, wenn nicht vorhanden und traegt ein.
			FileStream fs = new FileStream("c:\\Variables.txt", FileMode.Create, FileAccess.Write, FileShare.Write);
			fs.Close();
			StreamWriter ST_WR = new StreamWriter("c:\\Variables.txt");
			ST_WR.WriteLine("This is an example of using file handling concepts in VB .NET.");
			ST_WR.WriteLine("This concept is interesting.");
			ST_WR.Write(Environment.NewLine);
			ST_WR.Close();
		}

		public static void Read_Data_from_File()
		{
			// oeffnet oder legt an, wenn nicht vorhanden und traegt ein.
			FileStream fs = new FileStream("c:\\Variables.txt", FileMode.Open, FileAccess.Read);
			StreamWriter ST_WR = new StreamWriter("c:\\Variables.txt");
			ST_WR.ToString(); // ??
			ST_WR.Close();
		}

	}

}

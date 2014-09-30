using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace ViessmannControl
{
	public partial class MainForm: Form
	{
		private Protocol300BoilerConnectionConfiguration config;
		#region DEKLARATIONEN

		private int Zeichenanzahl1;
		private const String Appname = "Viessdata 2.03 ";
		private String DataFilename; // CSV Datei für Daten
		private const String cfg_DateiName = "vito_Config.xml";
		private const String DP_Dateiname = "vito_DP.xml";

		private SerialPort mySerialPort = new SerialPort();
		private static readonly Byte[] My_Serial_Output_Buffer = new byte[256];
		private static readonly Byte[] My_Serial_Input_Buffer = new byte[256];
		private int My_Serial_Input_Buffer_Zeiger;
		private int Ser_Uebertragungsstatus = 0;
		private int gelesener_Datenpunkt = 0;
		private int Minuten_seit_Montag_alt;
		private bool lese_alle_Datenpunkte = false;
		private bool lese_alle_Datenpunkte_zweimal = false;
		private int tempt2interval = 1;
		private float volstrom = 9999, kesseltemp;
		private int SparA5 = -20, SparA6 = -20, RTStemp = 0;

		private static readonly string[] Device_ID_Array = new String[50];
		private static readonly string[] Device_name_Array = new String[50];
		private static readonly string[] Device_protocol_Array = new String[50];

		private System.Windows.Forms.Timer t1 = new System.Windows.Forms.Timer(); // Timer anlegen
		private System.Windows.Forms.Timer t2 = new System.Windows.Forms.Timer(); // Timer anlegen


		private static int Reihe = 0;
		private static int Reihe_Zeiten = 0;
		private static double maxpower = 19; //Max.Leistung der Therme 


		private String[] Wochentag = new String[7] {"Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"}; // testweise

		private bool toolStripButton2_pressed = false;


		private float DefaultWert_AnaDaten = -20.0f;

		private bool auto_start = false;

		private static NotifyIcon notico; // eigenes Icon

		#endregion

		public MainForm(Protocol300BoilerConnectionConfiguration config) // Init + Variablendeklaration
		{
			this.config = config;
			InitializeComponent();
		}

		#region MainForm Event-Handler

		private void Main_Form_Load(object sender, EventArgs e)
		{
			mySerialPort.DataReceived += new SerialDataReceivedEventHandler(mySerialPort_DataReceived);
			notico = new NotifyIcon {Icon = new Icon("vitodens300w.ico"), Visible = true, Text = Appname};

			t1.Interval = 50; // Intervall festlegen, hier 15 ms
			t1.Tick += t1_Tick; // Eventhandler ezeugen der beim Timerablauf aufgerufen wird

			t2.Interval = 25; // Intervall festlegen, hier 15 ms
			tempt2interval = 25;
			t2.Tick += t2_Tick; // Eventhandler ezeugen der beim Timerablauf aufgerufen wird


			this.Size = new Size(1024, 600);



			dataSet1.Clear();
			lese_alle_Datenpunkte = true; // Beim 1. Durchlauf alle lesen

			dataSet1.ReadXml(System.IO.Directory.GetCurrentDirectory()+"\\"+DP_Dateiname);




			mydataGridView1.DataSource = dataSet1;
			mydataGridView1.DataMember = "datapoint";
			mydataGridView1.DefaultCellStyle.NullValue = "no entry";
			//        mydataGridView1.DefaultCellStyle.WrapMode =  DataGridViewTriState.True;

			//      mydataGridView1.Rows[mydataGridView1.RowCount-1].Visible = false;  // letzte blanke Zeile weg
			mydataGridView1.RowHeadersVisible = false;
			mydataGridView1.AllowUserToAddRows = false;
			mydataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft; // Ausrichtung 

			// 0 = CheckBox
			// 1 = addr
			// 2 = len
			// 3 = blocklen
			// 4 = Offset
			// 5 = precision
			// 6 = calc
			// 7 = description
			// 8 = enable
			// 9 = name
			//10 = ID
			//11 = Wert_hex
			//12 = Wert_dez
			//13 = Wert_val
			//14 = Nr

			//  Anzeigereihenfolge
			//  Dabei mußt du aber alle Columns durchgehen
			//  mydataGridView1.Columns[0].DisplayIndex;
			//  Sortierung - welche Spalte und absteigend oder aufsteigend
			DataGridViewColumn col = mydataGridView1.SortedColumn;
			System.Windows.Forms.SortOrder order = mydataGridView1.SortOrder;

			// alle ausschalten
			foreach (DataGridViewColumn clmn in mydataGridView1.Columns)
				clmn.Visible = false;

			mydataGridView1.AutoGenerateColumns = false;

			// Checkbox als linke Spalte Position 0
			DataGridViewCheckBoxColumn col_chkbox = new DataGridViewCheckBoxColumn();
			col_chkbox.ThreeState = false;
			col_chkbox.FalseValue = "0";
			col_chkbox.TrueValue = "1";
			col_chkbox.HeaderText = "Akt."; // Ueberschrift
			col_chkbox.Name = "Akt."; // Ansprechbarer Name   
			col_chkbox.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
			col_chkbox.FlatStyle = FlatStyle.Standard;
			col_chkbox.CellTemplate.Style.BackColor = Color.Beige;
			col_chkbox.DataPropertyName = "enable"; // aus der xml lesen
			mydataGridView1.Columns.Insert(0, col_chkbox);
			mydataGridView1.Columns["Akt."].Width = 5; //Ein
			mydataGridView1.Columns["Akt."].DisplayIndex = 0;
			mydataGridView1.Columns["Akt."].ToolTipText = "Aktualisieren";

			// Checkbox als linke Spalte Position 0
			DataGridViewCheckBoxColumn col_chkbox1 = new DataGridViewCheckBoxColumn();
			col_chkbox1.ThreeState = false;
			col_chkbox1.FalseValue = "0";
			col_chkbox1.TrueValue = "1";
			col_chkbox1.HeaderText = "Sp."; // Ueberschrift
			col_chkbox1.Name = "Sp."; // Ansprechbarer Name   
			col_chkbox1.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
			col_chkbox1.FlatStyle = FlatStyle.Standard;
			col_chkbox1.CellTemplate.Style.BackColor = Color.Beige;
			col_chkbox1.DataPropertyName = "speichern"; // aus der xml lesen
			mydataGridView1.Columns.Insert(1, col_chkbox1);
			mydataGridView1.Columns["Sp."].Width = 5; //Ein
			mydataGridView1.Columns["Sp."].DisplayIndex = 1;
			mydataGridView1.Columns["Sp."].ToolTipText = "speichern für Graph-History";


			// 9 = name
			mydataGridView1.Columns["name"].DisplayIndex = 2;
			mydataGridView1.Columns["name"].HeaderText = "Bezeichnung";
			mydataGridView1.Columns["name"].Visible = true;
			mydataGridView1.Columns["name"].Width = 245;

			// 1 = addr
			mydataGridView1.Columns["addr"].DisplayIndex = 3;
			mydataGridView1.Columns["addr"].HeaderText = "Adr.";
			mydataGridView1.Columns["addr"].Visible = true;
			mydataGridView1.Columns["addr"].Width = 35;

			// 11 = Wert_hex Spalte hinzufügen
			DataGridViewColumn col_wert_hex = new DataGridViewTextBoxColumn();
			col_wert_hex.Name = "Wert_Hex"; // Spaltenüberschrift
			mydataGridView1.Columns.Add(col_wert_hex);
			mydataGridView1.Columns["Wert_Hex"].DisplayIndex = 4;
			// mydataGridView1.Columns["Wert_Hex"].DefaultCellStyle.Format = "X04";

			// 12 = Wert_dez Spalte hinzufügen
			DataGridViewColumn col_wert_dez = new DataGridViewTextBoxColumn();
			col_wert_dez.Name = "Wert_Dez"; // Spaltenüberschrift
			mydataGridView1.Columns.Add(col_wert_dez);
			mydataGridView1.Columns["Wert_Dez"].DisplayIndex = 5;
			mydataGridView1.Columns["Wert_Dez"].Visible = false;

			// 13 = Wert_val Spalte hinzufügen
			DataGridViewColumn col_wert_val = new DataGridViewTextBoxColumn();
			col_wert_val.Name = "Wert_Val"; // Spaltenüberschrift
			mydataGridView1.Columns.Add(col_wert_val);
			mydataGridView1.Columns["Wert_Val"].DisplayIndex = 6;

			// 14 = Nr = Datpunktanzahl Spalte hinzufügen
			DataGridViewColumn col_Datensatz = new DataGridViewTextBoxColumn();
			col_Datensatz.Name = "Nr"; // Spaltenüberschrift
			mydataGridView1.Columns.Add(col_Datensatz);
			mydataGridView1.Columns["Nr"].DisplayIndex = 7;
			mydataGridView1.Columns["Nr"].Width = 35;
			// mydataGridView1.Columns["Nr"].HeaderText.Alignment = DataGridViewContentAlignment.MiddleRight;
			//mydataGridView1.Columns["Nr"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
			mydataGridView1.Columns["Nr"].DefaultCellStyle.Format = "000"; // Anzeige 3 stellig

			for (int i = 0; i<mydataGridView1.RowCount; i++) {
				mydataGridView1["Nr", i].Value = i;
			}

			/*      mydataGridView1.Columns["len"].Visible = false;
            mydataGridView1.Columns["len"].DisplayIndex = 12;

            mydataGridView1.Columns["offset"].Visible = false;
            mydataGridView1.Columns["offset"].DisplayIndex = 6;

            mydataGridView1.Columns["precision"].Visible = false;
            mydataGridView1.Columns["precision"].DisplayIndex = 7;

            mydataGridView1.Columns["blocklen"].Visible = false;
            //     mydataGridView1.Columns["blocklen"].DisplayIndex = 8;

            mydataGridView1.Columns["unit"].Visible = false;
            //     mydataGridView1.Columns["calc"].DisplayIndex = 9;

            mydataGridView1.Columns["description"].Visible = false;
            //     mydataGridView1.Columns["description"].DisplayIndex = 10;

            mydataGridView1.Columns["ID"].Visible = false;
            //     mydataGridView1.Columns["ID"].DisplayIndex = 11;

*/
			/*            for (int j = 0; j < mydataGridView1.ColumnCount; j++)
                        {
                            mydataGridView1.AutoResizeColumn(j);
                        }

                      //  mydataGridView1.RowHeadersVisible = true; */
			for (int j = 0; j<mydataGridView1.ColumnCount; j++) {
				mydataGridView1.Columns[j].Visible = true;
			}

			//  mydataGridView1.RowHeadersVisible = false;

			// UpdateGraphCountMenu();

			// UpdateColorSchemaMenu();


			XmlDocument ser_config = new XmlDocument();
			ser_config.Load(System.IO.Directory.GetCurrentDirectory()+"\\"+cfg_DateiName);


			this.fillTimerComboBox1();

			this.fill_Graf_LaengeComboBox();

			this.tabControl1.SelectedIndex = 1;
				// Aktivieren der Datenseite ist anscheinend zum Initialisieren von mydatagrid notwendig, sonst wird beim ersten Wechsel auf dieses Tab no entry angezeigt
			this.tabControl1.SelectedIndex = 0; // 1. Seite


			//      this.toolStripLabel2.TextAlign = HorizontalAlignment.Center;
			this.toolStripLabel2.Font = new System.Drawing.Font("Arial", 7, FontStyle.Regular);

			//this.numUpDown_von.Font = new System.Drawing.Font("Arial", 11, FontStyle.Regular);
			//this.numUpDown_bis.Font = new System.Drawing.Font("Arial", 11, FontStyle.Regular);


			toolStripLabel2.Text = "";
			toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
				// Damit die Hintergrundfarbe geändert werden kann
			toolStripLabel2.BackColor = Color.Red;

			CB_Betriebsart.Items.Add("В режиме ожидания");
			CB_Betriebsart.Items.Add("Только горячая вода");
			CB_Betriebsart.Items.Add("Отопление и ГВС");
			CB_Betriebsart.Items.Add("Постоянно сниженный");
			CB_Betriebsart.Items.Add("Постоянно нормальный");

			CB_WWHysterese.Items.Add("2.5");
			for (int i = 1; i<11; i++)
				CB_WWHysterese.Items.Add(i.ToString());

			for (int i = 0; i<16; i++) {
				CB_Frostschutztemp.Items.Add(i.ToString());
			}

			CB_ZirkuFrequ.Items.Add("Ein nach Prog.");
			CB_ZirkuFrequ.Items.Add("1 mal/Std. n.P.");
			CB_ZirkuFrequ.Items.Add("2 mal/Std. n.P.");
			CB_ZirkuFrequ.Items.Add("3 mal/Std. n.P.");
			CB_ZirkuFrequ.Items.Add("4 mal/Std. n.P.");
			CB_ZirkuFrequ.Items.Add("5 mal/Std. n.P.");
			CB_ZirkuFrequ.Items.Add("6 mal/Std. n.P.");
			CB_ZirkuFrequ.Items.Add("Dauernd Ein");


			CB_SparHK.Items.Add("Inaktiv");
			for (int i = 1; i<6; i++) {
				CB_SparHK.Items.Add("AT > RTS + "+(6-i).ToString("0")+"K");
			}
			CB_SparHK.Items.Add("AT > RTS");
			for (int i = 7; i<16; i++) {
				CB_SparHK.Items.Add("AT > RTS - "+(i-6).ToString("0")+"K");
			}

			for (int i = 0; i<=30; i++) {
				CB_SparBrenner.Items.Add("AT ged. > "+(6+i).ToString("0")+"°");
			}
			CB_SparBrenner.Items.Add("Inaktiv");

			Label0842.BackColor = Color.Yellow; // Brenner
			Label0846.BackColor = Color.Yellow; // WW-Zirkulationspumpe
			Label0845.BackColor = Color.Yellow; // Speicherladepumpe
			Label3906.BackColor = Color.Yellow; // Speicherladepumpe
			Label0883.BackColor = Color.Yellow; // Sammelstoerung 0 = ok
			label29.BackColor = Color.Yellow; // Brennerstoerung 0 = ok
			Frostgefahr.BackColor = Color.Yellow; // Frostgefahr 
			Label_SparA5.BackColor = Color.Yellow; // SparA5 
			Label_SparA6.BackColor = Color.Yellow; // SparA6 


			load_maxpower();
			textBox3.Text = maxpower.ToString();


			//numUpDown_von.LostFocus += new EventHandler(numUpDown_von_Leave);  // Interrupthaendler anlegen
			//numUpDown_bis.LostFocus += new EventHandler(numUpDown_bis_Leave);  // Interrupthaendler anlegen



			//numUpDown_von.Hexadecimal = true;
			//numUpDown_bis.Hexadecimal = true;

			//numUpDown_von.Minimum = 0x0;
			//numUpDown_von.Maximum = 0xFFFF;
			//numUpDown_von.Value = 0;


			//numUpDown_bis.Minimum = 0x1;
			//numUpDown_bis.Maximum = 0xFFFF;
			//numUpDown_bis.Value = 0x1000;

			toolStripButton2.Text = "Start";


			//   InitDataGraphs();
			//   plotterDisplayEx1.Refresh();
			this.toolStripComboBox2.SelectedIndex = this.toolStripComboBox2.Items.IndexOf("1Tag"); //Voreinstellung

			lese_alle_Datenpunkte = true; // Beim 1. Durchlauf alle lesen
			Lese_Steuerungen(); // Tabelle mit den bekannten Steuerungen lesen


		}

		private void Main_Form_Closing(object sender, FormClosingEventArgs e)
		{
			notico.Visible = false;
				//comport.DiscardOutBuffer();
				//comport.DiscardInBuffer();
				//comport.Dispose();
				if (!mySerialPort.IsOpen) Open_mySerialPort();
				My_Serial_Output_Buffer[0] = 0x04; // 0x04 senden, um KW Protokoll zu initialisieren
				mySerialPort.Write(My_Serial_Output_Buffer, 0, 1);
				if (mySerialPort.IsOpen) {
					mySerialPort.ReadExisting();
						mySerialPort.Close();
				}
			save_maxpower();
			Application.Exit();
		}

		private void TabControl1_SelectedIndexChanged(Object sender, System.EventArgs e)
		{
			for (int i = 0; i<mydataGridView1.RowCount; i++) {
				mydataGridView1["Nr", i].Value = i;

			}



			switch (tabControl1.SelectedIndex) {
				case 0: // wozu das hier gut ist habe ich keine Ahnung
					int i = 3;
					i++;
					break;
				case 1:
					int j = 3;
					j++;
					break;
			}
		}

		private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (toolStripComboBox1.SelectedItem.ToString()) {
				case "Stop":
					toolStripButton2.Text = "Start"; // Bildchen umschalten
					toolStripButton2_pressed = false;
					t2.Stop();

					return;

				case "20 Sek":

					t2.Interval = 20000; //20s
					tempt2interval = 20000;
					break;

				case "30 Sek":

					t2.Interval = 30000; //30s
					tempt2interval = 30000;
					break;

				case "40 Sek":

					t2.Interval = 40000; //40s
					tempt2interval = 40000;
					break;

				case "50 Sek":

					t2.Interval = 50000; //50s
					tempt2interval = 50000;
					break;

				default:
					break;
			}


		}


		private void toolStripButton2_Click(object sender, EventArgs e)
		{
			// Play-Button
			toolStripButton2_pressed = !toolStripButton2_pressed; //negiere
			if (toolStripButton2_pressed) {
				toolStripComboBox1.SelectedItem = (tempt2interval/1000).ToString("00")+" Sek"; // jetzt automatisch
				toolStripButton2.Text = "Stop";
				// schnittstelle öffnen und auf das 05h warten
				this.Text = Appname+" - verbinde";
				toolStripLabel2.BackColor = Color.Yellow;
				Ser_Uebertragungsstatus = 0;
				gelesener_Datenpunkt = 0;

				if (!mySerialPort.IsOpen) Open_mySerialPort();
				t1.Start();
				t2.Start();

			}
			else // wenn gestartet war und ich will stoppen
			{
				toolStripButton2.Text = "Start"; // kann wieder starten
				toolStripComboBox1.SelectedItem = "Stop";
				t2.Stop();
			}


		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			if (lese_alle_Datenpunkte)
				lese_alle_Datenpunkte_zweimal = true;
			lese_alle_Datenpunkte = true;
		}

		private byte calc_CRC(byte[] telegram)
		{
			uint CRCsum = 0;
			byte telestart = 1;
			byte teleend = (byte)(telegram[1]+1);

			if (telegram[0]!=0x41) // vielleicht noch ein 0x06 davor?
			{
				telestart++;
				teleend = (byte)(telegram[2]+2);
				if ((telegram[0]!=0x06)&(telegram[1]!=0x41)) return (0);
			}
			for (byte i = telestart; i<=teleend; i++) {
				CRCsum += telegram[i];
			}
			return ((byte)(CRCsum%0x100));
		}

		private bool check_CRC()
		{
			return (calc_CRC(My_Serial_Input_Buffer)==My_Serial_Input_Buffer[(My_Serial_Input_Buffer[2]+3)%0x100]);
		}

		private void t1_Tick(object sender, EventArgs e)
		{
			// es kam eine bestimmte Zeit kein Zeichen mehr. Timer1 ist abgelaufen
			string zeichen1 = "";

			t1.Stop(); // definierten Zustand des Timers
			t2.Stop(); // Automatic=zyklische Abfrage  stop
			int M_S_I_B_Z = My_Serial_Input_Buffer_Zeiger; // wie viele Zeichen waren es?
			My_Serial_Input_Buffer_Zeiger = 0; // Für das nächste Telegramm  

			int z = 0;

			if (!mySerialPort.IsOpen) {
				t2.Start();
				return;
			}

			switch (Ser_Uebertragungsstatus) {
				case 0: // 0 = Empfangsbereit
					if (M_S_I_B_Z==0) My_Serial_Input_Buffer[0] = 0x04;
					switch (My_Serial_Input_Buffer[0]) // Empfang im Ruhezustand 05h
					{
						case 0x05:
							My_Serial_Output_Buffer[0] = 0x16;
							My_Serial_Output_Buffer[1] = 0x00;
							My_Serial_Output_Buffer[2] = 0x00;
							mySerialPort.Write(My_Serial_Output_Buffer, 0, 3); // Buffer, ab 0, von 0 bis 2, = 3 Bytes senden
							t1.Interval = 50;
							this.t1.Start(); // Timer starten
							break;
						case 0x06:
							My_Serial_Output_Buffer[0] = 0x41;
							My_Serial_Output_Buffer[1] = 0x05;
							My_Serial_Output_Buffer[2] = 0x00;
							My_Serial_Output_Buffer[3] = 0x01;
							My_Serial_Output_Buffer[4] = 0x00;
							My_Serial_Output_Buffer[5] = 0xF8;
							My_Serial_Output_Buffer[6] = 0x02; // Ich erwarte 2 Zeichen 
							My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
							Ser_Uebertragungsstatus = 1;
							mySerialPort.Write(My_Serial_Output_Buffer, 0, 8);
							t1.Interval = 50;
							this.t1.Start(); // Timer starten
							break;
						default:
							My_Serial_Output_Buffer[0] = 0x04;
							mySerialPort.Write(My_Serial_Output_Buffer, 0, 1); // Buffer, ab 0, 1 Byte senden
							t1.Interval = 50;
							this.t1.Start(); // Timer starten
							break;
					}
					break;

				case 1: // 1 = ich wartete auf Anlagenkennung 
					if ((My_Serial_Input_Buffer[0]!=0x06)|(M_S_I_B_Z==0)|(!(check_CRC()))) {
						Ser_Uebertragungsstatus = 0;
						break;
					}
					if (My_Serial_Input_Buffer[7]==My_Serial_Output_Buffer[6]) // erwartete Anzahl war korrekt
					{
						for (z = 0; z<35; z++) {
							if (!(String.IsNullOrEmpty(Device_ID_Array[z]))&
							    (Device_ID_Array[z].Contains(My_Serial_Input_Buffer[8].ToString("X2")+
							                                 My_Serial_Input_Buffer[9].ToString("X2")))) {
								this.Text = Appname+" - verbunden mit:  "+Device_name_Array[z]+"   Protokoll:  "+Device_protocol_Array[z];
								this.toolStripLabel2.BackColor = Color.LightGreen;
								Ser_Uebertragungsstatus = 2;
								break;
							}
						}
						if (Ser_Uebertragungsstatus!=2) {
							this.Text = Appname+" - verbunden mit Device: "+My_Serial_Input_Buffer[8].ToString("X2")+
							            My_Serial_Input_Buffer[9].ToString("X2");
							this.toolStripLabel2.BackColor = Color.LightGreen;
							Ser_Uebertragungsstatus = 2;
						}
						// bis zum 1. selektierten springen
						if (Reihe>=mydataGridView1.RowCount) Reihe = 0;

						if (lese_alle_Datenpunkte==false) {
							//String ttt = mydataGridView1["Akt.", Reihe].Value.ToString();
							while (Reihe<(mydataGridView1.RowCount)) {
								if ((mydataGridView1["Akt.", Reihe].Value.ToString()=="0") ||
								    (mydataGridView1["len", Reihe].Value.ToString()=="0"))
									Reihe++; //Checkbox = 0 dann Reihe überspringen
								else
									break;
							}
							// Abfrage ob es die letzte Reihe war und die war ebenfalls deaktiviert
							if (Reihe>=(mydataGridView1.RowCount)) {
								Ser_Uebertragungsstatus = 0; // dann alles  stoppen
								Reihe = 0;
								gelesener_Datenpunkt = 0;
								this.Text = Appname+" - nicht verbunden ";
								this.toolStripLabel2.BackColor = Color.Red;
								return;
							}

						}
						else {
							while (Reihe<(mydataGridView1.RowCount)) {
								if ((mydataGridView1["len", Reihe].Value.ToString()=="0"))
									Reihe++; //len = 0 dann Reihe überspringen
								else
									break;
							}
							// Abfrage ob es die letzte Reihe war und die war ebenfalls deaktiviert
							if (Reihe>=(mydataGridView1.RowCount)) {

								Beende_Ser_Uebertragung();
								return;
							}

						}

						// wenn einer selektiert war, dann wird er hier angefordert 
						My_Serial_Output_Buffer[0] = 0x41; // Telegrammstart
						My_Serial_Output_Buffer[1] = 0x05; // Nutzdaten, hier immer 5
						My_Serial_Output_Buffer[2] = 0x00; // Anfrage
						My_Serial_Output_Buffer[3] = 0x01; // Lesen
						My_Serial_Output_Buffer[4] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(0, 2),
							System.Globalization.NumberStyles.HexNumber);
						My_Serial_Output_Buffer[5] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(2, 2),
							System.Globalization.NumberStyles.HexNumber);
						My_Serial_Output_Buffer[6] = byte.Parse(mydataGridView1["len", Reihe].Value.ToString());
							// Länge mit de hex wandlung
						My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
						mySerialPort.Write(My_Serial_Output_Buffer, 0, 8); // Buffer senden
						t1.Interval = 50;
						this.t1.Start(); // Timer starten
						gelesener_Datenpunkt++; //Zähler für Textbox

					}
					else // wenn die Anzahl nicht korrekt war 
					{
						Ser_Uebertragungsstatus = 0;
					}
					break;

				case 2:
					// hier kommt Wert für Wert rein
					if ((My_Serial_Input_Buffer[0]==0x06) // Status ok
						//& (My_Serial_Input_Buffer[3] == 0x01)  //Antwort ohne Fehler
					    &(My_Serial_Input_Buffer[7]==My_Serial_Output_Buffer[6]) // erwartete Anzahl war korrekt
					    &(M_S_I_B_Z==9+My_Serial_Output_Buffer[6])
					    &(check_CRC())) {
						toolStripTextBox1.Text = Reihe.ToString(); // Anzeige der ausgelesenen Reihe
						toolStripTextBox2.Text = gelesener_Datenpunkt.ToString();


						for (int i = 0; i<My_Serial_Output_Buffer[6]; i++) // Anzeige der HEX-Werte
						{
							string zeichen = My_Serial_Input_Buffer[8+i].ToString("X2");
							zeichen1 = zeichen1+zeichen+" ";
						}
						mydataGridView1["Wert_Hex", Reihe].Value = zeichen1;

						zeichen1 = "";

						for (int i = 0; i<My_Serial_Output_Buffer[6]; i++) // Anzeige der dez-Werte
						{
							string zeichen = My_Serial_Input_Buffer[8+i].ToString("000");
							zeichen1 = zeichen1+zeichen+" ";
						}
						mydataGridView1["Wert_Dez", Reihe].Value = zeichen1;

						// Datum und Uhrzeit formatieren
						if (mydataGridView1["addr", Reihe].Value.ToString()=="088E") // Anzeige der Value (umgerechneten Werte)
						{
							string myString;
							myString = Wochentag[(My_Serial_Input_Buffer[12]+6)%7]+" "+
							           My_Serial_Input_Buffer[11].ToString("X2")+"."+My_Serial_Input_Buffer[10].ToString("X2")+"."+
							           My_Serial_Input_Buffer[8].ToString("X2")+My_Serial_Input_Buffer[9].ToString("X2")+" "+
							           My_Serial_Input_Buffer[13].ToString("X2")+":"+My_Serial_Input_Buffer[14].ToString("X2")+":"+
							           My_Serial_Input_Buffer[15].ToString("X2");
							mydataGridView1["Wert_Val", Reihe].Value = myString;
						}


						// wenn precision nicht leer ist            
						if (mydataGridView1["precision", Reihe].Value.ToString()!="") // Anzeige der Value (umgerechneten Werte)
						{
							float myValue = 0;

							switch (mydataGridView1["len", Reihe].Value.ToString()) {
								case "1":
									if (mydataGridView1["addr", Reihe].Value.ToString()=="27d4") // Neigung evtl. negativ
									{
										int myValue1 = (My_Serial_Input_Buffer[8]);

										if (myValue1>0x80) {
											myValue1 = (256-myValue1);
											myValue = myValue1*
											          float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
											myValue = -myValue;
										}
										else {
											myValue = myValue1*
											          float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
										}

										mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);

									}
									else {
										myValue = My_Serial_Input_Buffer[8]*
										          float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
										mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);
									}
									break;
								case "2":
									// negative Temperaturen behandeln
									// Die können nur bei den u. g. Datenpunkten auftreten
									if (mydataGridView1["addr", Reihe].Value.ToString()=="0800"|
									    mydataGridView1["addr", Reihe].Value.ToString()=="5525"|
									    mydataGridView1["addr", Reihe].Value.ToString()=="5527") // Aussentemp negativ
									{
										int myValue1 = ((My_Serial_Input_Buffer[9]<<8)+My_Serial_Input_Buffer[8]);
										if (myValue1>0x8000) {

											myValue1 = myValue1^0xFFFF;
											myValue = myValue1*
											          float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
											myValue = -myValue;
											mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);

										}
										else {
											myValue = ((My_Serial_Input_Buffer[9]<<8)+My_Serial_Input_Buffer[8])*
											          float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
											mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);
										}
										break;
									}
									// hier sind normale Werte, Temperaturen etc.
									myValue = ((My_Serial_Input_Buffer[9]<<8)+My_Serial_Input_Buffer[8])*
									          float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);

									mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);
									break;
								case "4":
									myValue = ((My_Serial_Input_Buffer[11]<<24)+(My_Serial_Input_Buffer[10]<<16)
									           +(My_Serial_Input_Buffer[9]<<8)+(My_Serial_Input_Buffer[8]));
										// * double.Parse(mydataGridView1["precision", Reihe].Value.ToString());
									mydataGridView1["Wert_Val", Reihe].Value =
										Math.Round(
											myValue/float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture), 2);

									break;
								default:
									break;
							}
						}

						Screen_Update(Reihe);
						// Bis hier ist die letzte Anforderung verarbeitet worden 

						Reihe++; // Nächste Reihe vorbereiten
						if (Reihe>=(mydataGridView1.RowCount)) // wenn der letzte Datensatz eingelesen war beende Abfrage
						{
							Beende_Ser_Uebertragung();
							if (lese_alle_Datenpunkte) {
								t2.Interval = 1;
							}
							else {
								t2.Interval = tempt2interval;
							}

							if (lese_alle_Datenpunkte) {
								lese_alle_Datenpunkte = false; // Nach den 1. Duchlauf beenden

							}
							if (lese_alle_Datenpunkte_zweimal) {
								lese_alle_Datenpunkte_zweimal = false;
								lese_alle_Datenpunkte = true;
							}
							return;
						}

						// bis zum naechsten selektierten springen
						if (lese_alle_Datenpunkte==false) {
							while ((Reihe<mydataGridView1.RowCount) &&
							       (mydataGridView1["Akt.", Reihe].Value.ToString()=="0" ||
							        mydataGridView1["len", Reihe].Value.ToString()=="0")) {
								Reihe++; //Checkbox = 0 dann Reihe überspringen
							}
							// hier kommt das Prog nur hin, wenn es innnerhalb des Grid ist 
							if (Reihe>=(mydataGridView1.RowCount)) // und letzter Datensatz aber nicht zum Lesen markiert
							{
								Beende_Ser_Uebertragung();
								if (lese_alle_Datenpunkte) {
									t2.Interval = 1;
								}
								else {
									t2.Interval = tempt2interval;
								}

								lese_alle_Datenpunkte = false; // Nach den 1. Duchlauf beenden
								if (lese_alle_Datenpunkte_zweimal) {
									lese_alle_Datenpunkte_zweimal = false;
									lese_alle_Datenpunkte = true;
								}
								return;
							}
						}
						else {
							while ((Reihe<mydataGridView1.RowCount) && (mydataGridView1["len", Reihe].Value.ToString()=="0")) {
								Reihe++; //Checkbox = 0 dann Reihe überspringen
							} // hier kommt das Prog nur hin, wenn es innnerhalb des Grid ist 
							if (Reihe>=(mydataGridView1.RowCount)) // und letzter Datensatz aber nicht zum Lesen markiert
							{
								Beende_Ser_Uebertragung();
								if (lese_alle_Datenpunkte) {
									t2.Interval = 1;
								}
								else {
									t2.Interval = tempt2interval;
								}

								lese_alle_Datenpunkte = false; // Nach den 1. Duchlauf beenden
								if (lese_alle_Datenpunkte_zweimal) {
									lese_alle_Datenpunkte_zweimal = false;
									lese_alle_Datenpunkte = true;
								}
								return;
							}

						}

						if (Reihe>=(mydataGridView1.RowCount)) // und letzter Datensatz aber nicht zum Lesen markiert
						{
							Beende_Ser_Uebertragung();
							if (lese_alle_Datenpunkte) {
								t2.Interval = 1;
							}
							else {
								t2.Interval = tempt2interval;
							}

							lese_alle_Datenpunkte = false; // Nach den 1. Duchlauf beenden
							if (lese_alle_Datenpunkte_zweimal) {
								lese_alle_Datenpunkte_zweimal = false;
								lese_alle_Datenpunkte = true;
							}
							return;
						}

						My_Serial_Output_Buffer[0] = 0x41; // Telegrammstart
						My_Serial_Output_Buffer[1] = 0x05; // Nutzdaten, hier immer 5
						My_Serial_Output_Buffer[2] = 0x00; // Anfrage
						My_Serial_Output_Buffer[3] = 0x01; // Lesen
						My_Serial_Output_Buffer[4] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(0, 2),
							System.Globalization.NumberStyles.HexNumber);
						My_Serial_Output_Buffer[5] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(2, 2),
							System.Globalization.NumberStyles.HexNumber);
						My_Serial_Output_Buffer[6] = byte.Parse(mydataGridView1["len", Reihe].Value.ToString());
							// Länge mit de hex wandlung
						My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
						mySerialPort.Write(My_Serial_Output_Buffer, 0, 8); // Buffer senden
						t1.Interval = 50;
						this.t1.Start(); // Timer starten
						gelesener_Datenpunkt++; //Zähler für Textbox


					} //end if wenn es nicht die Zeichenanzahl war
					else {
						My_Serial_Output_Buffer[0] = 0x41; // Telegrammstart
						My_Serial_Output_Buffer[1] = 0x05; // Nutzdaten, hier immer 5
						My_Serial_Output_Buffer[2] = 0x00; // Anfrage
						My_Serial_Output_Buffer[3] = 0x01; // Lesen
						My_Serial_Output_Buffer[4] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(0, 2),
							System.Globalization.NumberStyles.HexNumber);
						My_Serial_Output_Buffer[5] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(2, 2),
							System.Globalization.NumberStyles.HexNumber);
						My_Serial_Output_Buffer[6] = byte.Parse(mydataGridView1["len", Reihe].Value.ToString());
							// Länge mit de hex wandlung
						My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
						mySerialPort.Write(My_Serial_Output_Buffer, 0, 8); // Buffer senden
						t1.Interval = 50;
						this.t1.Start(); // Timer starten


					}

					break;


				default:
					break; // alles andere interessiert uns nicht
			} //switch

		} //void t1_Tick

		private void t2_Tick(object sender, EventArgs e)
		{
			t2.Stop();

			Ser_Uebertragungsstatus = 0;
			gelesener_Datenpunkt = 0;
			if (!mySerialPort.IsOpen) {
				Open_mySerialPort();
			}
			;
			// starte Abfragezyklus
			if (mySerialPort.IsOpen)
				t1.Start();
			else
				t2.Start();
		}

		private void mySerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			// wird aufgerufen, wenn ein oder mehrere Zeichen im Puffer sind
				Zeichenanzahl1 = this.mySerialPort.BytesToRead; // puffern, da BytesToRead nach lesen = 0
				this.mySerialPort.Read(My_Serial_Input_Buffer, My_Serial_Input_Buffer_Zeiger, Zeichenanzahl1);
				My_Serial_Input_Buffer_Zeiger = My_Serial_Input_Buffer_Zeiger+Zeichenanzahl1;
				this.Invoke(new EventHandler(Timer1_Trigger)); // Timer triggern solange Daten ankommen
		}

		private void Timer1_Trigger(object sender, EventArgs e)
		{
			// solange Zeichen reinkommen wird der Timer1 getriggert. Kommt kein Zeichen mehr wird 
			// nach Ablauf der Zeit der Int. ausgelöst 
			t1.Stop();
			t1.Interval = 50;
			t1.Start();
		}

		#endregion

		#region tabPage0 Event-Handler



		#endregion

		#region tabPage1 Event-Handler

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			// Wenn die Datei vorhanden war ggf. löschen, anschließend mit 1. Zeile neu erstellen
			if (checkBox1.Checked) {

				if (File.Exists(DataFilename)) {
					DialogResult result1 = MessageBox.Show("Datei "+DataFilename+" überschreiben?",
						"Alle Daten gehen verloren!!", MessageBoxButtons.YesNo);
					if (result1==DialogResult.Yes) {
							File.Delete(DataFilename);
					}
					else {
						return;
					}
				}
					using (FileStream File_Stream = new FileStream(DataFilename, FileMode.Append, FileAccess.Write, FileShare.Write)) {
						using (StreamWriter Stream_Writer = new StreamWriter(File_Stream)) {
							Stream_Writer.Write(System.DateTime.Now.ToString(";"+"yyyy-MM-dd")+";");
							for (int j = 0; j<mydataGridView1.RowCount; j++) {
								if (mydataGridView1["Sp.", j].Value.ToString()=="1") // Wenn selektiert dann..
								{
									Stream_Writer.Write(mydataGridView1["addr", j].Value.ToString()+";");
								}
							}
							Stream_Writer.Write("\r\n"); // \r=return \n=newline
						}
						File_Stream.Close();
					}
			}
		}

		private void btn_select_alle_Click(object sender, EventArgs e)
		{
			// selektiere alle
			for (int i = 0; i<mydataGridView1.RowCount; i++) {
				mydataGridView1["Akt.", i].Value = "1";
			}
		}

		private void btn_select_kein_Click(object sender, EventArgs e)
		{
			// selektiere kein
			for (int i = 0; i<mydataGridView1.RowCount; i++) {
				mydataGridView1["Akt.", i].Value = "0";

			}
		}

		private void button14_Click(object sender, EventArgs e)
		{
			for (int i = 0; i<mydataGridView1.RowCount; i++) {
				mydataGridView1["Wert_Val", i].Value = 0; // null;
				mydataGridView1["Wert_Hex", i].Value = 0; // null;
				mydataGridView1["Wert_Dez", i].Value = 0; //  null;
			}
		}




		private void Screen_Update(int Reihe)
		{
			// Achtung Event musste zu Fuss im Designer eingetragen werden.
			//if (e.ColumnIndex == 15) //mydataGridView1.Columns["Wert_Val"].Index & mydataGridView1.Columns["Wert_Val"].Index.ToString() != "0")   // wenn sich die Valuespalte geändert hat
			bool convertok;

			switch (mydataGridView1["addr", Reihe].Value.ToString()) // Welche Adresse?
			{
				case "088E": // Datum Zeit
					//TextBox0200.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "0800": // Temperatur Sensor 1 Aussen 
					TextBox0800.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					break;
				case "083A": // Aussen Sensor  Status ok nicht ok
					if (mydataGridView1["Wert_Val", Reihe].Value.ToString()!="0") // Wert val
					{
						TextBox0800.BackColor = Color.Red;
					}
					else {
						TextBox0800.BackColor = Color.LightGreen;
					}
					break; // Hintergrund Rot
				case "0896": // Temperatur Sensor Raum HK1 
					if (mydataGridView1["len", Reihe].Value.ToString()!="0") {
						label91.Visible = true;
						TextBoxRT.Visible = true;
						TextBoxRT.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					}
					break;
				case "5523": // AussenTmp ged.
					float ATged;
					convertok = float.TryParse(mydataGridView1["Wert_Val", Reihe].Value.ToString(), out ATged);
					if ((convertok) && (SparA6!=-20)) {
						if (ATged>SparA6) Label_SparA6.BackColor = Color.Green;
						if (ATged<(SparA6-2)) Label_SparA6.BackColor = Color.LightGray;
					}
					break;
				case "5525": // AussenTmp geg.
					float ATgeg;
					convertok = float.TryParse(mydataGridView1["Wert_Val", Reihe].Value.ToString(), out ATgeg);
					if ((convertok) && (SparA5!=-20) && (RTStemp!=-20)) {
						if (ATgeg>SparA5) Label_SparA5.BackColor = Color.Green;
						if (ATgeg<(SparA5-1)) Label_SparA5.BackColor = Color.LightGray;
					}
					break;
				case "5527": // AussenTmp gem.
					TextBox5525.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					break;
				case "2500": // Frostgefahr    
					byte frostg = byte.Parse(mydataGridView1["Wert_hex", Reihe].Value.ToString().Substring(16*3, 2),
						System.Globalization.NumberStyles.HexNumber); // Substring(ab wann,länge)

					if ((frostg&1)!=0) {
						Frostgefahr.BackColor = Color.Red;
					}
					else {
						Frostgefahr.BackColor = Color.LightGray;
					}
					break;

				case "0802": // Temperatur Sensor 3 Kessel
					TextBox0802.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					convertok = float.TryParse(mydataGridView1["Wert_Val", Reihe].Value.ToString(), out kesseltemp);
					if (!convertok) {
						kesseltemp = 0;
					}
					break;
				case "083B": // Kessel Sensor 2  Status ok nicht ok
					if (mydataGridView1["Wert_Val", Reihe].Value.ToString()!="0") // Wert val
					{
						TextBox0802.BackColor = Color.Red;
					}
					else {
						TextBox0802.BackColor = Color.LightGreen;
					}
					break; // Hintergrund Rot
				case "555A": // KesselT Soll
					TextBox0810.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					break;
				case "080A": // RL Temp
					if (mydataGridView1["len", Reihe].Value.ToString()!="0")
						//wenn len=0, dann wird RL-Temp nicht von Therme geliefert und wird berechnet
					{
						label7.Visible = true;
						TextBoxRL.Visible = true;
						TextBoxRL.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					}
					break;

				case "0808": // Temperatur Sensor 15 Abgas
					TextBox0808.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					break;
				case "083E": // Abgas Sensor 2  Status ok nicht ok
					if (mydataGridView1["Wert_Val", Reihe].Value.ToString()!="0") // Wert val
					{
						TextBox0808.BackColor = Color.Red;
					}
					else {
						TextBox0808.BackColor = Color.LightGreen;
					}
					break; // Hintergrund Rot

				case "0804": // Temperatur Sensor 5 WW-Speicher
					TextBox0804.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					break;
				case "083C": // WW-Speicher Sensor   Status ok nicht ok
					if (mydataGridView1["Wert_Val", Reihe].Value.ToString()!="0") // Wert val
					{
						TextBox0804.BackColor = Color.Red;
					}
					else {
						TextBox0804.BackColor = Color.LightGreen;
					}
					break; // Hintergrund Rot
				case "6500": // WW SpeicherT Soll
					TextBox0812.Text = String.Format("{0:0.0}", mydataGridView1["Wert_Val", Reihe].Value);
					break;

				case "A38F": // Brennerleistung
					TextBox0818.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
					if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()!="00 ") {
						Label0842.BackColor = Color.Green;
					}
					else {
						Label0842.BackColor = Color.LightGray;
					}

					int l = 0;
					while ((l<(mydataGridView1.RowCount)) && (mydataGridView1["addr", l].Value.ToString()!="080A")) {
						l++;
					}
					if ((l<(mydataGridView1.RowCount)) && (mydataGridView1["len", l].Value.ToString()=="0"))
						//wenn len=0, dann wird RL-Temp nicht von Therme geliefert und wird berechnet
					{
						float brennerleistung;
						convertok = float.TryParse(mydataGridView1["Wert_Val", Reihe].Value.ToString(), out brennerleistung);
						if ((convertok) && (volstrom!=0) && (volstrom!=9999) && (kesseltemp!=0)) {
							label7.Visible = true;
							TextBoxRL.Visible = true;
							TextBoxRL.Text = (kesseltemp-(brennerleistung/volstrom*maxpower*0.8571579)).ToString("00.0");
						}
						else {
							if ((volstrom==9999) || (!convertok)) {
								TextBoxRL.Text = "";
							}
							else {
								TextBoxRL.Text = kesseltemp.ToString("00.0");
							}
						}

						mydataGridView1["Wert_Val", l].Value = TextBoxRL.Text;
					}
					break;

				case "0A10": // Umaschaltventil
					switch (mydataGridView1["Wert_Val", Reihe].Value.ToString()) {
						case "0":
							TextBox081A.Text = "nicht def.";
							break;
						case "1":
							TextBox081A.Text = "Heizen";
							break;
						case "2":
							TextBox081A.Text = "Mittelst.";
							break;
						case "3":
							TextBox081A.Text = "Warmwasser";
							break;
						default:
							break;
					}
					break;

				case "0A3C": // Interne Pumpe 
					if (mydataGridView1["len", Reihe].Value.ToString()!="0") {
						TextBox080C.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
						if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()!="00 ") {
							Label3906.BackColor = Color.Green;
						}
						else {
							Label3906.BackColor = Color.LightGray;
						}
					}
					break;
					//case "7660":  // Interne Pumpe Status
					//    if (mydataGridView1["Wert_Hex", Reihe].Value == null) break;
					//    if (mydataGridView1["Wert_Hex", Reihe].Value.ToString() == "01 ")
					//    { Label3906.BackColor = Color.Green; }
					//     else { Label3906.BackColor = Color.LightGray; } break;


				case "6513": // Speicherladepumpe
					if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
					if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()=="01 ") {
						Label0845.BackColor = Color.Green;
					}
					else {
						Label0845.BackColor = Color.LightGray;
					}
					break;

				case "0C24": // Volumenstrom
					if (mydataGridView1["len", Reihe].Value.ToString()!="0") {
						label9.Visible = true;
						Label11.Visible = true;
						TextBox080A.Visible = true;
						convertok = float.TryParse(mydataGridView1["Wert_Val", Reihe].Value.ToString(), out volstrom);
						if (convertok) {
							TextBox080A.Text = (volstrom*10).ToString();
						}
						else {
							volstrom = 9999;
							TextBox080A.Text = "";
						}
					}
					break;

				case "6515": // Zirkulationspumpe 
					if (mydataGridView1["len", Reihe].Value.ToString()!="0") {
						if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
						if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()=="01 ") {
							Label0846.BackColor = Color.Green;
						}
						else {
							Label0846.BackColor = Color.LightGray;
						}
					}
					break;

				case "0A82": // Sammelstoerung
					if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
					if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()=="01 ") {
						Label0883.BackColor = Color.Red;
					}
					else {
						Label0883.BackColor = Color.LightGray;
					}
					break;

				case "5738": // Brennerstoerung
					if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
					if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()!="00 ") {
						label29.BackColor = Color.Red;
					}
					else {
						label29.BackColor = Color.LightGray;
					}
					break;

				case "088A": // Brennerstarts
					TextBox088A.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "08A7": // Brennerstunden
					TextBox08A7.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "2323": // Betriebsart 
					byte mode = byte.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString());
					if (mode<5) CB_Betriebsart.SelectedIndex = mode;
					break;
				case "2302": // Checkbox Sparbetrieb M2
					if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
					if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()=="01 ") {
						ChB_Sparbetrieb.Checked = true;
					}
					else {
						ChB_Sparbetrieb.Checked = false;
					}
					break;
				case "2303": // Checkbox Partybetrieb M2
					if (mydataGridView1["Wert_Hex", Reihe].Value==null) break;
					if (mydataGridView1["Wert_Hex", Reihe].Value.ToString()=="01 ") {
						ChB_Partybetrieb.Checked = true;
					}
					else {
						ChB_Partybetrieb.Checked = false;
					}
					break;
				case "2308": // Raumtemperatur Party Soll
					TextBoxRTS_Party.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "6300": // WW Soll
					TextBoxWWS.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "27d4": // M2 Niveau
					TextBox3304.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "27d3": // M2 Neigung
					TextBox3305.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "2306": // Raumtemp Tag soll
					TextBoxRTS_Tag.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					convertok = int.TryParse(mydataGridView1["Wert_Val", Reihe].Value.ToString(), out RTStemp);
					break;

				case "2307": // Raumtemp Nacht soll
					TextBoxRTS_Nacht.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;

				case "27FA": // KTS Erhoehung nach red. Betrieb
					TB_ErhoehungKTS.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "27FB": // KTS Erhoehungszeit nach red. Betrieb
					TB_ErhoehungszeitKTS.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "27E7": // Pumpenleistung min bei Normbetrieb
					TB_PlstminbeiNorm.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "27E6": // Pumpenleistung max bei Normbetrieb
					TB_PlstmaxbeiNorm.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "27E8": // Pumpenleistung bei red. Betrieb ?
					if (mydataGridView1["Wert_Val", Reihe].Value.ToString()=="1")
						ChB_PlstbeiRed.Checked = true;
					else
						ChB_PlstbeiRed.Checked = false;
					break;
				case "27E9": // Pumpenleistung bei red. Betrieb
					TB_PlstbeiRed.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "676C": // Pumpenleistung bei WW Bereitung
					TB_PumpebeiWW.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;

				case "6759": // WW Hysterese 
					byte wwhysterese = byte.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString());
					if (wwhysterese<11) CB_WWHysterese.SelectedIndex = wwhysterese;
					break;

				case "27A3": // Frostschutztemp 
					byte frostschutz = byte.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString());
					if (frostschutz<16) CB_Frostschutztemp.SelectedIndex = frostschutz;
					break;
				case "8832": // Max. Brennerleistung bei Normheizbetrieb
					TB_MaxBrennerNH.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "676F": // Max. Brennerleistung bei WW Bereitung
					TB_MaxBrennerWW.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "6760": // Max. Delta KTS zu WWS bei WW Bereitung
					TB_MaxDeltaKTWW.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "6762": // Pumpennachlaufzeit bei WW Bereitung
					TB_NachlaufWW.Text = mydataGridView1["Wert_Val", Reihe].Value.ToString();
					break;
				case "7790": // Dämpfung AT
					TB_DaempfungAT.Text = (int.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString())*10).ToString();
					break;
				case "6773": // Frequenz Zirkulationpumpe 
					byte zirkufrequ = byte.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString());
					if (zirkufrequ<8) CB_ZirkuFrequ.SelectedIndex = zirkufrequ;
					break;
				case "27A5": // Sparschaltung HK-Pumpe 
					byte sparhkpumpe = byte.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString());
					if (sparhkpumpe<16) {
						CB_SparHK.SelectedIndex = sparhkpumpe;
						SparA5 = RTStemp+6-sparhkpumpe;
						if (sparhkpumpe==0) SparA5 = -20;
					}
					break;
				case "27A6": // Sparschaltung Brenner und HK-Pumpe 
					byte sparbrenner = byte.Parse(mydataGridView1["Wert_Val", Reihe].Value.ToString());
					if ((sparbrenner>4) && (sparbrenner<37)) {
						CB_SparBrenner.SelectedIndex = sparbrenner-5;
						SparA6 = sparbrenner+1;
						if (sparbrenner==36) SparA6 = -20;
					}
					break;
				default:
					break;

			}
		}

		private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			// Klick in eine Zelle auswerten
			//String msg = String.Format("Row: {0}, Column: {1} Checked: {2}", mydataGridView1.CurrentCell.RowIndex, mydataGridView1.CurrentCell.ColumnIndex, mydataGridView1.CurrentCell.Value);
			//     MessageBox.Show(msg, "Current Cell");

			// wenn die Daten gespeichert werden sollen und es wurde ein Selectfeld verändert, muss eine neue Dateie erstellt werden
			if (mydataGridView1.CurrentCell.ColumnIndex==0) // Wenn selektiert dann..
			{
				checkBox1.Checked = false;
				//if (mydataGridView1.Columns["Akt."].State == true) mydataGridView1["Akt.", mydataGridView1.CurrentCell.RowIndex].Value = "0";
			}


		}

		#endregion

		public void Lese_Steuerungen()
		{
			// Parameter aus XML lesen
			// Parameter aus XML lesen
			XmlDocument ser_config = new XmlDocument();
			ser_config.Load(System.IO.Directory.GetCurrentDirectory()+"\\"+cfg_DateiName);
			XmlNodeList deviceliste = ser_config.GetElementsByTagName("device");
			int z = 0;
			//  string[] Device_Array = new string[deviceliste.Count];

			foreach (XmlNode node in deviceliste) {
				if (node!=null) //kein node in Datei
				{

					XmlAttribute xmlAttr0 = deviceliste[z].Attributes["ID"];
					Device_ID_Array[z] = xmlAttr0.InnerText;
					XmlAttribute xmlAttr1 = deviceliste[z].Attributes["name"];
					Device_name_Array[z] = xmlAttr1.InnerText;
					XmlAttribute xmlAttr2 = deviceliste[z].Attributes["protocol"];
					Device_protocol_Array[z] = xmlAttr2.InnerText;
					z++;
				}


			}
		}

		

		private bool Open_mySerialPort()
		{
				if (!mySerialPort.IsOpen) {
					//mySerialPort.Close();
					mySerialPort.PortName = config.PortName;
					mySerialPort.BaudRate = config.BaudRate;
					mySerialPort.Parity = config.Parity;
					mySerialPort.DataBits = config.DataBits;
					mySerialPort.StopBits = config.StopBits;
					// mySerialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandshake.SelectedItem.ToString());


					mySerialPort.Open();

				}
				return true;

		}

		public static int BerechneMinute() // Static damit es allen bkannt ist
		{
			// Welche Minute ist seit Montag 0Uhr vergangen?
			//  normalerweise beginnt die Woche mit dem Sonntag = Day of week = 0
			// da die Kalenderwoche jedoch mit dem Montag beginnen soll muss umgerechnet werden.

			DateTime jetzt = DateTime.Now;
			// DateTime jetzt = DateTime.Now.DayOfWeek;
			// DateTime jetzt = new DateTime(2011, 7, 30);

			int TDW = (int)jetzt.DayOfWeek; // Tag der Woche Sonntag = 0, Montag = 1, ..., Samstag = 6
			if (TDW!=0) TDW = TDW-1;
			else TDW = TDW+6;

			//   DateTime jetzt = DateTime.Now;
			int Min_Seit_Montag = TDW*24*60+jetzt.Hour*60+jetzt.Minute;
			return Min_Seit_Montag;
		}

		private void Beende_Ser_Uebertragung()
		{
			// MessageBox.Show("hallo ende");
			int Minuten_seit_Montag = 0;

			Ser_Uebertragungsstatus = 0; // dann alles  stoppen
			Reihe = 0;
			gelesener_Datenpunkt = 0;

			this.t1.Stop();
			this.Text = Appname+" - nicht verbunden ";
			this.toolStripLabel2.BackColor = Color.Red;
			//if (mySerialPort.IsOpen) 
			//    mySerialPort.Close();

			// nur Selektierte speichern vielleicht RAM-Disk?
			//if (lese_alle_Datenpunkte == false)  //c1
			{


				Minuten_seit_Montag = BerechneMinute();
				if (Minuten_seit_Montag_alt==Minuten_seit_Montag) {
					goto jump1; // nicht speichern
				}

				//neue Datei anlegen Sonntag 0Uhr
				//  int Minuten_seit_Montag = 0;

				if (Minuten_seit_Montag==0) {
					//speichere_daten(Minuten_seit_Montag);
					goto jump1;
				}

				if (File.Exists(DataFilename)) // nur wenn die Datei existiert
					// Uhrzeit + ";" schreiben, anschließende alle Werte + ";" und noch ein CRLF 
				{
						//speichere_daten(Minuten_seit_Montag);

				}
			}
			jump1:
			Minuten_seit_Montag_alt = Minuten_seit_Montag;
			//toolStripButton2.Image = Properties.Resources.Player_Play;  // Bildchen umschalten
			//toolStripButton2_pressed = false;      // Taster als nicht betaetigt markkieren  

			if (toolStripComboBox1.SelectedItem.ToString()!="Stop") {
				t2.Start(); // Nächsten abfragezylus einschalten
			}


		}


		private void fillTimerComboBox1()
		{
			this.toolStripComboBox1.Items.AddRange(new object[] {"Stop", "20 Sek", "30 Sek", "40 Sek", "50 Sek",});
			this.toolStripComboBox1.SelectedIndex = this.toolStripComboBox1.Items.IndexOf("40 Sek"); //Voreinstellung
		}

		private void fill_Graf_LaengeComboBox()
		{
			this.toolStripComboBox2.Items.AddRange(new object[]
			{"1h", "6h", "12h", "1Tag", "2Tage", "3Tage", "4Tage", "5Tage", "6Tage", "7Tage"});
			//     this.toolStripComboBox2.SelectedIndex = this.toolStripComboBox2.Items.IndexOf("1Tag"); //Voreinstellung
		}

		private void Fill_Zeiten(string Adresse)
		{
			Reihe_Zeiten = 0; // Anfangswert starten

			while (mydataGridView1["addr", Reihe_Zeiten].Value.ToString()!=Adresse) {
				Reihe_Zeiten++;
			}

			int pos = 0;
			for (int i = 0; i<=55; i++) {
				if (mydataGridView1["Wert_Hex", Reihe_Zeiten].Value==null) {
					this.tabControl1.Controls.Find("TextBox"+Convert.ToString(i+100), true)[0].Text = "n.v.";
				}
				else {
					this.tabControl1.Controls.Find("TextBox"+Convert.ToString(i+100), true)[0].Text = TextReturn(pos);
				}

				pos++;
				if (pos==8) {
					Reihe_Zeiten = Reihe_Zeiten+pos;
					pos = 0;
				}
			}

			Reihe_Zeiten = 0; // Anfangswert wieder setzen
		}

		private string TextReturn(int pos)
		{
			String TextRet;
			byte Zeit = byte.Parse(mydataGridView1["Wert_hex", Reihe_Zeiten].Value.ToString().Substring(pos*3, 2),
				System.Globalization.NumberStyles.HexNumber); // Substring(ab wann,länge)
			if (Zeit==0xff) {
				TextRet = "--:--";
			}
			else {
				TextRet = (Zeit>>3).ToString("00")+":"+((Zeit&0x7)*10).ToString("00");
			}
			return (TextRet);
		}


		private void load_maxpower()
		{

			XmlDocument therme_config = new XmlDocument();
			therme_config.Load(System.IO.Directory.GetCurrentDirectory()+"\\"+cfg_DateiName);
			XmlNodeList therme_Liste = therme_config.GetElementsByTagName("Therme");

			foreach (XmlNode node in therme_Liste) {
				int z = 0;
				if (node!=null) //kein node in Datei
				{
					XmlNode xmlNode1 = therme_Liste[z].SelectSingleNode("MaxPower");
					maxpower = float.Parse(xmlNode1.InnerText, CultureInfo.InvariantCulture);
					z++;
				}
			}
		}

		private void save_maxpower()
		{

			XmlDocument therme_config = new XmlDocument();
			therme_config.Load(System.IO.Directory.GetCurrentDirectory()+"\\"+cfg_DateiName);
			XmlNodeList therme_Liste = therme_config.GetElementsByTagName("Therme");

			foreach (XmlNode node in therme_Liste) {
				int z = 0;
				if (node!=null) //kein node in Datei
				{
					XmlNode xmlNode1 = therme_Liste[z].SelectSingleNode("MaxPower");
					xmlNode1.InnerText = maxpower.ToString();

					z++;
				}
			}

			therme_config.Save(System.IO.Directory.GetCurrentDirectory()+"\\"+cfg_DateiName);
		}

		private void Main_Form_Shown(object sender, EventArgs e)
		{
			if (auto_start) toolStripButton2_Click(toolStripButton2, null);
		}

		private void send_parameter(ushort address, byte value)
		{
			My_Serial_Output_Buffer[0] = 0x41; // Telegrammstart
			My_Serial_Output_Buffer[1] = 0x06; // Nutzdaten, hier 6
			My_Serial_Output_Buffer[2] = 0x00; // Anfrage
			My_Serial_Output_Buffer[3] = 0x02; // Schreiben
			My_Serial_Output_Buffer[4] = (byte)(address>>8);
			My_Serial_Output_Buffer[5] = (byte)(address%0x100);
			My_Serial_Output_Buffer[6] = 0x01; // Länge mit de hex wandlung
			My_Serial_Output_Buffer[7] = value;
			My_Serial_Output_Buffer[8] = calc_CRC(My_Serial_Output_Buffer);
			if (!mySerialPort.IsOpen) Open_mySerialPort();
			mySerialPort.Write(My_Serial_Output_Buffer, 0, 9); // Buffer senden
			//mySerialPort.Close();
			if (lese_alle_Datenpunkte) lese_alle_Datenpunkte_zweimal = true;
			lese_alle_Datenpunkte = true; // Beim 1. Durchlauf alle lesen
			t1.Interval = 50;
			this.t1.Start(); // Timer starten
		}

		private void CB_Betriebsart_Leave(object sender, EventArgs e)
		{
			int value = ((ComboBox)sender).SelectedIndex;
			if ((value>=0)&(value<5))
				send_parameter(0x2323, (byte)value);
		}

		private void TextBoxRTS_Tag_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>0)&(value<30))
				send_parameter(0x2306, value);
		}

		private void TextBoxRTS_Nacht_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>0)&(value<30))
				send_parameter(0x2307, value);
		}

		private void TextBoxRTS_Party_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>0)&(value<30))
				send_parameter(0x2308, value);
		}

		private void TextBoxWWS_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>30)&(value<80))
				send_parameter(0x6300, value);
		}

		private void TextBoxHKLNiveau_Leave(object sender, EventArgs e)
		{
			float value = float.Parse(((TextBox)sender).Text);
			if ((value>=-15)&(value<=40))
				send_parameter(0x27d4, (byte)value);
		}

		private void TextBoxHKLNeigung_Leave(object sender, EventArgs e)
		{
			float valuef = float.Parse(((TextBox)sender).Text);
			byte value = (byte)(valuef*10+0.5);
			if ((value>=2)&(value<=35))
				send_parameter(0x27d3, value);
		}

		private void TB_ErhoehungKTS_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=0)&(value<=50))
				send_parameter(0x27FA, value);
		}

		private void TB_ErhoehungszeitKTS_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=0)&(value<=150))
				send_parameter(0x27FB, value);
		}


		private void TB_PlstminbeiNorm_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=0)&(value<=100))
				send_parameter(0x27E7, value);
		}

		private void TB_PlstmaxbeiNorm_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=0)&(value<=100))
				send_parameter(0x27E6, value);
		}

		private void ChB_PlstbeiRed_Leave(object sender, MouseEventArgs e)
		{
			if (((CheckBox)sender).Checked)
				send_parameter(0x27E8, 1);
			else
				send_parameter(0x27E8, 0);
		}

		private void TB_PlstbeiRed_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=0)&(value<=100))
				send_parameter(0x27E9, value);
		}

		private void TB_PumpebeiWW_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>0)&(value<=100))
				send_parameter(0x676C, value);
		}

		private void CB_WWHysterese_Leave(object sender, EventArgs e)
		{
			int value = ((ComboBox)sender).SelectedIndex;
			if ((value>=0)&(value<11))
				send_parameter(0x6759, (byte)value);
		}

		private void CB_Frostschutztemp_Leave(object sender, EventArgs e)
		{
			int value = ((ComboBox)sender).SelectedIndex;
			if ((value>=0)&(value<16))
				send_parameter(0x27A3, (byte)value);
		}

		private void TB_MaxBrennerHK_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=8)&(value<=100))
				send_parameter(0x8832, value);
		}

		private void TB_MaxBrennerWW_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>0)&(value<=100))
				send_parameter(0x676F, value);
		}

		private void TB_MaxDeltaKTWW_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=5)&(value<=25))
				send_parameter(0x6760, value);
		}

		private void TB_NachlaufWW_Leave(object sender, EventArgs e)
		{
			byte value = byte.Parse(((TextBox)sender).Text);
			if ((value>=0)&(value<=15))
				send_parameter(0x6762, value);
		}

		private void TB_DaempfungAT_Leave(object sender, EventArgs e)
		{
			int value = int.Parse(((TextBox)sender).Text);
			if ((value>=10)&(value<=1990))
				send_parameter(0x7790, (byte)(value/10));
		}

		private void CB_ZirkuFrequ_Leave(object sender, EventArgs e)
		{
			int value = ((ComboBox)sender).SelectedIndex;
			if ((value>=0)&(value<8))
				send_parameter(0x6773, (byte)value);
		}

		private void CB_SparHK_Leave(object sender, EventArgs e)
		{
			int value = ((ComboBox)sender).SelectedIndex;
			if (value<16)
				send_parameter(0x27A5, (byte)value);
		}

		private void CB_SparBrenner_Leave(object sender, EventArgs e)
		{
			int value = ((ComboBox)sender).SelectedIndex+5;
			if ((value>4) && (value<37))
				send_parameter(0x27A6, (byte)value);
		}

		private void textBox3__Leave(object sender, EventArgs e)
		{
			double value = double.Parse(((TextBox)sender).Text);

			if ((value>5)&(value<=50)) {
				maxpower = value;
			}

		}

		private void TextBox_Enter(object sender, KeyEventArgs e)
		{
			if (e.KeyCode==Keys.Enter) {
				//    Control nextctl;
				//nextctl = GetNextControl((TextBox)sender,true);
				tabControl1.Focus();
			}
		}



	} // end class
}


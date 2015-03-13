using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Timers;

namespace ViessmannControl
{
    public class Protocol300Boiler
    {
			private Timer t1;

			private SerialPort serialPort = new SerialPort();
	    private Protocol300BoilerConnectionConfiguration configuration;
			private byte[] portInputBuffer = new byte[256];
			private byte[] portOutputBuffer = new byte[256];
	    private int inputBufferOffset = 0;

			private void OpenPort()
			{
				if (!serialPort.IsOpen) {
					//mySerialPort.Close();
					serialPort.PortName = configuration.PortName;
					serialPort.BaudRate = configuration.BaudRate;
					serialPort.Parity = configuration.Parity;
					serialPort.DataBits = configuration.DataBits;
					serialPort.StopBits = configuration.StopBits;
					serialPort.Open();
				}
			}

			private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
			{
				var butesToRead = serialPort.BytesToRead; // puffern, da BytesToRead nach lesen = 0
				serialPort.Read(portInputBuffer, inputBufferOffset, butesToRead);
				inputBufferOffset = inputBufferOffset + butesToRead;
				//Inner_Connect();
				Timer1_Trigger();  // Timer triggern solange Daten ankommen
			}

			private void Timer1_Trigger()
			{
				// solange Zeichen reinkommen wird der Timer1 getriggert. Kommt kein Zeichen mehr wird 
				// nach Ablauf der Zeit der Int. ausgelöst 
				t1.Stop();
				t1.Interval = 50;
				t1.Start();
			}

	    private byte calc_CRC(byte[] telegram)
	    {
		    uint CRCsum = 0;
		    byte telestart = 1;
		    var teleend = (byte)(telegram[1]+1);

		    if (telegram[0]!=0x41) // vielleicht noch ein 0x06 davor?
		    {
			    telestart++;
			    teleend = (byte)(telegram[2]+2);
			    if ((telegram[0]!=0x06)&(telegram[1]!=0x41)) return (0);
		    }
		    for (var i = telestart; i<=teleend; i++) {
			    CRCsum += telegram[i];
		    }
		    return ((byte)(CRCsum%0x100));
	    }


			private bool CheckInputBufferCrc()
			{
				//return true;
				return (calc_CRC(portInputBuffer) == portInputBuffer[(portInputBuffer[2] + 3) % 0x100]);
			}

	    public void Initialize()
	    {
	    }

	    //private int connectionState = 0;
	    public void ReadData(Action readCompleted)
	    {

	    }

			private void SendParameter(DataPoint dataPoint, byte value)
			{
				var address = (byte)dataPoint;
				portOutputBuffer[0] = 0x41; // Telegrammstart
				portOutputBuffer[1] = 0x06; // Nutzdaten, hier 6
				portOutputBuffer[2] = 0x00; // Anfrage
				portOutputBuffer[3] = 0x02; // Schreiben
				portOutputBuffer[4] = (byte)(address >> 8);
				portOutputBuffer[5] = (byte)(address % 0x100);
				portOutputBuffer[6] = 0x01; // Länge mit de hex wandlung
				portOutputBuffer[7] = value;
				portOutputBuffer[8] = calc_CRC(portOutputBuffer);
				if (!serialPort.IsOpen)
					OpenPort();
				serialPort.Write(portOutputBuffer, 0, 9); // Buffer senden
				//t1.Start();
			}

			public void SetOperatingData(OperatingData boilerStatus)
			{
				SendParameter(DataPoint.OperatingData, (byte)boilerStatus);
			}

	    public void SetRoomTemperatureStandard(byte temperature)
	    {
				if (temperature<3 || temperature>37)
					throw new ArgumentOutOfRangeException("temperature");
				SendParameter(DataPoint.SetRoomTemperatureStandard, temperature);
	    }

	    private Action onConnected;
	    public void Connect(Action onConnected)
	    {
		    this.onConnected = onConnected;
				t1 = new Timer(50);
		    t1.Elapsed += (sender, args) => Inner_Connect(); // Intervall festlegen, hier 15 ms
				t1.Start();
		    //t1.Tick += (sender, args) => Inner_Connect(); // Eventhandler ezeugen der beim Timerablauf aufgerufen wird

		    //Inner_Connect();
	    }

	    private bool isConnected;
	    private int connectionStatus = 0;
	    private void Inner_Connect()
	    {
				t1.Stop();

				if (connectionStatus>1)
					return;

				if (!serialPort.IsOpen)
					OpenPort();

				var ibo = inputBufferOffset;
				inputBufferOffset = 0;
		    switch (connectionStatus) {
			    case 0:
				    if (ibo==0) portInputBuffer[0] = 0x04;

				    switch (portInputBuffer[0]) {
					    case 0x05:
						    portOutputBuffer[0] = 0x16;
								portOutputBuffer[1] = 0x00;
								portOutputBuffer[2] = 0x00;
						    serialPort.Write(portOutputBuffer, 0, 3); // Buffer, ab 0, von 0 bis 2, = 3 Bytes senden
								this.t1.Start();
						    //Thread.Sleep(50);
						    //Inner_Connect();
						    break;
					    case 0x06:
								portOutputBuffer[0] = 0x41;
								portOutputBuffer[1] = 0x05;
								portOutputBuffer[2] = 0x00;
								portOutputBuffer[3] = 0x01;
								portOutputBuffer[4] = 0x00;
								portOutputBuffer[5] = 0xF8;
						    portOutputBuffer[6] = 0x02; // Ich erwarte 2 Zeichen 
								portOutputBuffer[7] = calc_CRC(portOutputBuffer);
								connectionStatus = 1;
								serialPort.Write(portOutputBuffer, 0, 8);
								this.t1.Start();
						    //return;
						    //Thread.Sleep(50);
								//Inner_Connect();
						    break;
					    default:
								portOutputBuffer[0] = 0x04;
						    serialPort.Write(portOutputBuffer, 0, 1); // Buffer, ab 0, 1 Byte senden
						    //Thread.Sleep(50);
								//Inner_Connect();
								this.t1.Start();
						    break;
				    }
				    break;
					case 1: // 1 = ich wartete auf Anlagenkennung 
						if ((portInputBuffer[0] != 0x06) || (ibo == 0) || (!(CheckInputBufferCrc()))) {
							connectionStatus = 0;
							break;
						}
				    if (portInputBuffer[7]==portOutputBuffer[6]) // erwartete Anzahl war korrekt
				    {
					    /*for (var z = 0; z<35; z++) {
							if (!(String.IsNullOrEmpty(Device_ID_Array[z]))&
									(Device_ID_Array[z].Contains(portInputBuffer[8].ToString("X2") +
																							 portInputBuffer[9].ToString("X2")))) {
								Console.WriteLine("Connected with:  "+Device_name_Array[z]+"   Protocol:  "+Device_protocol_Array[z]);
								connectionStatus = 2;
								break;
							}
						}*/
					    //if (connectionStatus != 2) {
					    Console.WriteLine("Connected with:  "+portInputBuffer[8].ToString("X2")+portInputBuffer[9].ToString("X2"));
					    connectionStatus = 2;
					    onConnected();
					    //}
					    // bis zum 1. selektierten springen
					    /*if (Reihe>=mydataGridView1.RowCount) Reihe = 0;

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
						gelesener_Datenpunkt++; //Zähler für Textbox*/

				    }
				    else // wenn die Anzahl nicht korrekt war 
				    {
					    connectionStatus = 0;
				    }
				    break;
					//default:
				  //  throw new NotSupportedException();
		    }

		    //Thread.Sleep(25);

		    //isConnected = true;
	    }

	    public void Disconnect()
	    {
				serialPort.Close();
	    }

	    public Protocol300Boiler(Protocol300BoilerConnectionConfiguration config)
	    {
				configuration = config;
				serialPort.DataReceived += serialPort_DataReceived;
	    }
    }
}

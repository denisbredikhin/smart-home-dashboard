using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViessmannControl
{
    public class Protocol300Boiler
    {
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
				//this.Invoke(new EventHandler(Timer1_Trigger));  // Timer triggern solange Daten ankommen
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
				return (calc_CRC(portInputBuffer) == portInputBuffer[(portInputBuffer[2] + 3) % 0x100]);
			}

	    public void Initialize()
	    {
	    }

	    private int connectionState = 0;
	    public void ReadData(Action readCompleted)
	    {
		    /*OpenPort();

				var inputOffset = this.inputBufferOffset;  // wie viele Zeichen waren es?
				this.inputBufferOffset = 0;      // Für das nächste Telegramm  

				var z = 0;
		    connectionState = 0;

				switch (connectionState) {
					case 0:   // 0 = Empfangsbereit
						if (inputOffset == 0) 
							portInputBuffer[0] = 0x04;
						switch (portInputBuffer[0])  // Empfang im Ruhezustand 05h
						{
							case 0x05:
								portOutputBuffer[0] = 0x16;
								portOutputBuffer[1] = 0x00;
								portOutputBuffer[2] = 0x00;
								serialPort.Write(portOutputBuffer, 0, 3); // Buffer, ab 0, von 0 bis 2, = 3 Bytes senden
								Thread.Sleep(50);
								ReadData(readCompleted);
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
								connectionState = 1;
								serialPort.Write(portOutputBuffer, 0, 8);
								Thread.Sleep(50);
								ReadData(readCompleted);
								break;
							default:
								portOutputBuffer[0] = 0x04;
								serialPort.Write(portOutputBuffer, 0, 1); // Buffer, ab 0, 1 Byte senden
								Thread.Sleep(50);
								ReadData(readCompleted);
								break;
						}
						break;

					case 1:    // 1 = ich wartete auf Anlagenkennung 
						if ((portInputBuffer[0] != 0x06) | (inputOffset == 0) | (!(CheckInputBufferCrc()))) {
							connectionState = 0;
							break;
						}
						if (portInputBuffer[7] == portInputBuffer[6])// erwartete Anzahl war korrekt
                    {
							for (z = 0; z < 35; z++) {
								if (!(String.IsNullOrEmpty(Device_ID_Array[z])) & (Device_ID_Array[z].Contains(portInputBuffer[8].ToString("X2") + portInputBuffer[9].ToString("X2")))) {
									this.Text = Appname + " - verbunden mit:  " + Device_name_Array[z] + "   Protokoll:  " + Device_protocol_Array[z];
									this.toolStripLabel2.BackColor = Color.LightGreen;
									connectionState = 2;
									break;
								}
							}
							if (connectionState != 2) {
								this.Text = Appname + " - verbunden mit Device: " + My_Serial_Input_Buffer[8].ToString("X2") + My_Serial_Input_Buffer[9].ToString("X2");
								this.toolStripLabel2.BackColor = Color.LightGreen;
								connectionState = 2;
							}
							// bis zum 1. selektierten springen
							if (Reihe >= mydataGridView1.RowCount) Reihe = 0;

							if (lese_alle_Datenpunkte == false) {
								//String ttt = mydataGridView1["Akt.", Reihe].Value.ToString();
								while (Reihe < (mydataGridView1.RowCount)) {
									if ((mydataGridView1["Akt.", Reihe].Value.ToString() == "0") || (mydataGridView1["len", Reihe].Value.ToString() == "0"))
										Reihe++; //Checkbox = 0 dann Reihe überspringen
									else
										break;
								}
								// Abfrage ob es die letzte Reihe war und die war ebenfalls deaktiviert
								if (Reihe >= (mydataGridView1.RowCount)) {
									TextBox_status.Text = "Achtung! keine Datenpunkte selektiert";
									Ser_Uebertragungsstatus = 0;  // dann alles  stoppen
									Reihe = 0;
									gelesener_Datenpunkt = 0;
									this.Text = Appname + " - nicht verbunden ";
									this.toolStripLabel2.BackColor = Color.Red;
									return;
								}

							}
							else {
								while (Reihe < (mydataGridView1.RowCount)) {
									if ((mydataGridView1["len", Reihe].Value.ToString() == "0"))
										Reihe++; //len = 0 dann Reihe überspringen
									else
										break;
								}
								// Abfrage ob es die letzte Reihe war und die war ebenfalls deaktiviert
								if (Reihe >= (mydataGridView1.RowCount)) {

									Beende_Ser_Uebertragung();
									return;
								}

							}

							// wenn einer selektiert war, dann wird er hier angefordert 
							My_Serial_Output_Buffer[0] = 0x41;  // Telegrammstart
							My_Serial_Output_Buffer[1] = 0x05;  // Nutzdaten, hier immer 5
							My_Serial_Output_Buffer[2] = 0x00;  // Anfrage
							My_Serial_Output_Buffer[3] = 0x01;  // Lesen
							My_Serial_Output_Buffer[4] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
							My_Serial_Output_Buffer[5] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
							My_Serial_Output_Buffer[6] = byte.Parse(mydataGridView1["len", Reihe].Value.ToString()); // Länge mit de hex wandlung
							My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
							mySerialPort.Write(My_Serial_Output_Buffer, 0, 8);  // Buffer senden
							t1.Interval = 50;
							this.t1.Start(); // Timer starten
							gelesener_Datenpunkt++;  //Zähler für Textbox

						}
						else  // wenn die Anzahl nicht korrekt war 
                    {
							Ser_Uebertragungsstatus = 0;
						}
						break;

					case 2:
						// hier kommt Wert für Wert rein
						if ((My_Serial_Input_Buffer[0] == 0x06) // Status ok
							//& (My_Serial_Input_Buffer[3] == 0x01)  //Antwort ohne Fehler
								& (My_Serial_Input_Buffer[7] == My_Serial_Output_Buffer[6]) // erwartete Anzahl war korrekt
								& (M_S_I_B_Z == 9 + My_Serial_Output_Buffer[6])
								& (check_CRC())) {
							toolStripTextBox1.Text = Reihe.ToString();  // Anzeige der ausgelesenen Reihe
							toolStripTextBox2.Text = gelesener_Datenpunkt.ToString();


							for (int i = 0; i < My_Serial_Output_Buffer[6]; i++) // Anzeige der HEX-Werte
                        {
								string zeichen = My_Serial_Input_Buffer[8 + i].ToString("X2");
								zeichen1 = zeichen1 + zeichen + " ";
							}
							mydataGridView1["Wert_Hex", Reihe].Value = zeichen1;

							zeichen1 = "";

							for (int i = 0; i < My_Serial_Output_Buffer[6]; i++) // Anzeige der dez-Werte
                        {
								string zeichen = My_Serial_Input_Buffer[8 + i].ToString("000");
								zeichen1 = zeichen1 + zeichen + " ";
							}
							mydataGridView1["Wert_Dez", Reihe].Value = zeichen1;

							// Datum und Uhrzeit formatieren
							if (mydataGridView1["addr", Reihe].Value.ToString() == "088E") // Anzeige der Value (umgerechneten Werte)
                        {
								string myString;
								myString = Wochentag[(My_Serial_Input_Buffer[12] + 6) % 7] + " " +
								My_Serial_Input_Buffer[11].ToString("X2") + "." + My_Serial_Input_Buffer[10].ToString("X2") + "." +
								My_Serial_Input_Buffer[8].ToString("X2") + My_Serial_Input_Buffer[9].ToString("X2") + " " +
								My_Serial_Input_Buffer[13].ToString("X2") + ":" + My_Serial_Input_Buffer[14].ToString("X2") + ":" + My_Serial_Input_Buffer[15].ToString("X2");
								mydataGridView1["Wert_Val", Reihe].Value = myString;
							}


							// wenn precision nicht leer ist            
							if (mydataGridView1["precision", Reihe].Value.ToString() != "") // Anzeige der Value (umgerechneten Werte)
                        {
								float myValue = 0;

								switch (mydataGridView1["len", Reihe].Value.ToString()) {
									case "1":
										if (mydataGridView1["addr", Reihe].Value.ToString() == "27d4") // Neigung evtl. negativ
                                    {
											int myValue1 = (My_Serial_Input_Buffer[8]);

											if (myValue1 > 0x80) {
												myValue1 = (256 - myValue1);
												myValue = myValue1 * float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
												myValue = -myValue;
											}
											else {
												myValue = myValue1 * float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
											}

											mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);

										}
										else {
											myValue = My_Serial_Input_Buffer[8] * float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
											mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);
										}
										break;
									case "2":
										// negative Temperaturen behandeln
										// Die können nur bei den u. g. Datenpunkten auftreten
										if (mydataGridView1["addr", Reihe].Value.ToString() == "0800" |
												mydataGridView1["addr", Reihe].Value.ToString() == "5525" |
												mydataGridView1["addr", Reihe].Value.ToString() == "5527") // Aussentemp negativ
                                    {
											int myValue1 = ((My_Serial_Input_Buffer[9] << 8) + My_Serial_Input_Buffer[8]);
											if (myValue1 > 0x8000) {

												myValue1 = myValue1 ^ 0xFFFF;
												myValue = myValue1 * float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
												myValue = -myValue;
												mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);

											}
											else {
												myValue = ((My_Serial_Input_Buffer[9] << 8) + My_Serial_Input_Buffer[8]) * float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);
												mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);
											}
											break;
										}
										// hier sind normale Werte, Temperaturen etc.
										myValue = ((My_Serial_Input_Buffer[9] << 8) + My_Serial_Input_Buffer[8]) * float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture);

										mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue, 2);
										break;
									case "4":
										myValue = ((My_Serial_Input_Buffer[11] << 24) + (My_Serial_Input_Buffer[10] << 16)
															+ (My_Serial_Input_Buffer[9] << 8) + (My_Serial_Input_Buffer[8])); // * double.Parse(mydataGridView1["precision", Reihe].Value.ToString());
										mydataGridView1["Wert_Val", Reihe].Value = Math.Round(myValue / float.Parse(mydataGridView1["precision", Reihe].Value.ToString(), CultureInfo.InvariantCulture), 2);

										break;
									default:
										break;
								}
							}

							Screen_Update(Reihe);
							// Bis hier ist die letzte Anforderung verarbeitet worden 

							Reihe++; // Nächste Reihe vorbereiten
							if (Reihe >= (mydataGridView1.RowCount)) // wenn der letzte Datensatz eingelesen war beende Abfrage
                        {
								Beende_Ser_Uebertragung();
								if (lese_alle_Datenpunkte) {
									t2.Interval = 1;
								}
								else {
									t2.Interval = tempt2interval;
								}

								if (lese_alle_Datenpunkte) {
									lese_alle_Datenpunkte = false;  // Nach den 1. Duchlauf beenden
									if (radioButton1.Checked) { radioButton1.Checked = false; radioButton1.Checked = true; }
									if (radioButton2.Checked) { radioButton2.Checked = false; radioButton2.Checked = true; }
									if (radioButton3.Checked) { radioButton3.Checked = false; radioButton3.Checked = true; }

								}
								if (lese_alle_Datenpunkte_zweimal) {
									lese_alle_Datenpunkte_zweimal = false;
									lese_alle_Datenpunkte = true;
								}
								return;
							}

							// bis zum naechsten selektierten springen
							if (lese_alle_Datenpunkte == false) {
								while ((Reihe < mydataGridView1.RowCount) &&
												(mydataGridView1["Akt.", Reihe].Value.ToString() == "0" ||
												mydataGridView1["len", Reihe].Value.ToString() == "0")) {
									Reihe++; //Checkbox = 0 dann Reihe überspringen
								}
								// hier kommt das Prog nur hin, wenn es innnerhalb des Grid ist 
								if (Reihe >= (mydataGridView1.RowCount)) // und letzter Datensatz aber nicht zum Lesen markiert
                                {
									Beende_Ser_Uebertragung();
									if (lese_alle_Datenpunkte) {
										t2.Interval = 1;
									}
									else {
										t2.Interval = tempt2interval;
									}

									lese_alle_Datenpunkte = false;  // Nach den 1. Duchlauf beenden
									if (lese_alle_Datenpunkte_zweimal) {
										lese_alle_Datenpunkte_zweimal = false;
										lese_alle_Datenpunkte = true;
									}
									return;
								}
							}
							else {
								while ((Reihe < mydataGridView1.RowCount) && (mydataGridView1["len", Reihe].Value.ToString() == "0")) {
									Reihe++; //Checkbox = 0 dann Reihe überspringen
								}   // hier kommt das Prog nur hin, wenn es innnerhalb des Grid ist 
								if (Reihe >= (mydataGridView1.RowCount)) // und letzter Datensatz aber nicht zum Lesen markiert
                                {
									Beende_Ser_Uebertragung();
									if (lese_alle_Datenpunkte) {
										t2.Interval = 1;
									}
									else {
										t2.Interval = tempt2interval;
									}

									lese_alle_Datenpunkte = false;  // Nach den 1. Duchlauf beenden
									if (lese_alle_Datenpunkte_zweimal) {
										lese_alle_Datenpunkte_zweimal = false;
										lese_alle_Datenpunkte = true;
									}
									return;
								}

							}

							if (Reihe >= (mydataGridView1.RowCount)) // und letzter Datensatz aber nicht zum Lesen markiert
                        {
								Beende_Ser_Uebertragung();
								if (lese_alle_Datenpunkte) {
									t2.Interval = 1;
								}
								else {
									t2.Interval = tempt2interval;
								}

								lese_alle_Datenpunkte = false;  // Nach den 1. Duchlauf beenden
								if (lese_alle_Datenpunkte_zweimal) {
									lese_alle_Datenpunkte_zweimal = false;
									lese_alle_Datenpunkte = true;
								}
								return;
							}

							My_Serial_Output_Buffer[0] = 0x41;  // Telegrammstart
							My_Serial_Output_Buffer[1] = 0x05;  // Nutzdaten, hier immer 5
							My_Serial_Output_Buffer[2] = 0x00;  // Anfrage
							My_Serial_Output_Buffer[3] = 0x01;  // Lesen
							My_Serial_Output_Buffer[4] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
							My_Serial_Output_Buffer[5] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
							My_Serial_Output_Buffer[6] = byte.Parse(mydataGridView1["len", Reihe].Value.ToString()); // Länge mit de hex wandlung
							My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
							mySerialPort.Write(My_Serial_Output_Buffer, 0, 8);  // Buffer senden
							t1.Interval = 50;
							this.t1.Start(); // Timer starten
							gelesener_Datenpunkt++;  //Zähler für Textbox


						}  //end if wenn es nicht die Zeichenanzahl war
						else {
							My_Serial_Output_Buffer[0] = 0x41;  // Telegrammstart
							My_Serial_Output_Buffer[1] = 0x05;  // Nutzdaten, hier immer 5
							My_Serial_Output_Buffer[2] = 0x00;  // Anfrage
							My_Serial_Output_Buffer[3] = 0x01;  // Lesen
							My_Serial_Output_Buffer[4] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
							My_Serial_Output_Buffer[5] = byte.Parse(mydataGridView1["addr", Reihe].Value.ToString().Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
							My_Serial_Output_Buffer[6] = byte.Parse(mydataGridView1["len", Reihe].Value.ToString()); // Länge mit de hex wandlung
							My_Serial_Output_Buffer[7] = calc_CRC(My_Serial_Output_Buffer);
							mySerialPort.Write(My_Serial_Output_Buffer, 0, 8);  // Buffer senden
							t1.Interval = 50;
							this.t1.Start(); // Timer starten


						}

						break;


					default:
						break;   // alles andere interessiert uns nicht
				}  //switch*/
	    }

			public Protocol300Boiler(Protocol300BoilerConnectionConfiguration config)
	    {
				configuration = config;
				serialPort.DataReceived += serialPort_DataReceived;
	    }
    }
}

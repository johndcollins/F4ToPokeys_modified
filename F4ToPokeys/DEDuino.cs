/*
DEDuino
 * Software connector to allow Transmission of data to supported Arduino code
 * Originally Written by Uri Ben-Avrahm - 2014
 * http://pits.108vfs.org
 * 
 * Adapted to F4ToPokeys by John Collins and Beau Williamsen
*/

using F4SharedMem.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class DEDuino : BindableObject, IDisposable
    {
        #region private variables
        private SerialPort serialPort;
        private char serialBuffer;
        private byte[] blankByte = new byte[1];
        private bool powerOn = false;

        // Lightbits
        private uint lightBits = 0;
        private uint lightBits2 = 0;
        private uint lightBits3 = 0;

        // DEDLines
        private string[] dedLines = null;
        private string[] invLines = null;

        // PFLLines
        private string[] pflLines = null;
        private string[] pflInvert = null;

        // FuelFlow
        private float fuelFlow = 0;
        private float fuelFlow2 = 0;

        // SpeedBrakes
        private uint powerBits = 0;
        private float speeddBrakeValue;

        // CMDS
        private float chaffCount = 0;
        private float flareCount = 0;

        // Engine
        private float oilPressure = 0.0f;
        private float nozzlePos = 0.0f;
        private float rpm = 0.0f;
        private float ftit = 0.0f;

        // Altitude
        private float altitude = 0.0f;
        private float altBaro = 29.92f;

        #endregion

        #region Construction
        public DEDuino()
        {
            RemoveDEDuinoCommand = new RelayCommand(executeRemoveDEDuino);
            
            FalconConnector.Singleton.FlightDataLightsChanged += OnFlightDataLightsChanged;
            FalconConnector.Singleton.FlightDataChanged += OnFlightDataChanged;
            FalconConnector.Singleton.FalconStarted += OnFalconStarted;
            FalconConnector.Singleton.FalconStopped += OnFalconStopped;

            powerOn = (FalconConnector.GetFalconWindowHandle() != IntPtr.Zero);
            NewOldList = new List<string>() { "new", "old" };
        }

        private void OnFalconStopped(object sender, EventArgs e)
        {
            powerOn = false;
        }

        private void OnFalconStarted(object sender, EventArgs e)
        {
            powerOn = true;
        }

        private void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            if (e.newFlightData != null)
            {
                dedLines = e.newFlightData.DEDLines;
                invLines = e.newFlightData.Invert;

                pflLines = e.newFlightData.PFLLines;
                pflInvert = e.newFlightData.PFLInvert;

                fuelFlow = e.newFlightData.fuelFlow;
                fuelFlow2 = e.newFlightData.fuelFlow2;

                powerBits = e.newFlightData.powerBits;
                speeddBrakeValue = e.newFlightData.speedBrake;

                chaffCount = e.newFlightData.ChaffCount;
                flareCount = e.newFlightData.FlareCount;

                oilPressure = e.newFlightData.oilPressure;
                nozzlePos = e.newFlightData.nozzlePos;
                rpm = e.newFlightData.rpm;
                ftit = e.newFlightData.ftit;

                altitude = e.newFlightData.aauz;
                altBaro = e.newFlightData.AltCalReading / 100;
            }
            else
            {
                dedLines = null;
                invLines = null;

                pflLines = null;
                pflInvert = null;

                fuelFlow = 0.0f;
                fuelFlow2 = 0.0f;

                powerBits = 0;
                speeddBrakeValue = 0.0f;

                chaffCount = 0.0f;
                flareCount = 0.0f;

                oilPressure = 0.0f;
                nozzlePos = 0.0f;
                rpm = 0.0f;
                ftit = 0.0f;
            }
        }

        private void OnFlightDataLightsChanged(object sender, FlightDataChangedEventArgs e)
        {
            if (e.newFlightData != null)
            {
                lightBits = e.newFlightData.lightBits;
                lightBits2 = e.newFlightData.lightBits2;
                lightBits3 = e.newFlightData.lightBits3;
            }
            else
            {
                lightBits = 0;
                lightBits2 = 0;
                lightBits3 = 0;
            }
        }

        public void Dispose()
        {
        }
        #endregion

        #region COMPort
        [XmlIgnore]
        public string[] COMPortList => SerialPort.GetPortNames();

        public string COMPort
        {
            get { return comPort; }
            set
            {
                Error = null;
                if (string.IsNullOrEmpty(value))
                    return;

                if (!SerialPort.GetPortNames().Contains(value))
                {
                    Error = string.Format(Translations.Main.COMPortNotFoundError, value);
                    return;
                }

                comPort = value;
                RaisePropertyChanged("COMPort");

                if (serialPort != null)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();

                    serialPort.PortName = comPort;
                }
                else
                {
                    serialPort = new SerialPort();
                    SetupCOMPort();
                    serialPort.PortName = comPort;
                }

                Error = null;
                try
                {
                    serialPort.Open();
                }
                catch (Exception ex)
                {
                    Error = ex.Message;
                }
            }
        }
        private string comPort = string.Empty;
        #endregion

        #region SetupCOMPort
        private void SetupCOMPort()
        {
            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;

            // Set the read/write timeouts
            serialPort.ReadTimeout = 1000;

            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;
            serialPort.DataReceived += SerialPort_DataReceived;
        }
        #endregion

        #region SerialPort_DataReceived
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            #region DataProcessingLogic
            int buffersize = serialPort.BytesToRead;

            if (buffersize == 0)
                return;

            char[] s = new char[buffersize];
            for (short i = 0; i < s.Length; i++)
            {
                s[i] = (char)serialPort.ReadByte();
            }

            byte[] ResponseByte = new byte[1];

            char mode; // define "mode" variable
            ushort LineNum;
            #region mode_logic
            if (s.Length == 1)
            {
                if (char.IsNumber(s[0]))
                {
                    try
                    {
                        mode = serialBuffer;
                        LineNum = ushort.Parse(s[0].ToString());
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    try
                    {
                        //Debug.Print("letter only");
                        mode = s[0]; // mode is the first character from the serial string
                        serialBuffer = mode;
                        LineNum = 255;
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            else
            {
                try
                {
                    mode = s[0]; // mode is the first character from the serial string
                    if (char.IsNumber(s[1]))
                    {
                        LineNum = ushort.Parse(s[1].ToString());
                        //Debug.Print("two bytes - with number");
                    }
                    else
                    {
                        //Debug.Print("two bytes - no number");
                        LineNum = 255;
                    }
                }
                catch
                {
                    return;
                }
            }
            //Debug.Print("buff: " + new string (s));
            //Debug.Print("Mode: " + mode);
            //Debug.Print("Line: " + LineNum);
            #endregion

            switch (mode) // Get the party started.
            {
                #region DoWork
                case 'R': // Recive RDY call from arduino
                          //Debug.Print("buff: " + buffersize.ToString());
                    serialPort.DiscardInBuffer();
                    SendLine("G", 1); // if caught sent back GO command
                    serialPort.DiscardOutBuffer();
                    //MessageBox.Show("R");
                    break; // exit the interrupt
                case 'U': // Recive UPDATE commant from Arduino - This is not used, but retained for bawards compatibility
                    SendLine("k", 2); // if caught sent back GO command
                    break; // exit the interrupt                 
                case 'D': //Revice DED request from Arduino - requers reciving D and Number of line
                    #region DED_legacy
                    if (DED)
                    {
                        if (powerOn && LineNum >= 0 && LineNum < 5)
                        {
                            if (dedLines != null)
                                SendLine(NormalizeLine(dedLines[LineNum], invLines[LineNum]).ToString().PadRight(25, ' '), 25);
                            else
                                SendLine(" ".PadRight(25, ' '), 25);
                        }
                    }
                    break;
                #endregion
                case 'd': //Revice DED request from Arduino - requers reciving D and Number of line
                    #region DED
                    if (DED)
                    {
                        serialBuffer = 'd';
                        if (LineNum == 255)
                            break;

                        if (powerOn)
                        {
                            if (dedLines != null)
                                SendBytes(NormalizeLine(dedLines[LineNum], invLines[LineNum]), 24);
                            else
                                SendLine(" ".PadRight(24, ' '), 24);
                        }
                        else
                        {
                            if (LineNum == 2)
                                SendLine("FALCON NOT READY...".PadRight(24, ' '), 24);
                            else
                                SendLine(" ".PadRight(24, ' '), 24);
                        }
                    }
                    break;
                #endregion
                case 'P':
                    #region PFL_Legacy
                    if (PFL)
                    {
                        if (powerOn && LineNum >= 0 && LineNum < 5)
                        {
                            if (pflLines != null)
                                SendLine(NormalizeLine(pflLines[LineNum], pflInvert[LineNum]).ToString().PadRight(25, ' '), 25);
                            else
                                SendLine(" ".PadRight(25, ' '), 25);
                        }
                    }
                    break;
                #endregion
                case 'p':
                    #region PFL
                    if (PFL)
                    {
                        serialBuffer = 'p';
                        if (LineNum == 255)
                            break;

                        if (powerOn)
                        {
                            if (pflLines != null)
                                SendBytes(NormalizeLine(pflLines[LineNum], pflInvert[LineNum]), 24);
                            else
                                SendLine(" ".PadRight(24, ' '), 24);
                        }
                        else
                        {
                            if (LineNum == 2)
                                SendLine("FALCON NOT READY...".PadRight(24, ' '), 24);
                            else
                                SendLine(" ".PadRight(24, ' '), 24);
                        }
                    }
                    break;
                #endregion
                case 'M':
                    #region CMDS
                    if (CMDS)
                    {
                        serialBuffer = 'M';
                        if (LineNum == 255)
                            break;

                        if (powerOn)
                            SendLine(CMDSMakeLine((short)LineNum).PadRight(24, ' '), 24);
                        else
                            SendLine(" ".PadRight(24, ' '), 24);
                    }
                    break;
                #endregion
                case 'K':
                    #region ALT1000
                    if (Altimeter)
                    {
                        if (powerOn)
                            SendLine(Alt1000Convert((altitude)).PadLeft(5, '0'), 5);
                        else
                            SendLine("0".PadRight(5, '0'), 5);
                    }
                    break;
                #endregion
                case 'L':
                    #region ALT_BARO
                    if (Altimeter)
                    {
                        if (powerOn)
                            SendBytes(BitConverter.GetBytes(altBaro), 4);
                        else
                            SendBytes(BitConverter.GetBytes(uint.MinValue), 4);
                    }
                    break;
                #endregion
                case 'F':
                    #region FuelFlow
                    if (FFI)
                    {
                        if (powerOn)
                            SendLine(FuelFlowConvert((fuelFlow + fuelFlow2)).PadLeft(5, '0'), 5);
                        else
                            SendLine("0".PadRight(5, '0'), 5);
                    }
                    break;
                #endregion
                case 'A':
                    #region Indexers
                    if (Indexers)
                    {
                        blankByte[0] = (byte)0;
                        if (powerOn)
                            SendBytes(MakeAoaLight(), 1);
                        else
                            SendBytes(blankByte, 1);
                    }
                    break;
                #endregion
                case 'C':
                    #region CautionPanel
                    if (CautionPanel)
                    {
                        blankByte[0] = (byte)0;
                        if (powerOn)
                            SendBytes(MakeCautionPanel(cpVersion), 5); // types are "old" and "new", default is "new"                          
                        else
                        { 
                            SendBytes(BitConverter.GetBytes(uint.MinValue), 4);
                            SendBytes(blankByte, 1);
                        }
                    }
                    break;
                #endregion
                case 'G':
                    #region glareshield
                    if (GlareShield)
                    {
                        blankByte[0] = (byte)0;
                        if (powerOn)
                            SendBytes(MakeGlareShield(), 2);
                        else
                        {
                            // send two blank bytes
                            SendBytes(BitConverter.GetBytes(uint.MinValue), 2);
                        }
                    }
                    break;
                #endregion
                case 'T':
                    #region TWP
                    SendBytes(blankByte, 1);
                    break;
                #endregion
                case 'E':
                    #region Engine
                    if (Engine)
                    {
                        if (powerOn)
                        {
                            // senging out engine data by guage - top to bottom
                            SendBytes(BitConverter.GetBytes(oilPressure), 1);
                            SendBytes(BitConverter.GetBytes(nozzlePos), 1);
                            SendBytes(BitConverter.GetBytes(rpm), 1);
                            SendBytes(BitConverter.GetBytes(ftit), 2);
                        }
                        else
                            SendBytes(BitConverter.GetBytes(uint.MinValue), 5);
                    }
                    break;
                #endregion
                case 'S':
                    #region SpeedBrakes
                    if (SpeedBrakes)
                    {
                        if (powerOn)
                            SendBytes(MakeSpeedBrakes(), 1);
                        else
                            SendBytes(BitConverter.GetBytes('1'), 1); // send inop
                    }
                    break;
                    #endregion

                #endregion
            }
            #endregion // DataProcessingLogic
        }
        #endregion

        #region SendLine
        private void SendLine(string sendThis, int length)
        {
            if (sendThis.Length < length)
            {
                length = sendThis.Length;
            }
            byte[] sendBytes = Encoding.GetEncoding(1252).GetBytes(sendThis);
            if (serialPort.IsOpen)
                serialPort.Write(sendBytes, 0, length);
        }
        #endregion

        #region SendBytes
        private void SendBytes(byte[] sendThis, int length)
        {
            if (serialPort.IsOpen)
                serialPort.Write(sendThis, 0, length);
        }
        #endregion

        #region DED
        public bool DED
        {
            get { return ded; }
            set
            {
                ded = value;
                RaisePropertyChanged("DED");
                RaisePropertyChanged("IsDEDChecked");

                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool ded = false;

        //[XmlIgnore]
        //public bool CanDEDBeChecked => (!Indexers && !PFL && !FFI && !CautionPanel && !SpeedBrakes && !CMDS && !GlareShield && !Engine);
        [XmlIgnore]
        public bool IsDEDChecked => DED;
        #endregion

        #region PFL
        public bool PFL
        {
            get { return pfl; }
            set
            {
                pfl = value;
                RaisePropertyChanged("PFL");
                RaisePropertyChanged("IsPFLChecked");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool pfl = false;

        //[XmlIgnore]
        //public bool CanPFLBeChecked => (!DED && !Indexers && !FFI && !CautionPanel && !SpeedBrakes && !CMDS && !GlareShield && !Engine);
        [XmlIgnore]
        public bool IsPFLChecked => PFL;
        #endregion

        #region Altimeter
        public bool Altimeter
        {
            get { return altimeter; }
            set
            {
                altimeter = value;
                RaisePropertyChanged("Altimeter");
            }
        }

        private bool altimeter = false;
        #endregion

        #region FFI
        public bool FFI
        {
            get { return ffi; }
            set
            {
                ffi = value;
                RaisePropertyChanged("FFI");
                RaisePropertyChanged("IsFFIChecked");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool ffi = false;

        [XmlIgnore]
        public bool IsFFIChecked => FFI;

        public bool FFI_PR_MINUTE { get; set; }

        //[XmlIgnore]
        //public bool CanFFIBeChecked => (!DED && !PFL && !Indexers && !CautionPanel && !SpeedBrakes && !CMDS && !GlareShield && !Engine);
        #endregion

        #region CautionPanel
        public bool CautionPanel
        {
            get { return cautionPanel; }
            set
            {
                cautionPanel = value;
                RaisePropertyChanged("CautionPanel");
                RaisePropertyChanged("IsCautionPanelChecked");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool cautionPanel = false;

        public string CPVersion
        {
            get { return cpVersion; }
            set
            {
                cpVersion = value;
                RaisePropertyChanged("CPVersion");
            }
        }
        private string cpVersion = "new";

        [XmlIgnore]
        public List<string> NewOldList { get; }

        public bool JshepCP
        {
            get { return jShepCP; }
            set
            {
                jShepCP = value;
                RaisePropertyChanged("JshepCP");
            }
        }
        private bool jShepCP = false;

        [XmlIgnore]
        public bool IsCautionPanelChecked => (CautionPanel);
        //[XmlIgnore]
        //public bool CanCautionPanelBeChecked => (!DED && !PFL && !FFI && !Indexers && !SpeedBrakes && !CMDS && !GlareShield && !Engine);
        #endregion

        #region Indexers
        public bool Indexers
        {
            get { return indexers; }
            set
            {
                indexers = value;
                RaisePropertyChanged("Indexers");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool indexers = false;

        //[XmlIgnore]
        //public bool CanIndexersBeChecked => (!DED && !PFL && !FFI && !CautionPanel && !SpeedBrakes && !CMDS && !GlareShield && !Engine);
        #endregion

        #region SpeedBrakes
        public bool SpeedBrakes
        {
            get { return speedBrakes; }
            set
            {
                speedBrakes = value;
                RaisePropertyChanged("SpeedBrakes");
                RaisePropertyChanged("IsSpeedBrakesChecked");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool speedBrakes = false;

        //[XmlIgnore]
        //public bool CanSpeedBrakesBeChecked => (!DED && !PFL && !FFI && !CautionPanel && !Indexers && !CMDS && !GlareShield && !Engine);
        [XmlIgnore]
        public bool IsSpeedBrakesChecked => SpeedBrakes;
        #endregion

        #region CMDS
        public bool CMDS
        {
            get { return cmds; }
            set
            {
                cmds = value;
                RaisePropertyChanged("CMDS");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool cmds = false;

        //[XmlIgnore]
        //public bool CanCMDSBeChecked => (!DED && !PFL && !FFI && !CautionPanel && !Indexers && !SpeedBrakes && !GlareShield && !Engine);
        #endregion

        #region GlareShield
        public bool GlareShield
        {
            get { return glareShield; }
            set
            {
                glareShield = value;
                RaisePropertyChanged("GlareShield");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanEngineBeChecked");
            }
        }

        private bool glareShield = false;

        //[XmlIgnore]
        //public bool CanGlareShieldBeChecked => (!DED && !PFL && !FFI && !CautionPanel && !Indexers && !SpeedBrakes && !CMDS && !Engine);
        #endregion

        #region Engine
        public bool Engine
        {
            get { return engine; }
            set
            {
                engine = value;
                RaisePropertyChanged("Engine");

                //RaisePropertyChanged("CanDEDBeChecked");
                //RaisePropertyChanged("CanPFLBeChecked");
                //RaisePropertyChanged("CanFFIBeChecked");
                //RaisePropertyChanged("CanCautionPanelBeChecked");
                //RaisePropertyChanged("CanIndexersBeChecked");
                //RaisePropertyChanged("CanSpeedBrakesBeChecked");
                //RaisePropertyChanged("CanCMDSBeChecked");
                //RaisePropertyChanged("CanGlareShieldBeChecked");
            }
        }

        private bool engine = false;

        //[XmlIgnore]
        //public bool CanEngineBeChecked => (!DED && !PFL && !FFI && !CautionPanel && !Indexers && !SpeedBrakes && !CMDS && !GlareShield);
        #endregion


        public bool BMS432 { get; set; }

        private byte[] MakeAoaLight()
        /*
         * This function yanks out the Indexer bits and returns a byte with the 6 bits (and 2 spacers)
         */
        {
            BitArray mapping = new BitArray(8, false);

            mapping[7] = CheckLight(LightBits.RefuelDSC); // //RefuelDSC
            mapping[6] = CheckLight(LightBits.RefuelAR); //RefuelAR
            mapping[5] = CheckLight(LightBits.RefuelRDY); //RefuelRDY
            mapping[4] = false; // blank
            mapping[3] = false; // blank
            mapping[2] = CheckLight(LightBits.AOABelow); // AOABelow          
            mapping[1] = CheckLight(LightBits.AOAOn); //AOAOn
            mapping[0] = CheckLight(LightBits.AOAAbove); //AOAAbove
            byte[] result = new byte[1];
            mapping.CopyTo(result, 0);
            return result;
        }

        private string Alt1000Convert(float alt)
        {
            return (Math.Round(Convert.ToDecimal(alt) % 1000, 0)).ToString();
        }

        private string FuelFlowConvert(float FuelFlow)
        {
            if (FFI_PR_MINUTE)
                return (Math.Round(Convert.ToDecimal(FuelFlow) / 6) * 10).ToString();

            return (Math.Round(Convert.ToDecimal(FuelFlow) / 10) * 10).ToString();
        }

        private byte[] MakeCautionPanel(string version = "new")
        /* 
         * this function takes one string argument "new" or "old" and returns an 5 byte array of light bits acording to the selected layout of the Caution panel
         */
        {
            BitArray mapping = new BitArray(32, false);
            byte[] result = new byte[4];

            if (!CheckLight(LightBits.AllLampBitsOn) && !CheckLight(LightBits2.AllLampBits2On)) //  check if all the lamp bits on LB1 are up. pretty much will only happen when you check lights.  
            { //if "false" we are not in lightcheck - run logic
                switch (version)
                {
                    case "new":
                        #region newCautionPanel
                        /// left row (bottom to top)
                        mapping[31] = CheckLight(LightBits2.AftFuelLow); // AFT FUEL LOW
                        mapping[30] = CheckLight(LightBits2.FwdFuelLow); // FWD FUEL LOW
                        mapping[29] = false; // ATF NOT ENGAGED
                        mapping[28] = CheckLight(LightBits.CONFIG); // STORES CONFIG
                        mapping[27] = CheckLight(LightBits3.cadc); // CADC
                        mapping[26] = CheckLight(LightBits2.PROBEHEAT); // PROBE HEAT
                        mapping[25] = CheckLight(LightBits3.Elec_Fault); // ELEC SYS
                        mapping[24] = CheckLight(LightBits.FLCS); // FLCS FAULT
                        /// mid left row (bottom to top)
                        mapping[23] = false; //blank
                        mapping[22] = CheckLight(LightBits2.BUC); //BUC
                        mapping[21] = false; // EEC
                        mapping[20] = CheckLight(LightBits.Overheat); // OVERHEAT
                        mapping[19] = false; // INLET ICING
                        mapping[18] = CheckLight(LightBits2.FUEL_OIL_HOT); // FUEL OIL HOT
                        mapping[17] = CheckLight(LightBits2.SEC); // SEC
                        mapping[16] = CheckLight(LightBits.EngineFault); // ENGINE FAULT
                        /// mid right row (bottom to top)
                        mapping[15] = false;  //blank
                        mapping[14] = false;  //blank
                        mapping[13] = false; //blank
                        mapping[12] = false; // nuclear
                        mapping[11] = CheckLight(LightBits.IFF); // IFF
                        mapping[10] = CheckLight(LightBits.RadarAlt); // Radar ALT
                        mapping[9] = CheckLight(LightBits.EQUIP_HOT); // EQUIP HOT
                        mapping[8] = CheckLight(LightBits.Avionics); // Avionics Fault
                        /// right row (bottom to top)
                        mapping[7] = false; //blank
                        mapping[6] = false; //blank
                        mapping[5] = CheckLight(LightBits.CabinPress); // Cabin Press
                        mapping[4] = CheckLight(LightBits2.OXY_LOW); // Oxy_Low
                        mapping[3] = CheckLight(LightBits.Hook); // hook
                        mapping[2] = CheckLight(LightBits2.ANTI_SKID); // anti-skid
                        mapping[1] = CheckLight(LightBits.NWSFail); // NWS fail
                        mapping[0] = CheckLight(LightBits2.SEAT_ARM); // Seat not armed
                        #endregion
                        break;
                    case "old":
                        #region oldCautionPanel
                        /// left row (bottom to top)
                        mapping[31] = CheckLight(LightBits2.SEC); //SEC
                        mapping[30] = CheckLight(LightBits.EngineFault); // ENGINE FAULT
                        mapping[29] = false; // INLET ICING
                        mapping[28] = CheckLight(LightBits3.Elec_Fault); // ELEC SYS
                        mapping[27] = CheckLight(LightBits3.cadc); // CADC
                        mapping[26] = CheckLight(LightBits3.Lef_Fault); // LE FLAPS
                        mapping[25] = false; // ADC
                        mapping[24] = CheckLight(LightBits.FltControlSys); // FLT CONT SYS
                        /// mid left row (bottom to top)
                        mapping[23] = false; //blank
                        mapping[22] = CheckLight(LightBits2.SEAT_ARM); // SEAT NOT ARMED
                        mapping[21] = CheckLight(LightBits2.FUEL_OIL_HOT); // FUEL OIL HOT
                        mapping[20] = CheckLight(LightBits2.BUC); // BUC
                        mapping[19] = false; // EEC
                        mapping[18] = CheckLight(LightBits.Overheat); // OVERHEAT
                        mapping[17] = CheckLight(LightBits2.AftFuelLow);// AFT FUEL LOW
                        mapping[16] = CheckLight(LightBits2.FwdFuelLow); // FWD FUEL LOW
                        /// mid right row (bottom to top)
                        mapping[15] = false; //blank
                        mapping[14] = CheckLight(LightBits.CONFIG); ; // STORES CONFIG
                        mapping[15] = CheckLight(LightBits.ECM); // ECM
                        mapping[13] = CheckLight(LightBits.IFF); // IFF
                        mapping[11] = CheckLight(LightBits.EQUIP_HOT); // EQUIP HOT
                        mapping[10] = CheckLight(LightBits.RadarAlt); // RADAR ALT
                        mapping[9] = false; // ATF NOT ENGAGED
                        mapping[8] = CheckLight(LightBits.Avionics); // AVIONICS
                        /// right row (bottom to top)
                        mapping[7] = false; //blank
                        mapping[6] = CheckLight(LightBits2.PROBEHEAT); // PROBE HEAT
                        mapping[5] = false; // NUCLEAR
                        mapping[4] = CheckLight(LightBits2.OXY_LOW); // OXY_LOW
                        mapping[3] = CheckLight(LightBits.CabinPress); // CABIN PRESS
                        mapping[2] = CheckLight(LightBits.NWSFail); // NWS FAILT
                        mapping[1] = CheckLight(LightBits.Hook); // HOOK
                        mapping[0] = CheckLight(LightBits2.ANTI_SKID); // ANTI SKID
                        #endregion
                        break;
                }
                if (JshepCP)
                {
                    // Shep's CP is total non-sense as far as bit order goes.. put stuff in order for transmission
                    BitArray ShepCP = new BitArray(40, false);
                    ShepCP[0] = mapping[16];
                    ShepCP[1] = mapping[24];
                    ShepCP[2] = mapping[17];
                    ShepCP[3] = mapping[25];
                    ShepCP[4] = mapping[19];
                    ShepCP[5] = mapping[18];
                    ShepCP[6] = mapping[26];
                    ShepCP[7] = mapping[27];

                    ShepCP[8] = mapping[28];
                    ShepCP[9] = mapping[29];
                    ShepCP[10] = mapping[31];
                    ShepCP[11] = mapping[30];
                    ShepCP[12] = false;
                    ShepCP[13] = false;
                    ShepCP[14] = mapping[20];
                    ShepCP[15] = false;

                    ShepCP[16] = false;
                    ShepCP[17] = mapping[21];
                    ShepCP[18] = mapping[22];
                    ShepCP[19] = mapping[23];
                    ShepCP[20] = mapping[15];
                    ShepCP[21] = mapping[14];
                    ShepCP[22] = mapping[13];
                    ShepCP[23] = mapping[12];

                    ShepCP[24] = false;
                    ShepCP[25] = false;
                    ShepCP[26] = false;
                    ShepCP[27] = false;
                    ShepCP[28] = mapping[7];
                    ShepCP[29] = mapping[6];
                    ShepCP[30] = mapping[5];
                    ShepCP[31] = mapping[4];

                    ShepCP[32] = mapping[3];
                    ShepCP[33] = mapping[11];
                    ShepCP[34] = mapping[2];
                    ShepCP[35] = mapping[10];
                    ShepCP[36] = mapping[1];
                    ShepCP[37] = mapping[9];
                    ShepCP[38] = mapping[0];
                    ShepCP[39] = mapping[8];

                    ShepCP.CopyTo(result, 0);
                }
            }
            else
            {
                switch (version)
                {
                    case "new":
                        #region newCautionPanel
                        /// left row (bottom to top)
                        mapping[31] = true;
                        mapping[30] = true;
                        mapping[29] = true;
                        mapping[28] = true;
                        mapping[27] = true;
                        mapping[26] = true;
                        mapping[25] = true;
                        mapping[24] = true;
                        /// mid left row (bottom to top)
                        mapping[23] = false; //blank
                        mapping[22] = true;
                        mapping[21] = true;
                        mapping[20] = true;
                        mapping[19] = true;
                        mapping[18] = true;
                        mapping[17] = true;
                        mapping[16] = true;
                        /// mid right row (bottom to top)
                        mapping[15] = false;  //blank
                        mapping[14] = false;  //blank
                        mapping[13] = false; //blank
                        mapping[12] = true;
                        mapping[11] = true;
                        mapping[10] = true;
                        mapping[9] = true;
                        mapping[8] = true;
                        /// right row (bottom to top)
                        mapping[7] = false; //blank
                        mapping[6] = false; //blank
                        mapping[5] = true;
                        mapping[4] = true;
                        mapping[3] = true;
                        mapping[2] = true;
                        mapping[1] = true;
                        mapping[0] = true;
                        #endregion
                        break;
                    case "old":
                        #region oldCautionPanel
                        /// left row (bottom to top)
                        mapping[31] = true;
                        mapping[30] = true;
                        mapping[29] = true;
                        mapping[28] = true;
                        mapping[27] = true;
                        mapping[26] = true;
                        mapping[25] = true;
                        mapping[24] = true;
                        /// mid left row (bottom to top)
                        mapping[23] = false; //blank
                        mapping[22] = true;
                        mapping[21] = true;
                        mapping[20] = true;
                        mapping[19] = true;
                        mapping[18] = true;
                        mapping[17] = true;
                        mapping[16] = true;
                        /// mid right row (bottom to top)
                        mapping[15] = false; //blank
                        mapping[14] = true;
                        mapping[15] = true;
                        mapping[13] = true;
                        mapping[11] = true;
                        mapping[10] = true;
                        mapping[9] = true;
                        mapping[8] = true;
                        /// right row (bottom to top)
                        mapping[7] = false; //blank
                        mapping[6] = true;
                        mapping[5] = true;
                        mapping[4] = true;
                        mapping[3] = true;
                        mapping[2] = true;
                        mapping[1] = true;
                        mapping[0] = true;
                        #endregion
                        break;
                }
                if (JshepCP)
                {
                    // Shep's CP is total non-sense as far as bit order goes.. put stuff in order for transmission
                    BitArray ShepCP = new BitArray(40, false);
                    ShepCP[0] = mapping[16];
                    ShepCP[1] = mapping[24];
                    ShepCP[2] = mapping[17];
                    ShepCP[3] = mapping[25];
                    ShepCP[4] = mapping[19];
                    ShepCP[5] = mapping[18];
                    ShepCP[6] = mapping[26];
                    ShepCP[7] = mapping[27];

                    ShepCP[8] = mapping[28];
                    ShepCP[9] = mapping[29];
                    ShepCP[10] = mapping[31];
                    ShepCP[11] = mapping[30];
                    ShepCP[12] = false;
                    ShepCP[13] = false;
                    ShepCP[14] = mapping[20];
                    ShepCP[15] = false;

                    ShepCP[16] = false;
                    ShepCP[17] = mapping[21];
                    ShepCP[18] = mapping[22];
                    ShepCP[19] = mapping[23];
                    ShepCP[20] = mapping[15];
                    ShepCP[21] = mapping[14];
                    ShepCP[22] = mapping[13];
                    ShepCP[23] = mapping[12];

                    ShepCP[24] = false;
                    ShepCP[25] = false;
                    ShepCP[26] = false;
                    ShepCP[27] = false;
                    ShepCP[28] = mapping[7];
                    ShepCP[29] = mapping[6];
                    ShepCP[30] = mapping[5];
                    ShepCP[31] = mapping[4];

                    ShepCP[32] = mapping[3];
                    ShepCP[33] = mapping[11];
                    ShepCP[34] = mapping[2];
                    ShepCP[35] = mapping[10];
                    ShepCP[36] = mapping[1];
                    ShepCP[37] = mapping[9];
                    ShepCP[38] = mapping[0];
                    ShepCP[39] = mapping[8];

                    ShepCP.CopyTo(result, 0);
                }
            }

            return result;
        }

        private byte[] MakeGlareShield()
        /*
         * This function generates and returns a 2 byte array containing the glareshield lights
         */
        {
            BitArray mapping = new BitArray(16, false);
            byte[] result = new byte[mapping.Length];

            #region Generate_glareshield
            // Right side - top then bottom, from left to right
            mapping[0] = CheckLight(LightBits.ENG_FIRE); // Engine Fire
            mapping[1] = CheckLight(LightBits2.ENGINE); // Engine
            mapping[2] = CheckLight(LightBits.HYD); // HYD/OIL Press
            mapping[3] = CheckLight(LightBits.HYD); // HYD/OIL Press
            mapping[4] = CheckLight(LightBits.FLCS); // FLCS
            mapping[5] = CheckLight(LightBits3.DbuWarn); // DBU On
            mapping[6] = CheckLight(LightBits.T_L_CFG); //TO/LG Config
            mapping[7] = CheckLight(LightBits.T_L_CFG); //TO/LG Config
            mapping[8] = CheckLight(LightBits.CAN); // Canopy
            mapping[9] = CheckLight(LightBits.OXY_BROW); // OXY LOW (Brow)
            // Spacing
            mapping[10] = false; //spacer
            //left side - top then bottom, from left to right
            mapping[11] = CheckLight(LightBits.TF); // TF-FAIL
            mapping[12] = false; //blank
            mapping[13] = false;  //blank
            mapping[14] = false;  //blank
            // MC
            mapping[15] = CheckLight(LightBits.MasterCaution);  //Master Caution
            #endregion
            mapping.CopyTo(result, 0);
            return result;
        }

        private byte[] MakeSpeedBrakes()
        /* 
         * This fuction returns speedbreaks indicator status.
         * 0 - closed
         * 1 - INOP
         * 2 - Open
         */
        {
            byte[] result = new byte[1];
            if (!CheckLight(PowerBits.BusPowerEmergency) && !BMS432) //if emergency bus is down - speedbreaks indicator is INOP
                result[0] = 1;
            else if ((CheckLight(LightBits3.SpeedBrake)) && (speeddBrakeValue > 0.0)) // if speedbreaks are open
                result[0] = 2;
            else // if it's not INOP and not open - assume closed
                result[0] = 0;
            return result;
        }

        private string CMDSMakeLine(short line)
        {
            string CMDSLine = "";
            if (CheckLight(LightBits2.Go) || CheckLight(LightBits2.NoGo)) // if either GO or NOGO flags are on system is on, run logic
            {
                if (line == 0)
                { // If top line needs to be handled
                    if (CheckLight(LightBits2.NoGo))
                    { // NoGo bit (5 Chars)
                        CMDSLine += "NO GO";
                    }
                    else
                        CMDSLine += "".PadLeft(5, ' ');

                    CMDSLine += "".PadLeft(2, ' '); //space between windows (2 chars)
                    if (CheckLight(LightBits2.Go))
                    { // Go bit (2 Chars)
                        CMDSLine += "GO";
                    }
                    else
                        CMDSLine += " ".PadLeft(2, ' ');

                    CMDSLine += " ".PadLeft(4, ' '); //space between windows (4 chars)
                    if (CheckLight(LightBits2.Rdy))
                    { // Go bit (12 Chars)
                        CMDSLine += "DISPENSE RDY";
                    }
                    else
                        CMDSLine += "".PadLeft(12, ' ');
                }
                else if (line == 1)
                { // If bottom line is to be handled
                    if (CheckLight(LightBits2.Degr))
                    { // degr  bit (9 Chars)
                        CMDSLine += "AUTO DEGR";
                    }
                    else
                        CMDSLine += " ".PadLeft(9, ' ');

                    CMDSLine += " ".PadLeft(3, ' ');//space between windows (5 chars)
                    // Chaff low
                    if (CheckLight(LightBits2.ChaffLo))
                    { //  (3 Chars)
                        CMDSLine += "LO";
                    }
                    else
                        CMDSLine += " ".PadLeft(2, ' ');

                    // CHaff logic
                    if (chaffCount > 0) // if you have chaff
                        CMDSLine += chaffCount.ToString().PadLeft(3, ' '); //print chaff count
                    else if (chaffCount <= 0) // CM count of -1 = "out"
                        CMDSLine += "0".PadLeft(3, ' '); //print chaff count
                    else  // system is off or something
                        CMDSLine += " ".PadLeft(3, ' '); //send spaces

                    CMDSLine += "".PadLeft(1, ' '); //space between windows (1 chars)

                    if (CheckLight(LightBits2.FlareLo)) //Flare Low
                    { // (3 Chars)
                        CMDSLine += "LO";
                    }
                    else
                        CMDSLine += " ".PadLeft(2, ' ');

                    // Flare count logic
                    if (flareCount > 0) // if you have cm
                        CMDSLine += flareCount.ToString().PadLeft(3, ' '); //print chaff count
                    else if (flareCount <= 0) // CM count of -1 = "out"
                        CMDSLine += "0".PadLeft(3, ' '); //print chaff count
                    else // system is off or something
                        CMDSLine += "0".PadLeft(3, ' '); //send spaces
                }
                else
                {
                    CMDSLine = "err";
                }
            }
            else
            { // system is off - send blank line
                CMDSLine = "".PadRight(24, ' ');
            }
            return CMDSLine;
        }

        private bool CheckLight(LightBits datamask)
        {
            if ((lightBits & (Int32)datamask) == (Int32)datamask)
                return true;
            else
                return false;
        }

        private bool CheckLight(LightBits2 datamask)
        {
            if ((lightBits2 & (Int32)datamask) == (Int32)datamask)
                return true;
            else
                return false;
        }

        private bool CheckLight(LightBits3 datamask)
        {
            if ((lightBits3 & (Int32)datamask) == (Int32)datamask)
                return true;
            else
                return false;
        }

        private bool CheckLight(PowerBits datamask)
        {
            if ((powerBits & (Int32)datamask) == (Int32)datamask)
                return true;
            else
                return false;
        }

        #region RemoveDEDuinoCommand
        [XmlIgnore]
        public RelayCommand RemoveDEDuinoCommand { get; private set; }

        private void executeRemoveDEDuino(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveDEDuinoText),
                Translations.Main.RemoveDEDuinoCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.DEDuinoList.Remove(this);
            Dispose();
        }
        #endregion

        #region Error
        [XmlIgnore]
        public string Error
        {
            get { return error; }
            set
            {
                if (error == value)
                    return;
                error = value;
                RaisePropertyChanged("Error");
            }
        }
        private string error;
        #endregion

        #region owner
        public void setOwner(Configuration configuration)
        {
            owner = configuration;
        }

        private Configuration owner;
        #endregion

        private byte[] NormalizeLine(string Disp, string Inv)
        /*
         * This function takes two strings LINE and INV and mashes them into a string that conforms with the font on the Arduino Display
         * This works for DED and PFL
         */
        {
            char[] NormLine = new char[26]; // Create the result buffer
            for (short j = 0; j < Disp.Length; j++) // run the length of the Display string
            {
                if (Inv[j] == 2) // check if the corresponding position in the INV line is "lit" - indicated by char(2)
                { // if inverted
                    if (char.IsLetter(Disp[j])) // if char is letter (always uppercase)
                    {
                        NormLine[j] = char.ToLower((Disp[j])); // lowercase it - which is the inverted in the custom font
                    }
                    else if (Disp[j] == 1) // if it's the selection arrows
                    {
                        NormLine[j] = (char)192; // that is the selection arrow stuff
                    }
                    else if (Disp[j] == 2) // if it's a DED "*"
                    {
                        NormLine[j] = (char)170;
                    }
                    else if (Disp[j] == 3) // // if it's a DED "_"
                    {
                        NormLine[j] = (char)223;
                    }
                    else if (Disp[j] == '~') // Arrow down (PFD)
                    {
                        NormLine[j] = (char)252;
                    }
                    else if (Disp[j] == '^') // degree simbol (doesn't work with +128 from some reason so manualy do it
                    {
                        NormLine[j] = (char)222;
                    }
                    else // for everything else - just add 128 to the ASCII value (i.e numbers and so on)
                    {
                        NormLine[j] = (char)(Disp[j] + 128);
                    }
                }
                else // if it's non inverted
                {
                    if (Disp[j] == 1) // Selector double arrow
                    {
                        NormLine[j] = '@';
                    }
                    else if (Disp[j] == 2) // if it's a DED "*"
                    {
                        NormLine[j] = '*';
                    }
                    else if (Disp[j] == 3) // if it's a DED "_"
                    {
                        NormLine[j] = '_';
                    }
                    else if (Disp[j] == '~') // Arrow down (PFD)
                    {
                        NormLine[j] = '|';
                    }
                    else
                    {
                        NormLine[j] = Disp[j];
                    }
                }

            }

            if (BMS432)
            {
                return Encoding.GetEncoding(1252).GetBytes(NormLine, 1, 24);
            }
            else
            {
                return Encoding.GetEncoding(1252).GetBytes(NormLine, 0, 24);
            }
        }
    }
}

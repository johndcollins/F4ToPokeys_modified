using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Diagnostics;
using SimplifiedCommon.Win32;
using F4SharedMem;
using F4SharedMem.Headers;

namespace F4ToPokeys
{
    public class FalconConnector
    {
        #region Singleton
        public static FalconConnector Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new FalconConnector();
                return singleton;
            }
        }
        private static FalconConnector singleton;
        #endregion // Singleton

        #region Construction/Destruction

        public FalconConnector()
        {
            LightList = new List<FalconLight>();
            initLightList();

            GaugeList = new List<FalconGauge>();
            initGaugeList();

            timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = detectFalconTimerInterval;
        }

        #endregion Construction/Destruction

        #region Timer

        public void start()
        {
            timer.Start();
        }

        public void stop()
        {
            timer.Stop();
        }

        private void timerTick(object sender, EventArgs e)
        {
            if (reader == null)
            {
                if (GetFalconWindowHandle() != IntPtr.Zero)
                {
                    reader = new Reader();
                    if (!reader.IsFalconRunning)
                    {
                        reader.Dispose();
                        reader = null;
                    }
                    else
                    {
                        DebugUtils.Speak(string.Format("Falcon started"));
                        raiseFalconStarted();
                        timer.Interval = readFalconDataTimerInterval;
                    }
                }
            }
            else
            {
                if (GetFalconWindowHandle() == IntPtr.Zero)
                {
                    DebugUtils.Speak(string.Format("Falcon stopped"));
                    reader.Dispose();
                    reader = null;
                    raiseFalconStopped();
                    timer.Interval = detectFalconTimerInterval;
                }
            }

            FlightData newFlightData = null;
            if (reader != null)
                newFlightData = reader.GetCurrentData();

            if (newFlightData == null)
            {
                if (oldFlightData != null)
                {
                    raiseFlightDataChanged(oldFlightData, newFlightData);
                    raiseFlightDataLightsChanged(oldFlightData, newFlightData);
                }
            }
            else
            {
                if (oldFlightData == null)
                {
                    raiseFlightDataChanged(oldFlightData, newFlightData);
                    raiseFlightDataLightsChanged(oldFlightData, newFlightData);
                }
                else
                {
                    raiseFlightDataChanged(oldFlightData, newFlightData);

                    bool lightChanged = (oldFlightData.lightBits ^ newFlightData.lightBits) != 0;
                    lightChanged |= (oldFlightData.lightBits2 ^ newFlightData.lightBits2) != 0;
                    lightChanged |= (oldFlightData.lightBits3 ^ newFlightData.lightBits3) != 0;
                    lightChanged |= (oldFlightData.hsiBits ^ newFlightData.hsiBits) != 0;
                    lightChanged |= oldFlightData.speedBrake != newFlightData.speedBrake;
                    lightChanged |= oldFlightData.rpm != newFlightData.rpm;
                    lightChanged |= (oldFlightData.blinkBits ^ newFlightData.blinkBits) != 0;
                    lightChanged |= (oldFlightData.powerBits ^ newFlightData.powerBits) != 0;
                    lightChanged |= oldFlightData.tacanInfo != newFlightData.tacanInfo;
                    lightChanged |= oldFlightData.navMode != newFlightData.navMode;
                    lightChanged |= (oldFlightData.cmdsMode ^ newFlightData.cmdsMode) != 0;
                    lightChanged |= (oldFlightData.altBits ^ newFlightData.altBits) != 0;

                    if (lightChanged)
                        raiseFlightDataLightsChanged(oldFlightData, newFlightData);
                }
            }

            oldFlightData = newFlightData;
        }

        private DispatcherTimer timer;
        private readonly TimeSpan detectFalconTimerInterval = TimeSpan.FromSeconds(5);
        private Reader reader;
        private FlightData oldFlightData;

        #endregion // Timer

        #region ReadFalconDataTimerInterval

        private TimeSpan readFalconDataTimerInterval = TimeSpan.FromMilliseconds(100);

        public TimeSpan ReadFalconDataTimerInterval
        {
            get { return readFalconDataTimerInterval; }
            set
            {
                if (readFalconDataTimerInterval == value)
                    return;

                if (value.TotalMilliseconds < 0 || value.TotalMilliseconds > Int32.MaxValue)
                    throw new ArgumentOutOfRangeException("ReadFalconDataTimerInterval");

                readFalconDataTimerInterval = value;

                if (reader != null)
                    timer.Interval = readFalconDataTimerInterval;
            }
        }

        #endregion

        #region Events

        public event EventHandler FalconStarted;
        private void raiseFalconStarted()
        {
            if (FalconStarted != null)
                FalconStarted(this, EventArgs.Empty);
        }

        public event EventHandler FalconStopped;
        private void raiseFalconStopped()
        {
            if (FalconStopped != null)
                FalconStopped(this, EventArgs.Empty);
        }

        public event EventHandler<FlightDataChangedEventArgs> FlightDataChanged;
        private void raiseFlightDataChanged(FlightData oldFlightData, FlightData newFlightData)
        {
            if (FlightDataChanged != null)
                FlightDataChanged(this, new FlightDataChangedEventArgs(oldFlightData, newFlightData));
        }

        public event EventHandler<FlightDataChangedEventArgs> FlightDataLightsChanged;
        private void raiseFlightDataLightsChanged(FlightData oldFlightData, FlightData newFlightData)
        {
            if (FlightDataLightsChanged != null)
                FlightDataLightsChanged(this, new FlightDataChangedEventArgs(oldFlightData, newFlightData));
        }

        #endregion Events

        #region Static Falcon functions

        public static IntPtr GetFalconWindowHandle()
        {
            IntPtr hWnd = NativeMethods.FindWindow("FalconDisplay", null);
            return hWnd;
        }

        #endregion // Static Falcon functions

        #region Panels
        const string aoaIndexer = "HUD: AOA Indexer";
        const string airRefuel = "HUD: Air Refuel";
        const string leftEyebrow = "L. WARNING LIGHTS";
        const string rightEyebrow = "R. WARNING LIGHTS";
        const string threatWarningPrime = "TWP";
        const string miscArmament = "MISC";
        const string landingGear = "LANDING GEAR";
        const string centerConsole = "CENTER CONSOLE";
        const string threatWarningAuxiliary = "TWA";
        const string chaffFlare = "CMDS";
        const string cautionlightPanel = "CAUTION";
        const string enginePanel = "ENG & JET START";
        const string epuPanel = "EPU";
        const string elecPanel = "ELEC";
        const string avtrPanel = "AVTR";
        const string flightControlPanel = "FLT CONTROL";
        const string testPanel = "TEST";
        const string auxCommPanel = "AUXCOMM";     // Beau
        const string others = "Others";

        //
        // Eric's Enhancements
        //
        const string leftaux = "L. AUX CONSOLE";
        const string powerdistribution = "POWER DISTRIBUTION";
        const string realmagswitches = "REAL MAGNETIC SWITCHES";
        #endregion

        #region LightList

        public List<FalconLight> LightList { get; private set; }

        private void initLightList()
        {
            addToLightList(new FalconLightBit1(leftEyebrow, "MASTER CAUTION", LightBits.MasterCaution));
            addToLightList(new FalconLightBit1(leftEyebrow, "TF-FAIL", LightBits.TF));
            addToLightList(new FalconLightBit1(rightEyebrow, "OXY LOW (R. Warning)", LightBits.OXY_BROW));
            addToLightList(new FalconLightBit1(cautionlightPanel, "EQUIP HOT", LightBits.EQUIP_HOT));
            addToLightList(new FalconLightBit1(rightEyebrow, "ENG FIRE", LightBits.ENG_FIRE));
            addToLightList(new FalconLightBit1(cautionlightPanel, "STORES CONFIG", LightBits.CONFIG));
            addToLightList(new FalconLightBit1(rightEyebrow, "HYD/OIL PRESS", LightBits.HYD));
            addToLightList(new FalconLightBit1(testPanel, "FLCS TEST", LightBits.Flcs_ABCD));
            addToLightList(new FalconLightBit1(rightEyebrow, "FLCS/DUAL", LightBits.FLCS));
            addToLightList(new FalconLightBit1(rightEyebrow, "CANOPY", LightBits.CAN));
            addToLightList(new FalconLightBit1(rightEyebrow, "TO/LDG CONFIG", LightBits.T_L_CFG));
            addToLightList(new FalconLightBit1(aoaIndexer, "AOA Above", LightBits.AOAAbove));
            addToLightList(new FalconLightBit1(aoaIndexer, "AOA On", LightBits.AOAOn));
            addToLightList(new FalconLightBit1(aoaIndexer, "AOA Below", LightBits.AOABelow));
            addToLightList(new FalconLightBit1(airRefuel, "RDY", LightBits.RefuelRDY));
            addToLightList(new FalconLightBit1(airRefuel, "AR/NWS", LightBits.RefuelAR));
            addToLightList(new FalconLightBit1(airRefuel, "DISC", LightBits.RefuelDSC));
            addToLightList(new FalconLightBit1(cautionlightPanel, "FLCS FAULT", LightBits.FltControlSys));
            addToLightList(new FalconLightBit1(others, "LE FLAPS", LightBits.LEFlaps));
            addToLightList(new FalconLightBit1(cautionlightPanel, "ENGINE FAULT", LightBits.EngineFault));
            addToLightList(new FalconLightBit1(cautionlightPanel, "OVERHEAT", LightBits.Overheat));
            addToLightList(new FalconLightBit1(others, "FUEL LOW", LightBits.FuelLow));
            addToLightList(new FalconLightBit1(cautionlightPanel, "AVIONICS FAULT", LightBits.Avionics));
            addToLightList(new FalconLightBit1(cautionlightPanel, "RADAR ALT", LightBits.RadarAlt));
            addToLightList(new FalconLightBit1(cautionlightPanel, "IFF", LightBits.IFF));
            addToLightList(new FalconLightBit1(others, "ECM", LightBits.ECM));
            addToLightList(new FalconLightBit1(cautionlightPanel, "HOOK", LightBits.Hook));
            addToLightList(new FalconLightBit1(cautionlightPanel, "NWS FAIL", LightBits.NWSFail));
            addToLightList(new FalconLightBit1(cautionlightPanel, "CABIN PRESS", LightBits.CabinPress));
            addToLightList(new FalconLightBit1(miscArmament, "TFR STBY", LightBits.TFR_STBY));
            addToLightList(new FalconLightBit2(threatWarningPrime, "HANDOFF", LightBits2.HandOff));
            addToLightList(new FalconLightBit2(threatWarningPrime, "NAVAL", LightBits2.Naval));
            addToLightList(new FalconLightBit2(threatWarningPrime, "TGT SEP", LightBits2.TgtSep));
            addToLightList(new FalconLightBit2(chaffFlare, "GO", LightBits2.Go));
            addToLightList(new FalconLightBit2(chaffFlare, "NO GO", LightBits2.NoGo));
            addToLightList(new FalconLightBit2(chaffFlare, "AUTO DEGR", LightBits2.Degr));
            addToLightList(new FalconLightBit2(chaffFlare, "DISPENSE RDY", LightBits2.Rdy));
            addToLightList(new FalconLightBit2(chaffFlare, "BINGO CHAFF", LightBits2.ChaffLo));
            addToLightList(new FalconLightBit2(chaffFlare, "BINGO FLARE", LightBits2.FlareLo));

            //
            // New Blinking Lamps - Beau & Eric
            //
            addToLightList(new FalconBlinkingLamp(threatWarningPrime, "LAUNCH",
                flightData => (flightData.lightBits2 & (int)LightBits2.Launch) != 0, BlinkBits.Launch, 260));
            addToLightList(new FalconBlinkingLamp(threatWarningAuxiliary, "SEARCH",
                flightData => (flightData.lightBits2 & (int)LightBits2.AuxSrch) != 0, BlinkBits.AuxSrch, 250));
            addToLightList(new FalconBlinkingLamp(threatWarningPrime, "PRIORITY",
                flightData => (flightData.lightBits2 & (int)LightBits2.PriMode) != 0, BlinkBits.PriMode, 250));
            addToLightList(new FalconBlinkingLamp(threatWarningPrime, "UNKNOWN",
                flightData => (flightData.lightBits2 & (int)LightBits2.Unk) != 0, BlinkBits.Unk, 250));
            addToLightList(new FalconBlinkingLamp(centerConsole, "OUTER MARKER",
                flightData => (flightData.hsiBits & (int)HsiBits.OuterMarker) != 0, BlinkBits.OuterMarker, 500));
            addToLightList(new FalconBlinkingLamp(centerConsole, "MIDDLE MARKER",
                flightData => (flightData.hsiBits & (int)HsiBits.MiddleMarker) != 0, BlinkBits.MiddleMarker, 250));
            addToLightList(new FalconBlinkingLamp(cautionlightPanel, "PROBE HEAT",
                flightData => (flightData.lightBits2 & (int)LightBits2.PROBEHEAT) != 0, BlinkBits.PROBEHEAT, 100));

            addToLightList(new FalconTWPOpenLight(threatWarningPrime, "OPEN", flightData => (flightData.lightBits2 & (int)LightBits2.PriMode) != 0));
            addToLightList(new FalconLightBit2(threatWarningAuxiliary, "ACTIVITY", LightBits2.AuxAct));
            addToLightList(new FalconLightBit2(threatWarningAuxiliary, "LOW ALTITUDE", LightBits2.AuxLow));
            addToLightList(new FalconLightBit2(threatWarningAuxiliary, "SYSTEM POWER", LightBits2.AuxPwr));
            addToLightList(new FalconLightBit2(miscArmament, "ECM PWR", LightBits2.EcmPwr));
            addToLightList(new FalconLightBit2(miscArmament, "ECM FAIL", LightBits2.EcmFail));
            addToLightList(new FalconLightBit2(cautionlightPanel, "FWD FUEL LOW", LightBits2.FwdFuelLow));
            addToLightList(new FalconLightBit2(cautionlightPanel, "AFT FUEL LOW", LightBits2.AftFuelLow));
            addToLightList(new FalconLightBit2(epuPanel, "EPU ON", LightBits2.EPUOn));
            addToLightList(new FalconLightBit2(enginePanel, "JFS RUN", LightBits2.JFSOn));
            addToLightList(new FalconLightBit2(cautionlightPanel, "SEC", LightBits2.SEC));
            addToLightList(new FalconLightBit2(cautionlightPanel, "OXY LOW (Caution)", LightBits2.OXY_LOW));
            addToLightList(new FalconLightBit2(cautionlightPanel, "SEAT ARM", LightBits2.SEAT_ARM));
            addToLightList(new FalconLightBit2(cautionlightPanel, "BUC", LightBits2.BUC));
            addToLightList(new FalconLightBit2(cautionlightPanel, "FUEL OIL HOT", LightBits2.FUEL_OIL_HOT));
            addToLightList(new FalconLightBit2(cautionlightPanel, "ANTI SKID", LightBits2.ANTI_SKID));
            addToLightList(new FalconLightBit2(miscArmament, "TFR ENGAGED", LightBits2.TFR_ENGAGED));
            addToLightList(new FalconLightBit2(landingGear, "GEAR HANDLE", LightBits2.GEARHANDLE));
            addToLightList(new FalconLightBit2(rightEyebrow, "ENGINE", LightBits2.ENGINE));
            addToLightList(new FalconLightBit3(elecPanel, "FLCS PMG", LightBits3.FlcsPmg));
            addToLightList(new FalconLightBit3(elecPanel, "MAIN GEN", LightBits3.MainGen));
            addToLightList(new FalconLightBit3(elecPanel, "STBY GEN", LightBits3.StbyGen));
            addToLightList(new FalconLightBit3(elecPanel, "EPU GEN", LightBits3.EpuGen));
            addToLightList(new FalconLightBit3(elecPanel, "EPU PMG", LightBits3.EpuPmg));
            addToLightList(new FalconLightBit3(elecPanel, "TO FLCS", LightBits3.ToFlcs));
            addToLightList(new FalconLightBit3(elecPanel, "FLCS RLY", LightBits3.FlcsRly));
            addToLightList(new FalconLightBit3(elecPanel, "BAT FAIL", LightBits3.BatFail));
            addToLightList(new FalconLightBit3(epuPanel, "HYDRAZINE", LightBits3.Hydrazine));
            addToLightList(new FalconLightBit3(epuPanel, "AIR", LightBits3.Air));
            addToLightList(new FalconLightBit3(cautionlightPanel, "ELEC SYS", LightBits3.Elec_Fault));
            addToLightList(new FalconLightBit3(others, "LEF FAULT", LightBits3.Lef_Fault));

            //
            // Added missing LightBits - Beau
            //
            addToLightList(new FalconLightBit3(powerdistribution, "POWER OFF", LightBits3.Power_Off));
            addToLightList(new FalconLightBit3(landingGear, "SPEEDBRAKE", LightBits3.SpeedBrake));
            addToLightList(new FalconLightBit3(threatWarningPrime, "SYS TEST", LightBits3.SysTest));
            addToLightList(new FalconLightBit3(leftEyebrow, "MASTER CAUTION ANNOUNCED", LightBits3.MCAnnounced));
            addToLightList(new FalconLightBit3(others, "MAIN GEAR WOW", LightBits3.MLGWOW));
            addToLightList(new FalconLightBit3(others, "NOSE GEAR WOW", LightBits3.NLGWOW));
            addToLightList(new FalconLightBit3(others, "ATF NOT ENGAGED", LightBits3.ATF_Not_Engaged));
            addToLightList(new FalconHsiBits(centerConsole, "HSI TO FLAG", HsiBits.ToTrue));
            addToLightList(new FalconHsiBits(centerConsole, "HSI FROM FLAG", HsiBits.FromTrue));
            addToLightList(new FalconHsiBits(centerConsole, "HSI ILS WARN FLAG", HsiBits.IlsWarning));
            addToLightList(new FalconHsiBits(centerConsole, "HSI CRS WARN FLAG", HsiBits.CourseWarning));
            addToLightList(new FalconHsiBits(centerConsole, "HSI OFF FLAG", HsiBits.HSI_OFF));
            addToLightList(new FalconHsiBits(centerConsole, "ADI OFF FLAG", HsiBits.ADI_OFF));
            addToLightList(new FalconHsiBits(centerConsole, "ADI AUX FLAG", HsiBits.ADI_AUX));
            addToLightList(new FalconHsiBits(centerConsole, "ADI GS FLAG", HsiBits.ADI_GS));
            addToLightList(new FalconHsiBits(centerConsole, "ADI LOC FLAG", HsiBits.ADI_LOC));
            addToLightList(new FalconHsiBits(centerConsole, "BUP ADI OFF FLAG", HsiBits.BUP_ADI_OFF));
            addToLightList(new FalconHsiBits(centerConsole, "VVI OFF FLAG", HsiBits.VVI));
            addToLightList(new FalconHsiBits(centerConsole, "AOA OFF FLAG", HsiBits.AOA));
            addToLightList(new FalconHsiBits(others, "IN THE PIT", HsiBits.Flying));
            addToLightList(new FalconTacanBits(auxCommPanel, "AUX TACAN BAND X", TacanBits.band, (int)TacanSources.AUX));
            addToLightList(new FalconTacanBits(auxCommPanel, "AUX TACAN A-A", TacanBits.mode, (int)TacanSources.AUX));
            addToLightList(new FalconTacanBits(auxCommPanel, "UFC TACAN BAND X", TacanBits.band, (int)TacanSources.UFC));
            addToLightList(new FalconTacanBits(auxCommPanel, "UFC TACAN A-A", TacanBits.mode, (int)TacanSources.UFC));
            addToLightList(new FalconDataBits(centerConsole, "ALTIMETER in Hg", flightdata => (flightdata.altBits & (int)AltBits.CalType) == 1));
            addToLightList(new FalconDataBits(centerConsole, "ALT PNEU FLAG", flightdata => (flightdata.altBits & (int)AltBits.PneuFlag) == 1));
            addToLightList(new FalconDataBits(centerConsole, "NAV MODE TACAN", flightdata => (flightdata.navMode == (byte)NavModes.TACAN)));
            addToLightList(new FalconDataBits(centerConsole, "NAV MODE NAV", flightdata => (flightdata.navMode == (byte)NavModes.NAV)));
            addToLightList(new FalconDataBits(centerConsole, "NAV MODE ILS-TACAN", flightdata => (flightdata.navMode == (byte)NavModes.ILS_TACAN)));
            addToLightList(new FalconDataBits(centerConsole, "NAV MODE ILS-NAV", flightdata => (flightdata.navMode == (byte)NavModes.ILS_NAV)));
            addToLightList(new FalconDataBits(chaffFlare, "CMDS MODE OFF ", flightdata => (flightdata.cmdsMode == (int)CmdsModes.CmdsOFF)));
            addToLightList(new FalconDataBits(chaffFlare, "CMDS MODE STBY", flightdata => (flightdata.cmdsMode == (int)CmdsModes.CmdsSTBY)));
            addToLightList(new FalconDataBits(chaffFlare, "CMDS MODE MAN", flightdata => (flightdata.cmdsMode == (int)CmdsModes.CmdsMAN)));
            addToLightList(new FalconDataBits(chaffFlare, "CMDS MODE AUTO", flightdata => (flightdata.cmdsMode == (int)CmdsModes.CmdsAUTO)));
            addToLightList(new FalconDataBits(chaffFlare, "CMDS MODE BYP", flightdata => (flightdata.cmdsMode == (int)CmdsModes.CmdsBYP)));
            addToLightList(new FalconDataBits(chaffFlare, "CMDS MODE SEMI", flightdata => (flightdata.cmdsMode == (int)CmdsModes.CmdsSEMI)));
            //
            // End new Lightbits
            //

            addToLightList(new FalconLightBit3(flightControlPanel, "FLCS BIT RUN", LightBits3.FlcsBitRun));
            addToLightList(new FalconLightBit3(flightControlPanel, "FLCS BIT FAIL", LightBits3.FlcsBitFail));
            addToLightList(new FalconLightBit3(rightEyebrow, "DBU ON", LightBits3.DbuWarn));
            addToLightList(new FalconLightBit3(cautionlightPanel, "C ADC", LightBits3.cadc));

            //
            // Real Mag Switch Enhancements - Eric
            //
            addToLightList(new FalconLightBit1(realmagswitches, "AUTOPILOT REAL MAG", LightBits.AutoPilotOn));
            addToLightList(new FalconLightBit3(realmagswitches, "PARKING BRAKE REAL MAG", LightBits3.ParkBrakeOn));
            addToLightList(new FalconLightBit3(realmagswitches, "FLCS BIT REAL MAG", LightBits3.FlcsBitRun));
            addToLightList(new FalconPowerBits(realmagswitches, "JFS REAL MAG", PowerBits.JetFuelStarter));


            addToLightList(new FalconHsiBits(avtrPanel, "AVTR", HsiBits.AVTR));

            //
            // Combined OM/MM Marker Beacon Lamp
            // - Removed because it doesn't support steady On state.  Use OM and MM Lamps wired in parallel to MRK Lamp.
            //
            //addToLightList(new FalconMarkerBeacon(centerConsole, "MARKER BEACON", 
            //    flightdata => ((flightdata.hsiBits & (int)HsiBits.OuterMarker) != 0) || (flightdata.hsiBits & (int)HsiBits.MiddleMarker) != 0));

            //
            // Power Bus Enhancements to control Power Buss Relays - Eric
            //
            addToLightList(new FalconPowerBits(powerdistribution, "BATTERY BUS", PowerBits.BusPowerBattery));
            addToLightList(new FalconPowerBits(powerdistribution, "EMERGENCY BUS", PowerBits.BusPowerEmergency));
            addToLightList(new FalconPowerBits(powerdistribution, "ESSENTIAL BUS", PowerBits.BusPowerEssential));
            addToLightList(new FalconPowerBits(powerdistribution, "NON-ESSENTIAL BUS", PowerBits.BusPowerNonEssential));
            addToLightList(new FalconPowerBits(powerdistribution, "MAIN GENERATOR", PowerBits.MainGenerator));
            addToLightList(new FalconPowerBits(powerdistribution, "STBY GENERATOR", PowerBits.StandbyGenerator));
            addToLightList(new FalconRPMOver65Percent(others, "RPM > 65%", flightdata => flightdata.rpm > 65.0F));


            addToLightList(new FalconLightNoseGearDown(landingGear, "NOSE GEAR DOWN"));
            addToLightList(new FalconLightLeftGearDown(landingGear, "LEFT GEAR DOWN"));
            addToLightList(new FalconLightRightGearDown(landingGear, "RIGHT GEAR DOWN"));
            addToLightList(new FalconLightSpeedBrake(landingGear, "SPEEDBRAKE > 0%", 0.0f / 3.0f));
            addToLightList(new FalconLightSpeedBrake(landingGear, "SPEEDBRAKE > 33%", 1.0f / 3.0f));
            addToLightList(new FalconLightSpeedBrake(landingGear, "SPEEDBRAKE > 66%", 2.0f / 3.0f));

            //
            // Support for Homebuilt Custom Mag Switches that need a pulse to reset them to the off position.  Michael
            //
            addToLightList(new FalconMagneticSwitchReset(flightControlPanel, "FLCS BIT DIY MAG SW RESET",
                flightData => (flightData.lightBits3 & (int)LightBits3.FlcsBitRun) == 0));
            addToLightList(new FalconMagneticSwitchReset(enginePanel, "JFS DIY MAG SW RESET",
                flightData => (flightData.lightBits2 & (int)LightBits2.JFSOn) == 0));
            addToLightList(new FalconMagneticSwitchReset(landingGear, "PARK BRAKE DIY MAG SW RESET",
                flightData => flightData.rpm > 87.0F));
            addToLightList(new FalconMagneticSwitchReset(miscArmament, "AUTOPILOT DIY MAG SW RESET",
                flightData => (flightData.lightBits & (int)LightBits.AutoPilotOn) == 0));

            //
            // Eric's ON-GROUND Enhancement 
            //
            addToLightList(new FalconGearHandleSol(landingGear, "ON-GROUND", flightData => (flightData.lightBits & (int)LightBits.ONGROUND) != 0x10));
            //
            // Eric's SpeedBrake Indicator Enhancement 
            // - Allows SpeedBrake Indicator to be tri-state; OPEN, CLOSED or Barber-Pole
            //   (Barber-Pole = !OPEN && !CLOSED)
            //
            addToLightList(new RealSpeedBrakeClosed(leftaux, "SPEEDBRAKE CLOSED", flightData => flightData.speedBrake == 0));
            addToLightList(new RealSpeedBrakeOpen(leftaux, "SPEEDBRAKE OPEN", flightData => flightData.speedBrake > 0));
        }

        private void addToLightList(FalconLight falconLight)
        {
            if (LightList.Find(item => item.Label == falconLight.Label) != null)
                throw new ArgumentException("F4ToPokeys.FalconConnector.LightList already contains an item labeled " + falconLight.Label);
            else
                LightList.Add(falconLight);
        }

        #endregion // LightList

        #region GaugeList

        public List<FalconGauge> GaugeList { get; private set; }

        private void initGaugeList()
        {
            addToGaugeList(new FalconGauge("AOA", flightData => flightData.alpha, -32.0F, 32.0F, 4, 0, 1));
            addToGaugeList(new FalconGauge("MACH", flightData => flightData.mach, 0.0F, 2.0F, 3, 0, 2));
            addToGaugeList(new FalconGauge("KIAS", flightData => flightData.kias, 0.0F, 1000.0F, 4, 0, 0));
            addToGaugeList(new FalconGauge("VVI", flightData => -flightData.zDot, -100.0F, 100.0F, 4, 0, 0));
            addToGaugeList(new FalconGauge("NOZ POS", flightData => flightData.nozzlePos, 0.0F, 1.0F, 3, 0, 2));
            addToGaugeList(new FalconGauge("NOZ POS 2", flightData => flightData.nozzlePos2, 0.0F, 1.0F, 3, 0, 2));
            addToGaugeList(new FalconGauge("RPM", flightData => flightData.rpm, 0.0F, 103.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("RPM 2", flightData => flightData.rpm2, 0.0F, 103.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("FTIT", flightData => flightData.ftit, 0.0F, 12.0F, 3, 0, 1));
            addToGaugeList(new FalconGauge("FTIT 2", flightData => flightData.ftit2, 0.0F, 12.0F, 3, 0, 1));
            addToGaugeList(new FalconGauge("SPEEDBRAKE", flightData => flightData.speedBrake, 0.0F, 1.0F, 3, 0, 2));
            addToGaugeList(new FalconGauge("EPU FUEL", flightData => flightData.epuFuel, 0.0F, 100.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("OIL PRESSURE", flightData => flightData.oilPressure, 0.0F, 100.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("OIL PRESSURE 2", flightData => flightData.oilPressure2, 0.0F, 100.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("CHAFF COUNT", flightData => flightData.ChaffCount >= 0.0F ? flightData.ChaffCount : 0.0F, 0.0F, 180.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("FLARE COUNT", flightData => flightData.FlareCount >= 0.0F ? flightData.FlareCount : 0.0F, 0.0F, 30.0F, 2, 0, 0));
            addToGaugeList(new FalconGauge("TRIM PITCH", flightData => flightData.TrimPitch, -0.5F, 0.5F, 4, 0, 2));
            addToGaugeList(new FalconGauge("TRIM ROLL", flightData => flightData.TrimRoll, -0.5F, 0.5F, 4, 0, 2));
            addToGaugeList(new FalconGauge("TRIM YAW", flightData => flightData.TrimYaw, -0.5F, 0.5F, 4, 0, 2));
            addToGaugeList(new FalconGauge("FUEL F/R", flightData => flightData.fwd, 0.0F, 40000.0F, 5, 0, 0));
            addToGaugeList(new FalconGauge("FUEL A/L", flightData => flightData.aft, 0.0F, 40000.0F, 5, 0, 0));
            addToGaugeList(new FalconGauge("FUEL TOTAL", flightData => flightData.total, 0.0F, 20000.0F, 5, 0, 0));
            addToGaugeList(new FalconGauge("FUEL FLOW", flightData => flightData.fuelFlow, 0.0F, 80000.0F, 5, 5, 0));
            addToGaugeList(new FalconGauge("ALT", flightData => -flightData.aauz, 0.0F, 60000.0F, 5, 0, 0));
            addToGaugeList(new FalconGauge("CURRENT HEADING", flightData => flightData.currentHeading, 0.0F, 360.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("HSI COURSE", flightData => flightData.desiredCourse, 0.0F, 360.0F, 3, 3, 0));
            addToGaugeList(new FalconGauge("HSI MILES", flightData => flightData.distanceToBeacon, 0.0F, 999.0F, 3, 3, 0));

            //
            // Added missing F4Shared Memory Gauges - Beau
            //
            addToGaugeList(new FalconGauge("ALT BARO", flightData => (float)flightData.AltCalReading / 100, 27.00F, 31.99F, 4, 2, 2));
            addToGaugeList(new FalconGauge("UHF CHANNEL", flightData => (float)flightData.BupUhfPreset, 0.0F, 20.0F, 2, 2, 0));
            addToGaugeList(new FalconGauge("UHF FREQ", flightData => (float)flightData.BupUhfFreq / 1000, 225.000F, 399.999F, 6, 3, 3));
            addToGaugeList(new FalconGauge("UFC TACAN CHANNEL", flightData => flightData.UFCTChan, 0.0F, 199.0F, 3, 3, 0));
            addToGaugeList(new FalconGauge("AUX TACAN CHANNEL", flightData => flightData.AUXTChan, 0.0F, 199.0F, 3, 3, 0));
            addToGaugeList(new FalconGauge("HYD PRESS A", flightData => flightData.hydPressureA, 0.0F, 9999.0F, 4, 0, 0));
            addToGaugeList(new FalconGauge("HYD PRESS B", flightData => flightData.hydPressureA, 0.0F, 9999.0F, 4, 0, 0));
            addToGaugeList(new FalconGauge("DESIRED HEADING", flightData => flightData.desiredHeading, 0.0F, 360.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("BEARING TO BEACON", flightData => flightData.bearingToBeacon, 0.0F, 360.0F, 3, 0, 0));
            addToGaugeList(new FalconGauge("CDI", flightData => flightData.courseDeviation, -10.0F, 10.0F, 4, 3, 1));
            addToGaugeList(new FalconGauge("CABIN ALT", flightData => flightData.cabinAlt, 0.0F, 60000.0F, 5, 0, 0));
        }

        private void addToGaugeList(FalconGauge falconGauge)
        {
            if (GaugeList.Find(item => item.Label == falconGauge.Label) != null)
                throw new ArgumentException("F4ToPokeys.FalconConnector.GaugeList already contains an item labeled " + falconGauge.Label);
            else
                GaugeList.Add(falconGauge);
        }

        #endregion // GaugeList
    }

    #region FlightDataChangedEventArgs

    public class FlightDataChangedEventArgs : EventArgs
    {
        public FlightDataChangedEventArgs(FlightData oldFlightData, FlightData newFlightData)
        {
            this.oldFlightData = oldFlightData;
            this.newFlightData = newFlightData;
        }

        public readonly FlightData oldFlightData;
        public readonly FlightData newFlightData;
    }

    #endregion // FlightDataChangedEventArgs
}

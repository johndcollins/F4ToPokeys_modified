using PoKeysDevice_DLL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class PoKeysStepperVID6066 : BindableObject, IDisposable
    {
        #region Contructor/Destructor
        private volatile bool _isHoming = false;
        private Stopwatch _homingTimeoutTimer = null;
        private int _homingTimeout = 2000;
        private byte _homingAxis = 0xFF;
        private DispatcherTimer _homingTimer = null;
        private readonly TimeSpan _homingTimerInterval = TimeSpan.FromMilliseconds(500);
        private bool _isFlying = false;

        public PoKeysStepperVID6066()
        {
            AddStepperCommand = new RelayCommand(executeAddStepper);

            FalconConnector.Singleton.FalconStarted += OnFalconFlyingStarted;
            FalconConnector.Singleton.FalconStopped += OnFalconFlyingStopped;
            //FalconConnector.Singleton.FalconFlyingStarted += OnFalconFlyingStarted;
            //FalconConnector.Singleton.FalconFlyingStopped += OnFalconFlyingStopped;
        }

        public void Dispose()
        {
            if (StepperList.Count > 0)
                DisablePulseEngine();

            foreach (PoKeysStepper stepper in StepperList)
                stepper.Dispose();
        }
        #endregion

        #region PowerOn Events
        private void OnFalconFlyingStopped(object sender, EventArgs e)
        {
            _isFlying = false;
        }

        private void OnFalconFlyingStarted(object sender, EventArgs e)
        {
            _isFlying = true;
            StartHomingAll();
        }
        #endregion

        #region InitPulseEngine
        private bool InitPulseEngine()
        {
            if ((owner == null) || (!owner.Connected))
                return false;

            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            // Check to see if Pulse Engine is Operating.  If not Reset Pulse Engine.
            owner.PokeysDevice.PEv2_GetStatus(ref peConfig);    // Check status
            if (peConfig.PulseEngineEnabled == 0)   // Is PulseEngineEnabled?
                ResetPulseEngine();  // Hard Reset Pulse Engine and Save Config

            //
            // Init Pulse Engine Code - Turn into Function to call at F4toPokeys Power up
            //
            //   - Activate and configure Pulse Engine
            //
            peConfig.PulseEngineEnabled = 8;  // Enable 8 axes
            peConfig.PulseGeneratorType = (byte)(0);  // Using external pulse generator without IO
            peConfig.ChargePumpEnabled = 0;   // Don't use charge pump output
                                               //config.EmergencySwitchPin = 0;  // No Emergency Switch Pin
                                               //                                // 0 - disabled i.e. none
                                               //                                // 1 - default input Pin 55
                                               //                                // 10 + 10-based pin ID
            peConfig.EmergencySwitchPolarity = 1; // 1 = Invert sensing
            owner.PokeysDevice.PEv2_SetupPulseEngine(ref peConfig);  // Now Setup the Pulse Engine with the above info

            owner.PokeysDevice.SaveConfiguration(); //Save the configuration

            return true;
        }
        #endregion

        #region DisablePulseEngine
        public bool DisablePulseEngine()
        {
            if ((owner == null) || (!owner.Connected))
                return false;

            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            peConfig.PulseEngineEnabled = 0;  // Disable
            peConfig.PulseGeneratorType = (byte)(0);  // Using external pulse generator without IO
            owner.PokeysDevice.PEv2_SetupPulseEngine(ref peConfig);  // Now Setup the Pulse Engine with the above info
            owner.PokeysDevice.PEv2_RebootEngine(ref peConfig); //Reboot the Pulse Engine
            owner.PokeysDevice.SaveConfiguration(); //Save the configuration
            //owner.PokeysDevice.RebootDevice();

            return true;
        }

        #endregion

        #region InitStepperAxis
        private bool InitStepperAxis(PoKeysStepper stepper)
        {
            if ((owner == null) || (!owner.Connected))
                return false;
            // 
            // Now configure and Set Axis Configurations for all steppers
            //
            SetAxisParameters(stepper);

            return true;
        }

        public void ResetPulseEngine()
        {
            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            peConfig.PulseEngineEnabled = 8;  // Enable 8 axes
            peConfig.PulseGeneratorType = (byte)(0);  // Using external pulse generator without IO
            owner.PokeysDevice.PEv2_SetupPulseEngine(ref peConfig);  // Now Setup the Pulse Engine with the above info
            owner.PokeysDevice.PEv2_RebootEngine(ref peConfig); //Reboot the Pulse Engine
            owner.PokeysDevice.SaveConfiguration(); //Save the configuration
        }
        #endregion

        #region SetAxisParameters
        private void SetAxisParameters(PoKeysStepper stepper)
        {
            if ((!stepper.StepperId.HasValue) || (stepper.StepperId.GetValueOrDefault() <= 0))
                return;

            int i = stepper.StepperId.GetValueOrDefault() - 1;

            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            peConfig.AxisEnabledInvertMask[i] = 1; // Use inverted axis enabled signal for PoStep drivers
            peConfig.AxesConfig[i] = (int)(ePEv2_AxisConfig.aoENABLED |
                ePEv2_AxisConfig.aoINTERNAL_PLANNER |
                ePEv2_AxisConfig.aoPOSITION_MODE);  // Use Position mode

            if (stepper.SoftLimitEnabled)
                peConfig.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoSOFT_LIMIT_ENABLED;

            if (stepper.Inverted)
                peConfig.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoINVERTED;

            if (stepper.HasHomeSwitch)
                peConfig.AxesSwitchConfig[i] |= (int)ePEv2_AxisSwitchOptions.aoSWITCH_HOME;

            if (stepper.HomeInverted)
                peConfig.AxesSwitchConfig[i] |= (int)ePEv2_AxisSwitchOptions.aoSWITCH_INVERT_HOME;

            if ((stepper.PinHomeSwitch > 0) && stepper.HasHomeSwitch)
                peConfig.PinHomeSwitch[i] = (byte)stepper.PinHomeSwitch;

            peConfig.HomingSpeed[i] = (byte)stepper.HomingSpeed;
            peConfig.HomingReturnSpeed[i] = (byte)stepper.HomingReturnSpeed;
            peConfig.MaxSpeed[i] = stepper.MaxSpeed;
            peConfig.MaxAcceleration[i] = stepper.MaxAcceleration;
            peConfig.MaxDecceleration[i] = stepper.MaxDecceleration;
            peConfig.SoftLimitMaximum[i] = stepper.MaxPoint.StepperValue;
            peConfig.SoftLimitMinimum[i] = stepper.MinPoint.StepperValue;
            peConfig.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set

            // Write (Set) above Axis Configuration
            //
            owner.PokeysDevice.PEv2_SetAxisConfiguration(ref peConfig); // Configure the axis
        }
        #endregion

        #region Homing
        private void StartHomingAll()
        {
            if ((owner == null) || (!owner.Connected))
                return;

            _homingTimer = new DispatcherTimer();
            _homingTimer.Tick += _homingTimer_Tick;
            _homingTimer.Interval = _homingTimerInterval;

            //
            // Home all 8 axis (stepper motors)
            //
            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            peConfig.HomingStartMaskSetup = 0xFF;  // Select all Axis for Homing
            _homingAxis = 0xFF;

            owner.PokeysDevice.PEv2_StartHoming(ref peConfig);  // Initiate the HOMING 

            _homingTimeoutTimer = new Stopwatch();
            _isHoming = true;
            _homingTimeoutTimer.Start();
            _homingTimer.Start();
        }

        internal void StartHomingSingle(PoKeysStepper stepper)
        {
            if (_isHoming)
                return;

            if (!stepper.StepperId.HasValue)
                return;

            if ((owner == null) || (!owner.Connected))
                return;

            _homingTimer = new DispatcherTimer();
            _homingTimer.Tick += _homingTimer_Tick;
            _homingTimer.Interval = _homingTimerInterval;

            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            peConfig.HomingStartMaskSetup = stepper.StepperId.Value;  // Select Axis for Homing
            _homingAxis = stepper.StepperId.Value;

            owner.PokeysDevice.PEv2_StartHoming(ref peConfig);  // Initiate the HOMING 

            _homingTimeoutTimer = new Stopwatch();
            _isHoming = true;
            _homingTimeoutTimer.Start();
            _homingTimer.Start();
        }


        private void _homingTimer_Tick(object sender, EventArgs e)
        {
            CheckHomingIsDone();
        }

        private void CheckHomingIsDone()
        {
            if (!owner.Connected)
                return;

            sPoKeysPEv2 peConfig = new sPoKeysPEv2();
            owner.PokeysDevice.PEv2_GetStatus(ref peConfig);

            if (peConfig.PulseEngineState != (byte)ePoKeysPEState.peHOMING)
            {
                if (peConfig.PulseEngineState == (byte)ePoKeysPEState.peHOME)
                {
                    EndHoming();
                    return;
                }
            }

            if (_homingTimeoutTimer.ElapsedMilliseconds >= _homingTimeout)
            {
                peConfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
                peConfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
                owner.PokeysDevice.PEv2_SetState(ref peConfig);
                EndHoming();
                return;
            }
        }

        private void EndHoming()
        {
            _homingTimer.Stop();
            _homingTimer = null;
            _homingTimeoutTimer.Stop();
            _homingTimeoutTimer = null;
            _isHoming = false;

            sPoKeysPEv2 peConfig = new sPoKeysPEv2();

            //
            // Stop the Pulse Engine so we can set each axis (stepper) postion to zero since we are HOMED
            //
            peConfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
            owner.PokeysDevice.PEv2_SetState(ref peConfig);

            //if (_homingAxis == 0xFF)
            //{
                //foreach (PoKeysStepper stepper in StepperList)
                //{
                //    if ((!stepper.StepperId.HasValue) || (stepper.StepperId.GetValueOrDefault() <= 0))
                //        continue;

                //    int i = stepper.StepperId.GetValueOrDefault() - 1;

                    //peConfig.param2 |= (byte)(1 << i); // Set param2 bit to indicate which stepper    
                    //peConfig.PositionSetup[i] = 0;     // Set axis (stepper) position to Zero
                    //owner.PokeysDevice.PEv2_SetPositions(ref peConfig);
                    //peConfig.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
                    //owner.PokeysDevice.PEv2_SetAxisConfiguration(ref peConfig); // Configure the axis
                //}
            //}

            peConfig.param2 |= _homingAxis; // Set param2 bit to indicate which stepper    
            for (int i = 0; i < peConfig.PositionSetup.values.Length; i++)
                peConfig.PositionSetup[i] = 0;     // Set axis (stepper) position to Zero
            owner.PokeysDevice.PEv2_SetPositions(ref peConfig);
            peConfig.param1 = _homingAxis; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
            owner.PokeysDevice.PEv2_SetAxisConfiguration(ref peConfig); // Configure the axis

            //
            // Set Pulse Engine back to RUNNING
            peConfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
            owner.PokeysDevice.PEv2_SetState(ref peConfig);

            //
            // Get Pulse Engine Statue and Check to confirm we are RUNNING
            //
            owner.PokeysDevice.PEv2_GetStatus(ref peConfig);
            if (peConfig.PulseEngineState != (byte)ePoKeysPEState.peRUNNING)
            {
                //
                // Not sure what we do in F4toPokeys if this happens
                //
                Error = "Pulse engine not in RUNNING state - check emergency switch. Quiting!";
                return;
            }
        }
        #endregion

        #region MoveStepper
        public bool MoveStepper(int stepper, int position)
        {
            if (_isHoming)
                return false;

            if ((owner == null) || (!owner.Connected))
                return false;

            try
            {
                sPoKeysPEv2 peConfig = new sPoKeysPEv2();
                peConfig.ReferencePositionSpeed[stepper] = position;
                owner.PokeysDevice.PEv2_Move(ref peConfig);
                owner.PokeysDevice.PEv2_GetStatus(ref peConfig);    // Check status

                return true;
            }
            catch { return false; }
        }
        #endregion

        //#region ReadStepper
        //public int ReadStepper(int stepper)
        //{
        //    if (_isHoming)
        //        return 0;

        //    if ((owner == null) || (!owner.Connected))
        //        return 0;

        //    try
        //    {
        //        owner.PokeysDevice.PEv2_GetStatus(ref peConfig);    // Check status
        //        return peConfig.CurrentPosition[stepper];
        //    }
        //    catch { return 0; }
        //}
        //#endregion

        #region StepperSlotsAvailable
        [XmlIgnore]
        public bool StepperSlotsAvailable
        {
            get { return StepperList.Count < 8; }
        }
        #endregion

        #region StepperList
        public ObservableCollection<PoKeysStepper> StepperList
        {
            get { return stepperList; }
            set
            {
                stepperList = value;
                RaisePropertyChanged("StepperList");
            }
        }
        private ObservableCollection<PoKeysStepper> stepperList = new ObservableCollection<PoKeysStepper>();
        #endregion

        #region AddStepperCommand
        [XmlIgnore]
        public RelayCommand AddStepperCommand { get; private set; }

        private void executeAddStepper(object o)
        {
            if (StepperList.Count == 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    Translations.Main.EnablePulseEngineWarning,
                    Translations.Main.EnablePulseEngineWarningCaption,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                InitPulseEngine();
            }

            PoKeysStepper poStepper = new PoKeysStepper();
            poStepper.setOwner(this);
            StepperList.Add(poStepper);
            InitStepperAxis(poStepper);
            RaisePropertyChanged("StepperSlotsAvailable");
        }

        public void StepperRemoved()
        {
            if (StepperList.Count == 0)
                DisablePulseEngine();

            RaisePropertyChanged("StepperSlotsAvailable");
        }
        #endregion

        #region owner
        public void setOwner(PoKeys pokeys)
        {
            owner = pokeys;

            foreach (PoKeysStepper poKeysStepper in StepperList)
                poKeysStepper.setOwner(this);

            if (StepperList.Count > 0)
                InitializeVID6066();
        }

        private PoKeys owner;
        #endregion

        #region InitializeVID6066
        public void InitializeVID6066()
        {
            if ((owner == null) || (!owner.Connected))
                return;

            InitPulseEngine();

            foreach (PoKeysStepper poKeysStepper in StepperList)
                InitStepperAxis(poKeysStepper);
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
        #endregion // Error
    }
}

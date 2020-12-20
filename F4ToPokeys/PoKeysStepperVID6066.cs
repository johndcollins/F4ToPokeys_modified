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
        sPoKeysPEv2 _PEconfig = new sPoKeysPEv2();
        private volatile bool _isHoming = false;
        private Stopwatch _homingTimeoutTimer = null;
        private int _homingTimeout = 2000;
        private int _homingAxis = -2;
        private int _homingStepper = 0;
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

            // Check to see if Pulse Engine is Operating.  If not Reset Pulse Engine.
            owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);    // Check status
            if (_PEconfig.PulseEngineEnabled == 0)   // Is PulseEngineEnabled?
                ResetPulseEngine();  // Hard Reset Pulse Engine and Save Config

            //
            // Init Pulse Engine Code - Turn into Function to call at F4toPokeys Power up
            //
            //   - Activate and configure Pulse Engine
            //
            _PEconfig.PulseEngineEnabled = 8;  // Enable 8 axes
            _PEconfig.PulseGeneratorType = (byte)(0);  // Using external pulse generator without IO
            _PEconfig.ChargePumpEnabled = 0;   // Don't use charge pump output
                                               //config.EmergencySwitchPin = 0;  // No Emergency Switch Pin
                                               //                                // 0 - disabled i.e. none
                                               //                                // 1 - default input Pin 55
                                               //                                // 10 + 10-based pin ID
            _PEconfig.EmergencySwitchPolarity = 1; // 1 = Invert sensing
            owner.PokeysDevice.PEv2_SetupPulseEngine(ref _PEconfig);  // Now Setup the Pulse Engine with the above info

            owner.PokeysDevice.SaveConfiguration(); //Save the configuration

            return true;
        }
        #endregion

        #region DisablePulseEngine
        public bool DisablePulseEngine()
        {
            if ((owner == null) || (!owner.Connected))
                return false;

            _PEconfig.PulseEngineEnabled = 0;  // Disable
            _PEconfig.PulseGeneratorType = (byte)(0);  // Using external pulse generator without IO
            owner.PokeysDevice.PEv2_SetupPulseEngine(ref _PEconfig);  // Now Setup the Pulse Engine with the above info
            owner.PokeysDevice.PEv2_RebootEngine(ref _PEconfig); //Reboot the Pulse Engine
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
            _PEconfig.PulseEngineEnabled = 8;  // Enable 8 axes
            _PEconfig.PulseGeneratorType = (byte)(0);  // Using external pulse generator without IO
            owner.PokeysDevice.PEv2_SetupPulseEngine(ref _PEconfig);  // Now Setup the Pulse Engine with the above info
            owner.PokeysDevice.PEv2_RebootEngine(ref _PEconfig); //Reboot the Pulse Engine
            owner.PokeysDevice.SaveConfiguration(); //Save the configuration
        }
        #endregion

        #region SetPinData
        public void SetPinData(PoKeysStepper stepper, byte pinID, byte pinFunction)
        {
            if ((owner == null) || (!owner.Connected))
                return;

            if (!owner.PokeysDevice.SetPinData((byte)(stepper.PinHomeSwitch - 1), pinFunction))
            {
                stepper.Error = string.Format(Translations.Main.DigitalInputErrorSetIOType, stepper.PinHomeSwitch);
                return;
            }

            owner.PokeysDevice.SaveConfiguration();

            pinFunction = 0;
            if (!owner.PokeysDevice.GetPinData((byte)(stepper.PinHomeSwitch - 1), ref pinFunction))
            {
                stepper.Error = Translations.Main.DigitalInputErrorGetIOType;
                return;
            }

            if (((pinFunction & 0x2) == 0) && ((pinFunction & 0x82) == 0))
            {
                stepper.Error = Translations.Main.DigitalInputErrorBadIOType;
                return;
            }

        }
        #endregion

        #region SetAxisParameters
        internal void SetAxisParameters(PoKeysStepper stepper)
        {
            if ((!stepper.StepperId.HasValue) || (stepper.StepperId.GetValueOrDefault() <= 0))
                return;

            stepper.Error = null;

            int i = stepper.StepperId.GetValueOrDefault() - 1;

            _PEconfig.AxisEnabledInvertMask[i] = 1; // Use inverted axis enabled signal for PoStep drivers
            _PEconfig.AxesConfig[i] = (int)(ePEv2_AxisConfig.aoENABLED |
                ePEv2_AxisConfig.aoINTERNAL_PLANNER |
                ePEv2_AxisConfig.aoPOSITION_MODE);  // Use Position mode

            if (stepper.SoftLimitEnabled)
                _PEconfig.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoSOFT_LIMIT_ENABLED;

            if (stepper.Inverted)
                _PEconfig.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoINVERTED;

            _PEconfig.AxesSwitchConfig[i] = 0;

            if (stepper.HasHomeSwitch)
                _PEconfig.AxesSwitchConfig[i] |= (int)ePEv2_AxisSwitchOptions.aoSWITCH_HOME;

            if (stepper.HomeInverted)
                _PEconfig.AxesSwitchConfig[i] |= (int)ePEv2_AxisSwitchOptions.aoSWITCH_INVERT_HOME;

            if ((stepper.PinHomeSwitch > 0) && stepper.HasHomeSwitch)
            {
                byte pinFunction = 0;
                if (stepper.HomeSwitchInverted)
                    pinFunction = 0x82;
                else
                    pinFunction = 0x2;
                SetPinData(stepper, (byte)stepper.PinHomeSwitch, pinFunction);
                _PEconfig.PinHomeSwitch[i] = (byte)(stepper.PinHomeSwitch);
            }

            _PEconfig.HomingSpeed[i] = (byte)stepper.HomingSpeed;
            _PEconfig.HomingReturnSpeed[i] = (byte)stepper.HomingReturnSpeed;
            _PEconfig.MaxSpeed[i] = (float)(stepper.MaxSpeed / 1000);
            _PEconfig.MaxAcceleration[i] = (float)(stepper.MaxAcceleration / 1000);
            _PEconfig.MaxDecceleration[i] = (float)(stepper.MaxDecceleration / 1000);
            _PEconfig.SoftLimitMaximum[i] = stepper.MaxPoint.StepperValue;
            _PEconfig.SoftLimitMinimum[i] = stepper.MinPoint.StepperValue;
            _PEconfig.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set

            // Write (Set) above Axis Configuration
            //
            owner.PokeysDevice.PEv2_SetAxisConfiguration(ref _PEconfig); // Configure the axis
        }
        #endregion

        #region Homing
        private void StartHomingAll()
        {
            if ((owner == null) || (!owner.Connected))
                return;

            foreach (PoKeysStepper stepper in StepperList)
            {
                stepper.Error = null;
                for (int j = 0; j < 3; j++)
                {
                    if (!MoveInitialHoming(stepper, 700))
                        break;
                }

                if (!string.IsNullOrEmpty(stepper.Error))
                    return;

                int i = stepper.StepperId.GetValueOrDefault() - 1;
                _PEconfig.MaxAcceleration[i] = (float)(stepper.HomingMaxAcceleration / 1000);
                _PEconfig.MaxDecceleration[i] = (float)(stepper.HomingMaxDecceleration / 1000);
                _PEconfig.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
                owner.PokeysDevice.PEv2_SetAxisConfiguration(ref _PEconfig); // Configure the axis
            }


            _homingTimer = new DispatcherTimer();
            _homingTimer.Tick += _homingTimer_Tick;
            _homingTimer.Interval = _homingTimerInterval;

            //
            // Home all 8 axis (stepper motors)
            //
            byte homingMask = 0;
            foreach (PoKeysStepper stepper in StepperList)
            {
                if ((!stepper.StepperId.HasValue) || (stepper.StepperId.GetValueOrDefault() <= 0))
                    continue;

                homingMask |= (byte)(1 << (stepper.StepperId.GetValueOrDefault() - 1));
            }

            _PEconfig.HomingStartMaskSetup = homingMask;  // Select all Axis for Homing
            _homingAxis = -1;
            _homingStepper = -1;

            owner.PokeysDevice.PEv2_StartHoming(ref _PEconfig);  // Initiate the HOMING 

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

            int i = stepper.StepperId.GetValueOrDefault() - 1;

            _PEconfig.MaxAcceleration[i] = (float)(stepper.HomingMaxAcceleration / 1000);
            _PEconfig.MaxDecceleration[i] = (float)(stepper.HomingMaxDecceleration / 1000);
            _PEconfig.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
            owner.PokeysDevice.PEv2_SetAxisConfiguration(ref _PEconfig); // Configure the axis

            stepper.Error = null;
            for (int j = 0; j < 3; j++)
            {
                if (!MoveInitialHoming(stepper, 700))
                    break;
            }

            if (!string.IsNullOrEmpty(stepper.Error))
                return;

            _homingTimer = new DispatcherTimer();
            _homingTimer.Tick += _homingTimer_Tick;
            _homingTimer.Interval = _homingTimerInterval;

            _PEconfig.HomingStartMaskSetup = (byte)(1 << i);  // Select Axis for Homing
            _homingAxis = i;
            _homingStepper = stepper.StepperId.GetValueOrDefault();

            owner.PokeysDevice.PEv2_StartHoming(ref _PEconfig);  // Initiate the HOMING 

            _homingTimeoutTimer = new Stopwatch();
            _isHoming = true;
            _homingTimeoutTimer.Start();
            _homingTimer.Start();
        }

        private bool MoveInitialHoming(PoKeysStepper stepper, int moveSteps)
        {
            if ((owner == null) || (!owner.Connected))
                return false;

            if (!stepper.ContinuousRotation)
                return false;

            int stepperId = stepper.StepperId.GetValueOrDefault() - 1;

            try
            {
                _PEconfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
                _PEconfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
                owner.PokeysDevice.PEv2_SetState(ref _PEconfig);

                //owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);    // Check status
                //int currentStepperPos = _PEconfig.CurrentPosition[stepperId];

                //int newStepperPosition = currentStepperPos - moveSteps;
                _PEconfig.ReferencePositionSpeed[stepperId] = moveSteps;
                if (!owner.PokeysDevice.PEv2_Move(ref _PEconfig))
                {
                    stepper.Error = string.Format(Translations.Main.StepperMoveError, stepper.StepperId);
                    return false;
                }

                Thread.Sleep(500);

                owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);    // Check status
                bool inputState = false;
                if (!owner.PokeysDevice.GetInput((byte)(_PEconfig.PinHomeSwitch.values[stepperId] - 1), ref inputState))
                {
                    stepper.Error = string.Format(Translations.Main.StepperGetInputError, _PEconfig.PinHomeSwitch.values[stepperId]);
                    return false;
                }

                return inputState;
            }
            catch { return false; }
        }

        private void _homingTimer_Tick(object sender, EventArgs e)
        {
            CheckHomingIsDone();
        }

        private void CheckHomingIsDone()
        {
            if (!owner.Connected)
                return;

            owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);

            if (_PEconfig.PulseEngineState != (byte)ePoKeysPEState.peHOMING)
            {
                if (_PEconfig.PulseEngineState == (byte)ePoKeysPEState.peHOME)
                {
                    EndHoming();
                    return;
                }
            }

            if (_homingTimeoutTimer.ElapsedMilliseconds >= _homingTimeout)
            {
                _PEconfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
                _PEconfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
                owner.PokeysDevice.PEv2_SetState(ref _PEconfig);
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

            owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);

            //
            // Stop the Pulse Engine so we can set each axis (stepper) postion to zero since we are HOMED
            //
            _PEconfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
            owner.PokeysDevice.PEv2_SetState(ref _PEconfig);

            if (_homingAxis == -1)
            {
                foreach (PoKeysStepper stepper in StepperList)
                {
                    if ((!stepper.StepperId.HasValue) || (stepper.StepperId.GetValueOrDefault() <= 0))
                        continue;

                    int i = stepper.StepperId.GetValueOrDefault() - 1;

                    _PEconfig.param2 = (byte)(1 << i); // Set param2 bit to indicate which stepper    
                    _PEconfig.PositionSetup[i] = 0;     // Set axis (stepper) position to Zero
                    owner.PokeysDevice.PEv2_SetPositions(ref _PEconfig);
                    _PEconfig.MaxAcceleration[i] = (float)(stepper.MaxAcceleration / 1000);
                    _PEconfig.MaxDecceleration[i] = (float)(stepper.MaxDecceleration / 1000);
                    _PEconfig.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
                    owner.PokeysDevice.PEv2_SetAxisConfiguration(ref _PEconfig); // Configure the axis
                }
            }
            else
            {
                _PEconfig.param2 = (byte)(1 << _homingAxis); // Set param2 bit to indicate which stepper 
                _PEconfig.PositionSetup[_homingAxis] = 0;     // Set axis (stepper) position to Zero

                owner.PokeysDevice.PEv2_SetPositions(ref _PEconfig);
                _PEconfig.MaxAcceleration[_homingAxis] = (float)(StepperList.First(s => s.StepperId == _homingStepper).MaxAcceleration / 1000);
                _PEconfig.MaxDecceleration[_homingAxis] = (float)(StepperList.First(s => s.StepperId == _homingStepper).MaxDecceleration / 1000);
                _PEconfig.param1 = (byte)_homingAxis; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
                owner.PokeysDevice.PEv2_SetAxisConfiguration(ref _PEconfig); // Configure the axis
            }

            _homingAxis = -2;
            _homingStepper = -1;

            //
            // Set Pulse Engine back to RUNNING
            _PEconfig.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
            owner.PokeysDevice.PEv2_SetState(ref _PEconfig);

            //
            // Get Pulse Engine Statue and Check to confirm we are RUNNING
            //
            owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);
            if (_PEconfig.PulseEngineState != (byte)ePoKeysPEState.peRUNNING)
            {
                //
                // Not sure what we do in F4toPokeys if this happens
                //
                Error = Translations.Main.StepperHomingError;
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
                _PEconfig.ReferencePositionSpeed[stepper] = position;
                if (!owner.PokeysDevice.PEv2_Move(ref _PEconfig))
                    return false;
                owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);    // Check status

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
        //        owner.PokeysDevice.PEv2_GetStatus(ref _PEconfig);    // Check status
        //        return _PEconfig.CurrentPosition[stepper];
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

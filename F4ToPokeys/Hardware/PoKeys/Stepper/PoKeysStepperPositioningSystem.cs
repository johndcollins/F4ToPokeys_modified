using PoKeysDevice_DLL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace F4ToPokeys
{
    public class PoKeysStepperPositioningSystem : IDisposable
    {
        #region Private Properties
        private Thread posThread = null;
        private bool ErrorFixed { get; set; } = false;
        private bool CheckNewState { get; set; } = false;

        private bool NewStateCommanded { get; set; } = false;
        private byte NewState { get; set; } = (byte)ePoKeysPEState.peSTOPPED;
        private PoKeysStepperPositioningState State { get; set; } = PoKeysStepperPositioningState.STOPPED;
        private PoKeysStepperPositioningState StateAfterMove { get; set; } = PoKeysStepperPositioningState.STOPPED;
        private int[] SetAxisPos = new int[8];
        private int[] TargetAxisPos = new int[8];
        private int[] TargetAxisSpeed = new int[8];
        private bool CommandMotion { get; set; } = false;
        private bool StepperRunning { get; set; } = true;
        private bool StartHoming { get; set; } = false;
        private bool StepperBoardHomed { get; set; } = false;
        private bool SetPosition { get; set; } = false;
        private PoKeysStepperBoardConfiguration StepperBoardConfig { get; set; }
        public bool Initialized { get; private set; } = false;

        PoKeysDevice poKeysDevice;
        private sPoKeysPEv2 PE = new sPoKeysPEv2();
        #endregion

        #region Constructor
        public PoKeysStepperPositioningSystem()
        {
        }

        public void Dispose()
        {
            DisableMotion();
            StepperRunning = false;

            if (posThread != null)
                posThread.Join();
        }
        #endregion

        #region public events
        public EventHandler<HomingStatusChangedEventArgs> HomingStatusChanged;
        private void raiseHomingStatusChanged(bool homingStatus)
        {
            if (HomingStatusChanged != null)
                HomingStatusChanged(this, new HomingStatusChangedEventArgs(homingStatus));
        }
        #endregion

        #region public properties
        public bool IsHomed { get; private set; } = false;
        public bool IsHoming { get; private set; } = false;
        public bool EmergencySwitchStatus => (PE.PulseEngineState == (byte)ePoKeysPEState.peSTOP_EMERGENCY) || (PE.PulseEngineState == (byte)ePoKeysPEState.peSTOP_LIMIT);
        public string Error { get; private set; }
        #endregion

        #region Initialize
        public bool Initialize(PoKeysStepperBoardConfiguration config, int? pokeysIndex)
        {
            if (Initialized)
                return false;

            if (poKeysDevice == null)
            {
                if (!pokeysIndex.HasValue)
                    return false;

                poKeysDevice = PoKeysEnumerator.Singleton.PoKeysDevice;

                if (!poKeysDevice.ConnectToDevice(pokeysIndex.Value))
                {
                    Error = Translations.Main.PokeysConnectError;
                    return false;
                }
            }

            // Configure the positioning device...
            poKeysDevice.PEv2_GetStatus(ref PE);
            for (int i = 0; i < 8; i++)
            {
                PE.param1 = (byte)i;
                poKeysDevice.PEv2_GetAxisConfiguration(ref PE);
            }

            UpdateConfiguration(config);

            //
            // Change Pulse Engine State to RUNNING
            //
            PE.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
            poKeysDevice.PEv2_SetState(ref PE);

            //
            // Get Pulse Engine Statue and Check to confirm we are RUNNING
            //
            poKeysDevice.PEv2_GetStatus(ref PE);
            StepperRunning = (PE.PulseEngineState == (byte)ePoKeysPEState.peRUNNING);

            Initialized = true;

            if (posThread == null)
            {
                posThread = new Thread(new ThreadStart(StepperPositioningThreadStub));
                posThread.Start();
            }

            return true;
        }
        #endregion

        #region SetConfiguration
        public void UpdateConfiguration(PoKeysStepperBoardConfiguration config)
        {
            StepperBoardConfig = config;

            // Configure pulse engine with 8 axes
            PE.PulseEngineEnabled = 8;
            PE.PulseGeneratorType = (byte)(0 | (1 << 7));  // Using external pulse generator with IO
            PE.ChargePumpEnabled = 0;   // Don't use charge pump output
                                        //config.EmergencySwitchPin = 0;  // No Emergency Switch Pin
                                        //                                // 0 - disabled i.e. none
                                        //                                // 1 - default input Pin 55
                                        //                                // 10 + 10-based pin ID
            PE.EmergencySwitchPolarity = (byte)(StepperBoardConfig.InvertEmergencySwitchPolarity ? 1 : 0);
            poKeysDevice.PEv2_SetupPulseEngine(ref PE);

            // Configure axes...
            for (int i = 0; i < 8; i++)
            {
                //
                // Set Axis Configuration variables
                //
                PE.AxisEnabledInvertMask[i] = 1; // Use inverted axis enabled signal for PoStep drivers ???
                PE.AxesConfig[i] = (byte)(ePEv2_AxisConfig.aoENABLED |
                    ePEv2_AxisConfig.aoINTERNAL_PLANNER |
                    ePEv2_AxisConfig.aoPOSITION_MODE);  // Use Position mode
                poKeysDevice.PEv2_GetStatus(ref PE);  // Get Status just to DEBUG - Remove for F4toPokeys code
                                                    //
                                                    // Enable Soft Limits for steppers with STOPs
                                                    // - Need to determine which steppers need this from the Config GUI
                                                    //
                PE.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoSOFT_LIMIT_ENABLED;
                poKeysDevice.PEv2_GetStatus(ref PE);  // Get Status just to DEBUG - Remove for F4toPokeys code

                //
                // My stepper is wired backwards so I added this. 
                // We may want to have a REVERSE Direction option as GUI Config Option
                //
                if (StepperBoardConfig.AxisConfig[i].Inverted)
                    PE.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoINVERTED;

                if (StepperBoardConfig.AxisConfig[i].HomeInverted)
                    PE.AxesConfig[i] |= (int)ePEv2_AxisConfig.aoINVERTED_HOME;

                if (StepperBoardConfig.AxisConfig[i].HasHomeSwitch)
                {
                    PE.PinHomeSwitch[i] = (byte)StepperBoardConfig.AxisConfig[i].PinHomeSwitch;
                    PE.AxesSwitchConfig[i] = (byte)ePEv2_AxisSwitchOptions.aoSWITCH_HOME;
                }
                else
                {
                    PE.PinHomeSwitch[i] = 0;
                    PE.AxesSwitchConfig[i] = (byte)(ePEv2_AxisSwitchOptions.aoSWITCH_HOME | ePEv2_AxisSwitchOptions.aoSWITCH_INVERT_HOME);
                }

                poKeysDevice.PEv2_GetStatus(ref PE);  // Get Status just to DEBUG - Remove for F4toPokeys code

                //
                // Set Axis Switch Configuration variables
                //
                PE.AxesSwitchConfig[i] = (int)(ePEv2_AxisSwitchOptions.aoSWITCH_HOME |
                    ePEv2_AxisSwitchOptions.aoSWITCH_INVERT_HOME);
                poKeysDevice.PEv2_GetStatus(ref PE);  // Get Status just to DEBUG - Remove for F4toPokeys code
                                                      //
                                                      // Set Axis Variables
                                                      //
                                                      //
                                                      // GUI Configuration Option
                                                      // - If stepper is 360 degree with a HOME switch we need a GUI configuration option that the pin for the axis.
                                                      //
                if (StepperBoardConfig.AxisConfig[i].HasHomeSwitch)
                    PE.PinHomeSwitch[i] = (byte)StepperBoardConfig.AxisConfig[i].PinHomeSwitch;// Convert.ToByte(i + 8);  // For testing, use Pins 8 - 15 as Home Switches for stepper 0 - 7
                else
                    PE.PinHomeSwitch[i] = 0;
                //config.PinHomeSwitch[i] = 8;  // #Matevz#: select pin 8 for the home switch for this single axis 

                PE.HomingSpeed[i] = (byte)StepperBoardConfig.AxisConfig[i].HomingSpeed; // 50;   // #Matevz#: homing speed is 50% of the maximum speed
                PE.HomingReturnSpeed[i] = (byte)StepperBoardConfig.AxisConfig[i].HomingReturnSpeed;// 20; // #Matevz#: homing return speed is 20% of the homing speed (therefore 10% of the max speed)
                PE.MaxSpeed[i] = StepperBoardConfig.AxisConfig[i].MaxSpeed;// 10; // Max speed: 15 kHz
                PE.MaxAcceleration[i] = StepperBoardConfig.AxisConfig[i].MaxAcceleration;// 0.010f; // Acceleration: 15 kHz / s
                PE.MaxDecceleration[i] = StepperBoardConfig.AxisConfig[i].MaxDecceleration; // 0.010f; // Acceleration: 15 kHz / s
                                                                                            //
                                                                                            // Set Soft Limits to 315 degree (3780 steps) X27 stepper position limits
                PE.SoftLimitMaximum[i] = StepperBoardConfig.AxisConfig[i].SoftLimitMaximum; // 3780;  // 315 degree
                PE.SoftLimitMinimum[i] = StepperBoardConfig.AxisConfig[i].SoftLimitMinimum; // 0;     // 0 degree
                PE.param1 = (byte)i; // Set parameter param1 to the bit mask to indicate what have the above Axis Configs set
                                     //
                                     // Write (Set) above Axis Configuration
                                     //
                poKeysDevice.PEv2_SetAxisConfiguration(ref PE); // Configure the axis
            }
        }
        #endregion

        #region Stepper Positioning
        public bool MoveToPosition(int[] targetPosition)
        {
            if (IsHoming)
                return false;

            for (int i = 0; i < 8; i++)
            {
                PE.ReferencePositionSpeed[i] = targetPosition[i];
            }

            CommandMotion = true;
            return true;
        }

        public bool MoveToPositionReal(float[] targetPosition)
        {
            if (IsHoming)
                return false;

            for (int i = 0; i < 8; i++)
            {
                PE.ReferencePositionSpeed[i] = (int)(targetPosition[i] * StepperBoardConfig.AxisConfig[i].StepsPerUnit);
            }

            CommandMotion = true;
            return true;
        }

        public bool MoveDiff(int[] diffPosition)
        {
            if (IsHoming)
                return false;

            for (int i = 0; i < 8; i++)
            {
                PE.ReferencePositionSpeed[i] = PE.CurrentPosition[i] + diffPosition[i];
            }

            CommandMotion = true;
            return true;
        }

        public bool MoveDiffReal(float[] diffPosition)
        {
            if (IsHoming)
                return false;

            for (int i = 0; i < 8; i++)
            {
                PE.ReferencePositionSpeed[i] = PE.CurrentPosition[i] + (int)(diffPosition[i] * StepperBoardConfig.AxisConfig[i].StepsPerUnit);
            }

            CommandMotion = true;
            return true;
        }

        public void EnableMotion()
        {
            NewState = (byte)ePoKeysPEState.peRUNNING;
            NewStateCommanded = true;
        }

        public void DisableMotion()
        {
            NewState = (byte)ePoKeysPEState.peSTOPPED;
            NewStateCommanded = true;
        }

        public void ResetPosition()
        {
            SetAxisPos = new int[8];
            SetPosition = true;
        }

        public float[] GetCurrentPositionReal()
        {
            float[] pos = new float[8];

            for (int i = 0; i < 8; i++)
            {
                pos[i] = (float)PE.CurrentPosition[i] / (float)StepperBoardConfig.AxisConfig[i].StepsPerUnit;
            }
            return pos;
        }
        #endregion

        #region Homing
        public void DoHoming()
        {
            if (StartHoming)
                return;

            StartHoming = true;
        }

        private bool HomeStepper()
        {
            PE.HomingStartMaskSetup = 0xFF;  // Home all Axis
            poKeysDevice.PEv2_StartHoming(ref PE);  // Initiate the HOMING 

            //
            // Wait for homing to complete...
            // - This is only for STOP style steppers.  We need to add code for 360 degree Sensor Steppers
            //
            if (!WaitHome(2000))
                return false;

            poKeysDevice.PEv2_SetState(ref PE);
            
            //
            // Stop the Pulse Engine so we can set each axis (stepper) postion to zero since we are HOMED
            //
            PE.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
            poKeysDevice.PEv2_SetState(ref PE);

            //
            // Set each stepper position to zero
            //
            for (int i = 0; i < 8; i++)
            {
                PE.param2 |= (byte)(1 << i);
                PE.PositionSetup[i] = 0;        // Set axis (stepper) position to Zero
                poKeysDevice.PEv2_SetPositions(ref PE);
            }

            //
            // Set Pulse Engine back to RUNNING
            PE.PulseEngineStateSetup = (byte)ePoKeysPEState.peRUNNING;
            poKeysDevice.PEv2_SetState(ref PE);

            //
            // Get Pulse Engine Statue and Check to confirm we are RUNNING
            poKeysDevice.PEv2_GetStatus(ref PE);
            StepperRunning = (PE.PulseEngineState == (byte)ePoKeysPEState.peRUNNING);

            PE.param2 = 0;
            PE.HomingStartMaskSetup = 0;
            return true;
        }

        private bool WaitHome(int timeout)
        {
            Stopwatch timer = Stopwatch.StartNew();

            while (timer.ElapsedMilliseconds < timeout && StepperRunning)
            {
                poKeysDevice.PEv2_GetStatus(ref PE);

                if (PE.PulseEngineState != (byte)ePoKeysPEState.peHOMING)
                {
                    if (PE.PulseEngineState == (byte)ePoKeysPEState.peHOME)
                    {
                        return true;
                    }

                    //PE.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
                    //poKeysDevice.PEv2_SetState(ref PE);
                    return false;
                }

                Thread.Sleep(10);
            }

            //PE.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
            //poKeysDevice.PEv2_SetState(ref PE);
            return false;
        }
        #endregion

        #region StepperPositioningthread
        private void StepperPositioningThreadStub()
        {
            if ((poKeysDevice == null) || (PE == null))
                return;

            while (StepperRunning)
            {
                // New Pulse engine state was commanded
                if (NewStateCommanded)
                {
                    PE.PulseEngineStateSetup = NewState;
                    poKeysDevice.PEv2_SetState(ref PE);
                    NewStateCommanded = false;
                }

                poKeysDevice.PEv2_GetStatus(ref PE);

                if (PE.PulseEngineState > 100) // Error or limit
                {
                    if (!ErrorFixed)
                    {
                        ErrorFixed = true;
                        Error = Translations.Main.StepperPositioningError;
                    }

                    // Do nothing else...
                    Thread.Sleep(10);
                    continue;
                }

                ErrorFixed = true;

                // Position update was commanded
                if (SetPosition)
                {
                    for (int i = 0; i < 8; i++)
                        PE.PositionSetup[i] = SetAxisPos[i];

                    PE.param2 = 0xFF; // Set all 8 positions
                    poKeysDevice.PEv2_SetPositions(ref PE);
                    SetPosition = false;
                }

                // Homing was commanded
                if (StartHoming)
                {
                    IsHoming = true;
                    raiseHomingStatusChanged(IsHoming);
                    if (HomeStepper())
                    {
                        IsHomed = true;
                    }

                    IsHoming = false;
                    raiseHomingStatusChanged(IsHoming);

                    StartHoming = false;
                }


                if (PE.PulseEngineState != (byte)ePoKeysPEState.peRUNNING)
                {
                    NewState = (byte)ePoKeysPEState.peRUNNING;
                    NewStateCommanded = true;
                }

                for (int i = 0; i < 8; i++)
                {
                    // Check axes states and change them to position mode...
                    if ((PE.AxesConfig[i] & (byte)ePEv2_AxisConfig.aoPOSITION_MODE) == 0)
                    {
                        PE.AxesConfig[i] |= (byte)(ePEv2_AxisConfig.aoPOSITION_MODE);
                        PE.param1 = (byte)i;
                        poKeysDevice.PEv2_SetAxisConfiguration(ref PE);
                    }
                }

                if (CheckNewState)
                {
                    switch (State)
                    {
                        case PoKeysStepperPositioningState.STOPPED:
                            // Stop the motion...
                            for (int i = 0; i < 8; i++)
                                PE.ReferencePositionSpeed[i] = PE.CurrentPosition[i];
                            break;

                        case PoKeysStepperPositioningState.MOVE_TO_POS:
                            for (int i = 0; i < 8; i++)
                                PE.ReferencePositionSpeed[i] = TargetAxisPos[i];
                            break;

                    }
                    poKeysDevice.PEv2_Move(ref PE);
                    CheckNewState = false;
                }
                else
                {
                    // Check existing state
                    switch (State)
                    {
                        case PoKeysStepperPositioningState.STOPPED:
                            break;

                        case PoKeysStepperPositioningState.MOVE_TO_POS:
                            // Check if move is complete
                            if (IsMoveComplete(5))
                            {
                                State = StateAfterMove;
                                if (State == PoKeysStepperPositioningState.STOPPED)
                                {
                                    // Well... do nothing...
                                }
                                else
                                {
                                    CheckNewState = true;
                                }
                            }
                            break;

                    }
                }

                if (CommandMotion)
                {
                    poKeysDevice.PEv2_Move(ref PE);
                    CommandMotion = false;
                }

                Thread.Sleep(10);
            }

            PE.PulseEngineStateSetup = (byte)ePoKeysPEState.peSTOPPED;
            poKeysDevice.PEv2_SetState(ref PE);
            poKeysDevice.DisconnectDevice();
        }
        #endregion

        #region IsMoveComplete
        public bool IsMoveComplete(int tolerance)
        {
            if (IsHoming)
                return false;

            // Check if current positions are inside tolerances...
            for (int i = 0; i < 8; i++)
            {
                if (Math.Abs(PE.CurrentPosition[i] - PE.ReferencePositionSpeed[i]) > tolerance)
                    return false;
            }

            return true;

        }
        #endregion
    }

    public class HomingStatusChangedEventArgs : EventArgs
    {
        public HomingStatusChangedEventArgs(bool homingStatus)
        {
            this.HomingStatus = homingStatus;
        }

        public readonly bool HomingStatus;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using F4SharedMem;
using F4SharedMem.Headers;

namespace F4ToPokeys
{
    #region FalconLight

    public abstract class FalconLight
    {
        #region Construction/Destruction

        protected FalconLight(string group, string label)
        {
            Label = label;
            Group = group;
        }

        #endregion // Construction/Destruction

        #region Label
        public string Label { get; private set; }
        #endregion // Label

        #region Group
        public string Group { get; private set; }
        #endregion // Group

        #region FalconLightChanged

        public event EventHandler<FalconLightChangedEventArgs> FalconLightChanged
        {
            add
            {
                falconLightChanged += value;

                ++nbUser;
                if (nbUser == 1)
                    FalconConnector.Singleton.FlightDataLightsChanged += OnFlightDataChanged;
            }

            remove
            {
                falconLightChanged -= value;

                --nbUser;
                if (nbUser == 0)
                    FalconConnector.Singleton.FlightDataLightsChanged -= OnFlightDataChanged;
            }
        }

        protected void raiseFalconLightChanged(bool? oldValue, bool? newValue)
        {
            if (falconLightChanged != null)
                falconLightChanged(this, new FalconLightChangedEventArgs(oldValue, newValue));
        }

        private EventHandler<FalconLightChangedEventArgs> falconLightChanged;
        private int nbUser = 0;

        #endregion FalconLightChanged

        #region OnFlightDataChanged
        protected abstract void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e);
        #endregion // OnFlightDataChanged
    }

    #endregion // FalconLight

    #region FalconLightBit

    public abstract class FalconLightBit : FalconLight
    {
        #region Construction/Destruction

        protected FalconLightBit(string group, string label, int bit)
            : base(group, label)
        {
            this.bit = bit;
        }

        #endregion // Construction/Destruction

        #region bit
        protected readonly int bit;
        #endregion // bit

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            if (oldValue != newValue)
            {
                raiseFalconLightChanged(oldValue, newValue);
            }
        }
        #endregion // OnFlightDataChanged

        #region getValue

        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getNonNullValue(flightData);
        }

        protected abstract bool getNonNullValue(FlightData flightData);

        #endregion // getValue
    }

    #endregion // FalconLightBit

    #region FalconLightBit1

    public class FalconLightBit1 : FalconLightBit
    {
        #region Construction/Destruction

        public FalconLightBit1(string group, string label, LightBits bit)
            : base(group, label, (int)bit)
        {
        }

        #endregion // Construction/Destruction

        #region getNonNullValue
        protected override bool getNonNullValue(FlightData flightData)
        {
            return (flightData.lightBits & bit) != 0;
        }
        #endregion // getNonNullValue
    }

    #endregion // FalconLightBit

    #region FalconLightBitFlip
    //
    ////
    //// Eric's LightBit Flip Enhancement - Unused
    ////
    //public abstract class FalconLightBitFlip : FalconLight
    //{
    //    #region Construction/Destruction

    //    protected FalconLightBitFlip(string group, string label, int bit)
    //        : base(group, label)
    //    {
    //        this.bit = bit;
    //    }

    //    #endregion // Construction/Destruction

    //    #region bit
    //    protected readonly int bit;
    //    #endregion // bit

    //    #region OnFlightDataChanged
    //    protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
    //    {
    //        bool? oldValue = getValue(e.oldFlightData);
    //        bool? newValue = getValue(e.newFlightData);
    //        bool EmerBusPwr = Convert.ToBoolean(PowerBits.BusPowerEmergency);

    //        if (EmerBusPwr == true)
    //        {
    //            if (newValue == true) { raiseFalconLightChanged(true, false); } else { raiseFalconLightChanged(false, true); }
    //        }
    //        else { }
    //        /* if (oldValue != newValue)
    //         {
    //             raiseFalconLightChanged(oldValue, newValue);
    //         }*/
    //    }
    //    #endregion // OnFlightDataChanged

    //    #region getValue

    //    private bool? getValue(FlightData flightData)
    //    {
    //        if (flightData == null)
    //            return null;
    //        else
    //            return getNonNullValue(flightData);
    //    }

    //    protected abstract bool getNonNullValue(FlightData flightData);

    //    #endregion // getValue
    //}

    #endregion // FalconLightBitFlip

    #region FalconMissileLightBit

    //public abstract class FalconMissileLightBit : FalconLight
    //{
    //    #region Construction/Destruction

    //    protected FalconMissileLightBit(string group, string label, int bit, int bit2)
    //        : base(group, label)
    //    {
    //        this.bit = bit;
    //        this.bit2 = bit2;
    //    }

    //    #endregion // Construction/Destruction

    //    #region bit
    //    protected readonly int bit;
    //    protected readonly int bit2;
    //    #endregion // bit

    //    #region OnFlightDataChanged
    //    protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
    //    {
    //        bool? oldValue = getValue(e.oldFlightData);
    //        bool? newValue = getValue(e.newFlightData);

    //        if (oldValue != newValue)
    //        {
    //            raiseFalconLightChanged(oldValue, newValue);
    //        }
    //    }
    //    #endregion // OnFlightDataChanged

    //    #region getValue

    //    private bool? getValue(FlightData flightData)
    //    {
    //        if (flightData == null)
    //            return null;
    //        else
    //            return getNonNullValue(flightData);
    //    }

    //    protected abstract bool getNonNullValue(FlightData flightData);

    //    #endregion // getValue
    //}

    #endregion // FalconMissileLightBit

    #region FalconLightBit2

    public class FalconLightBit2 : FalconLightBit
    {
        #region Construction/Destruction

        public FalconLightBit2(string group, string label, LightBits2 bit)
            : base(group, label, (int)bit)
        {
        }

        #endregion // Construction/Destruction

        #region getNonNullValue
        protected override bool getNonNullValue(FlightData flightData)
        {
            return (flightData.lightBits2 & bit) != 0;
        }
        #endregion // getNonNullValue
    }

    #endregion // FalconLightBit2

    #region FalconLightBit3

    public class FalconLightBit3 : FalconLightBit
    {
        #region Construction/Destruction

        public FalconLightBit3(string group, string label, LightBits3 bit)
            : base(group, label, (int)bit)
        {
        }
        //
        // The following code was removed because Lightnings Tools no longer has a Bms4LightBits3 structure, it
        // all of those bits were moved to LightBits3 
        //
        //public FalconLightBit3(string group, string label, Bms4LightBits3 bit)
        //    : base(group, label, (int)bit)
        //{
        //}

        #endregion // Construction/Destruction

        #region getNonNullValue
        protected override bool getNonNullValue(FlightData flightData)
        {
            return (flightData.lightBits3 & bit) != 0;
        }
        #endregion // getNonNullValue
    }

    #endregion // FalconLightBit3

    #region FalconHsiBits

    public class FalconHsiBits : FalconLightBit
    {
        #region Construction/Destruction

        public FalconHsiBits(string group, string label, HsiBits bit)
            : base(group, label, (int)bit)
        {
        }

        #endregion // Construction/Destruction

        #region getNonNullValue
        protected override bool getNonNullValue(FlightData flightData)
        {
            return (flightData.hsiBits & bit) != 0;
        }
        #endregion // getNonNullValue
    }

    #endregion // FalconHsiBits

    #region FalconBlinkBits
    //
    // Eric's Original Blink Enhancement
    //

    //public class FalconBlinkBits : FalconLightBit
    //{
    //    #region Construction/Destruction

    //    public FalconBlinkBits(string group, string label, BlinkBits bit)
    //        : base(group, label, (int)bit)
    //    {
    //    }

    //    #endregion // Construction/Destruction

    //    #region getNonNullValue
    //    protected override bool getNonNullValue(FlightData flightData)
    //    {
    //        return (flightData.blinkBits & bit) != 0;
    //    }
    //    #endregion // getNonNullValue
    //}

    #endregion // FalconBlinkBits

    #region FalconPowerBits

    public class FalconPowerBits : FalconLightBit
    {
        #region Construction/Destruction

        public FalconPowerBits(string group, string label, PowerBits bit)
            : base(group, label, (int)bit)
        {
        }

        #endregion // Construction/Destruction

        #region getNonNullValue
        protected override bool getNonNullValue(FlightData flightData)
        {
            return (flightData.powerBits & bit) != 0;
        }
        #endregion // getNonNullValue
    }

    #endregion // FalconPowerBits

    #region FalconLightGearDown

    public abstract class FalconLightGearDown : FalconLightBit3
    {
        #region Construction/Destruction

        public FalconLightGearDown(string group, string label, LightBits3 bit)
            : base(group, label, bit)
        {
        }

        #endregion // Construction/Destruction

        #region getNonNullValue

        protected override bool getNonNullValue(FlightData flightData)
        {
            //
            // The following code was removed because Lightnings Tools no longer supports anything but Falcon BMS4.3
            //
            //if (flightData.DataFormat == FalconDataFormats.OpenFalcon || flightData.DataFormat == FalconDataFormats.BMS4)
            //{
            //    return base.getNonNullValue(flightData);
            //}
            //else
            //{
            //    return getGearDownValue(flightData);
            //}
            return base.getNonNullValue(flightData);
            //return getGearDownValue(flightData);
        }

        protected abstract bool getGearDownValue(FlightData flightData);

        #endregion // getNonNullValue
    }

    #endregion // FalconLightGearDown

    #region FalconLightNoseGearDown

    public class FalconLightNoseGearDown : FalconLightGearDown
    {
        #region Construction/Destruction

        public FalconLightNoseGearDown(string group, string label)
            : base(group, label, LightBits3.NoseGearDown)
        {
        }

        #endregion // Construction/Destruction

        #region getGearDownValue
        protected override bool getGearDownValue(FlightData flightData)
        {
            return flightData.NoseGearPos == 1.0f;
        }
        #endregion // getGearDownValue
    }

    #endregion // FalconLightNoseGearDown

    #region FalconLightLeftGearDown

    public class FalconLightLeftGearDown : FalconLightGearDown
    {
        #region Construction/Destruction

        public FalconLightLeftGearDown(string group, string label)
            : base(group, label, LightBits3.LeftGearDown)
        {
        }

        #endregion // Construction/Destruction

        #region getGearDownValue
        protected override bool getGearDownValue(FlightData flightData)
        {
            return flightData.LeftGearPos == 1.0f;
        }
        #endregion // getGearDownValue
    }

    #endregion // FalconLightLeftGearDown

    #region FalconLightRightGearDown

    public class FalconLightRightGearDown : FalconLightGearDown
    {
        #region Construction/Destruction

        public FalconLightRightGearDown(string group, string label)
            : base(group, label, LightBits3.RightGearDown)
        {
        }

        #endregion // Construction/Destruction

        #region getGearDownValue
        protected override bool getGearDownValue(FlightData flightData)
        {
            return flightData.RightGearPos == 1.0f;
        }
        #endregion // getGearDownValue
    }

    #endregion // FalconLightRightGearDown

    #region FalconLightSpeedBrake

    public class FalconLightSpeedBrake : FalconLight
    {
        #region Construction/Destruction
        //
        // Eric's Bus Pwr Enhancement
        //
        bool EmergBusPwr = Convert.ToBoolean(PowerBits.BusPowerEmergency);

        public FalconLightSpeedBrake(string group, string label, float percent)
            : base(group, label)
        {
            this.percent = percent;
        }

        #endregion // Construction/Destruction

        #region percent
        protected readonly float percent;
        #endregion // percent

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            //
            // Eric's Bus Pwr Enhancement
            //
            if (EmergBusPwr == true)
            {
                if (oldValue != newValue)
                {
                    raiseFalconLightChanged(oldValue, newValue);
                }
            }
            else { }

            //if (oldValue != newValue)
            //{
            //    raiseFalconLightChanged(oldValue, newValue);
            //}
        }
        #endregion // OnFlightDataChanged

        #region getValue

        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return flightData.speedBrake > percent;
        }

        #endregion // getValue
    }

    #endregion // FalconLightSpeedBrake

    #region FalconMagneticSwitchReset
    //
    // Original code by Michael for use with Homebuilt Magnetic Switches that need an output to force the switch
    // into the off position.
    //
    public class FalconMagneticSwitchReset : FalconLight
    {
        #region Construction/Destruction
        public FalconMagneticSwitchReset(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;

            timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = TimeSpan.FromMilliseconds(200);
        }
        #endregion

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            if (oldValue != newValue && newValue == true)
            {
                timer.Stop();
                raiseFalconLightChanged(oldValue, newValue);
                timer.Start();
            }
        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion

        #region Timer
        private void timerTick(object sender, EventArgs e)
        {
            timer.Stop();
            raiseFalconLightChanged(true, false);
        }

        private DispatcherTimer timer;
        #endregion
    }
    #endregion

    #region FalconBlinkingLamp
    //
    // Original code by Eric. Modified by Beau
    //  - Provides functionality for Lights that can both be on steady or blink depending on F4Shared Memory BlinkBits.
    //

    public class FalconBlinkingLamp : FalconLight
    {
        private bool lamplit = false;
        private bool newBlinkingBitSet = false;
        private bool oldBlinkingBitSet = false;
        private bool LightBitSet = false;


        #region Construction/Destruction

        public FalconBlinkingLamp(string group, string label, Func<FlightData, bool> getFlightDataValue, BlinkBits blinkbit, int rate)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;
            this.blinkbit = blinkbit;


            timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = TimeSpan.FromMilliseconds(rate);
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        private readonly BlinkBits blinkbit;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);
            if (newValue == true) { LightBitSet = true; } else { LightBitSet = false; }
            if (e.newFlightData != null)    // Catch case when BMS has exited and e.newFlightData is null
            {
                newBlinkingBitSet = ((e.newFlightData.blinkBits & (int)blinkbit) != 0);
            }
            else
            {
                newBlinkingBitSet = false;
            }
            if (e.oldFlightData != null)    // Catch case when BMS has exited and e.oldFlightData is null
            {
                oldBlinkingBitSet = ((e.oldFlightData.blinkBits & (int)blinkbit) != 0);
            }
            else
            {
                oldBlinkingBitSet = false;
            }
            if ((oldValue != newValue) || (newBlinkingBitSet != oldBlinkingBitSet))
            {
                timer.Stop();
                raiseFalconLightChanged(oldValue, newValue);
                if (newValue == true)           // LB went from Off to On
                {
                    lamplit = true;
                    if (newBlinkingBitSet)      // Blinking. Start Blink timer.
                    {
                        timer.Start();
                    }
                    else                        // Not Blinking. Stop Blink timer.
                    {
                        timer.Stop();
                    }
                }
                else                            // LB went from On to Off
                {
                    lamplit = false;
                    timer.Stop();
                }
            }
        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion

        #region Timer
        private void timerTick(object sender, EventArgs e)
        {
            if (LightBitSet)        // Light is On.
            {
                if (newBlinkingBitSet) // Light Blinking?
                {
                    if (lamplit)    // Lamp is On. Turn it off.
                    {
                        lamplit = false;
                        timer.Stop();
                        raiseFalconLightChanged(true, false);
                        timer.Start();
                    }
                    else            // Lamp is Off.  Turn it on.
                    {
                        lamplit = true;
                        timer.Stop();
                        raiseFalconLightChanged(false, true);
                        timer.Start();
                    }
                }
                else                // Light is On but no longer Blinking. Stop blink timer.
                {
                    lamplit = true;
                    timer.Stop();
                    raiseFalconLightChanged(false, true);
                }
            }
            else                    // Light is off. Stop blink timer.
            {
                lamplit = false;
                timer.Stop();
                raiseFalconLightChanged(true, false);
            }
        }

        private DispatcherTimer timer;
        #endregion
    }
    #endregion // FalconBlinkingLamp

    #region FalconRPMOver65Percent
    //
    // Detects when Engine RPM is over 65 percent.
    // - Used to control blower in the cockpit that blows air through Air Vents to add realism.
    //

    public class FalconRPMOver65Percent : FalconLight
    {
        
        #region Construction/Destruction

        public FalconRPMOver65Percent(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion  //Construction/Destruction

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            if (oldValue != newValue)
            {
                raiseFalconLightChanged(oldValue, newValue);
            }
        }
        #endregion  //OnFlightDataChanged

        #region getValue
        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion  //getValue

    }
    #endregion
					  
    #region FalconGearHandleSol

    public class FalconGearHandleSol : FalconLight
    {
        // public int lamplit;
        public int Engaged = 0;
        public int onGnd = 0;
        bool MainGenOn = Convert.ToBoolean(PowerBits.MainGenerator);
        bool InPit = Convert.ToBoolean(HsiBits.Flying);

        #region Construction/Destruction

        public FalconGearHandleSol(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;

            //   this.InPit = (int)HsiBits.Flying;
            //  this.MainGenOn = (int)PowerBits.MainGenerator;

            timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = TimeSpan.FromMilliseconds(260);
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);
            if (newValue == true) { Engaged = 1; } else { Engaged = 0; }
            // if (onGnd == 1) { Engaged = 1; } else { Engaged = 0; }

            //  if (InPit == 1) { InPit = 1; } else {InPit = 0;}

            if (oldValue != newValue && newValue == true)
            {
                timer.Stop();
                raiseFalconLightChanged(oldValue, newValue);
                timer.Start();
            }

        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {

            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion

        #region Timer
        private void timerTick(object sender, EventArgs e)
        {
            if (Engaged == 0 && InPit == true)
            {
                // lamplit = 1;
                timer.Stop();
                raiseFalconLightChanged(true, false);
                timer.Start();
            }
            else
            {
                // lamplit = 0;
                timer.Stop();
                raiseFalconLightChanged(false, true);
                timer.Start();
            }
            // }
            //  else
            //  {
            // dahtimer.Stop();
            //raiseFalconLightChanged(false, true);
            // dahtimer.Start();
            //  }

        }

        private DispatcherTimer timer;
        #endregion
    }
    #endregion 

    #region RealSpeedBrakeOpen

    public class RealSpeedBrakeOpen : FalconLight
    {
        int Engaged;
        bool EmergBusPwr = Convert.ToBoolean(PowerBits.BusPowerEmergency);


        #region Construction/Destruction

        public RealSpeedBrakeOpen(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;

            timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = TimeSpan.FromMilliseconds(260);
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            if (newValue == true) { Engaged = 1; } else { Engaged = 0; }
            //   if (EmergBusPwr == true)
            //   {

            if (oldValue != newValue && newValue == true)
            {
                timer.Stop();
                raiseFalconLightChanged(oldValue, newValue);
                timer.Start();
            }
            // }
            // else { }

        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {

            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion

        #region Timer
        private void timerTick(object sender, EventArgs e)
        {
            if (Engaged == 1 && EmergBusPwr == true)
            {
                // lamplit = 1;
                timer.Stop();
                raiseFalconLightChanged(false, true);
                timer.Start();
            }
            else
            {
                // lamplit = 0;
                timer.Stop();
                raiseFalconLightChanged(true, false);
                timer.Start();
            }
        }

        private DispatcherTimer timer;
        #endregion
    }
    #endregion   //RealSpeedBrakeOpen

    #region RealSpeedBrakeClosed

    public class RealSpeedBrakeClosed : FalconLight
    {
        int Engaged;
        bool EmergBusPwr = Convert.ToBoolean(PowerBits.BusPowerEmergency);


        #region Construction/Destruction

        public RealSpeedBrakeClosed(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;

            timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = TimeSpan.FromMilliseconds(260);
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            if (newValue == true) { Engaged = 1; } else { Engaged = 0; }
            //   if (EmergBusPwr == true)
            //   {

            if (oldValue != newValue && newValue == true)
            {
                timer.Stop();
                raiseFalconLightChanged(oldValue, newValue);
                timer.Start();
            }
            // }
            // else { }

        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {

            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion

        #region Timer
        private void timerTick(object sender, EventArgs e)
        {
            if (Engaged == 1 && EmergBusPwr == true)
            {
                // lamplit = 1;
                timer.Stop();
                raiseFalconLightChanged(true, false);
                timer.Start();
            }
            else
            {
                // lamplit = 0;
                timer.Stop();
                raiseFalconLightChanged(false, true);
                timer.Start();
            }
        }

        private DispatcherTimer timer;
        #endregion
    }
    #endregion // RealSpeedBrakeClosed

    #region FalconHandleBit
    //
    // Unused Class
    //

    //public class FalconHandleBit : FalconLightBitFlip
    //{

    //    #region Construction/Destruction

    //    public FalconHandleBit(string group, string label, LightBits3 bit)
    //        : base(group, label, (int)bit)
    //    {
    //    }

    //    //public FalconHandleBit(string group, string label, Bms4LightBits3 bit)
    //    //    : base(group, label, (int)bit)
    //    //{
    //    //}

    //    #endregion // Construction/Destruction

    //    #region getNonNullValue
    //    protected override bool getNonNullValue(FlightData flightData)
    //    {
    //        return (flightData.lightBits3 & bit) != 0;
    //    }
    //    #endregion // getNonNullValue
    //}

    #endregion // FalconHandleBit

    #region FalconLightChangedEventArgs

    public class FalconLightChangedEventArgs : EventArgs
    {
        public FalconLightChangedEventArgs(bool? oldValue, bool? newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public readonly bool? oldValue;
        public readonly bool? newValue;
    }

    #endregion // FalconLightChangedEventArgs

    #region FalconTacanBits

    public class FalconTacanBits : FalconLightBit
    {

        public FalconTacanBits(string group, string label, TacanBits bit, int tacanSource)
            : base(group, label, (int)bit)
        {
            this.tacanSource = tacanSource;
        }

        #region tacanSource
        protected readonly int tacanSource;
        #endregion // tacanSource

        #region getNonNullValue
        protected override bool getNonNullValue(FlightData flightData)
        {
            return (flightData.tacanInfo[tacanSource] & bit) != 0;
        }
        #endregion // getNonNullValue
    }

    #endregion //FalconTacanBits

    #region FalconDataBits

    public class FalconDataBits : FalconLight
    {

        #region Construction/Destruction

        public FalconDataBits(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);

            if (oldValue != newValue)
            {
                raiseFalconLightChanged(oldValue, newValue);
            }
        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion
    }
    #endregion // FalconDataBits

    #region FalconTWPOpenLight

    public class FalconTWPOpenLight : FalconLight
    {

        #region Construction/Destruction

        private bool newAuxPwrOn = false;
        private bool oldAuxPwrOn = false;

        public FalconTWPOpenLight(string group, string label, Func<FlightData, bool> getFlightDataValue)
            : base(group, label)
        {
            this.getFlightDataValue = getFlightDataValue;
        }

        #endregion // Construction/Destruction

        #region getFlightDataValue
        private readonly Func<FlightData, bool> getFlightDataValue;
        #endregion

        #region OnFlightDataChanged
        protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            bool? oldValue = getValue(e.oldFlightData);
            bool? newValue = getValue(e.newFlightData);
            if (oldValue != null && newValue != null)
            {
                newAuxPwrOn = ((e.newFlightData.lightBits2 & (int)LightBits2.AuxPwr) != 0);
                oldAuxPwrOn = ((e.oldFlightData.lightBits2 & (int)LightBits2.AuxPwr) != 0);

                if (newAuxPwrOn)   // Only Flip-Flop OPEN Light if RWR is On.
                {
                    if (oldValue != newValue)   // PRIORITY Light status changed. Update OPEN Light
                    {
                        if (newValue == true)   // PRIORITY Light went On.  Turn OPEN Light OFF.
                        {
                            raiseFalconLightChanged(true, false);
                        }
                        else   // PRIORITY Light went OFF.  Turn OPEN Light ON.
                        {
                            raiseFalconLightChanged(false, true);
                        }
                    }
                }
                if (oldAuxPwrOn != newAuxPwrOn)     // AuxPwr Changed
                {
                    if (newAuxPwrOn && newValue == false)  // AuxPwr came back on & PRIORITY Light is Off. Turn on OPEN Light.
                    {
                        raiseFalconLightChanged(false, true);
                    }
                    else    // AuxPwr came back on but PRIORITY Light is One. Turn off OPEN Light.
                    {
                        raiseFalconLightChanged(true, false);
                    }
                }
            }
        }
        #endregion

        #region getValue
        private bool? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataValue(flightData);
        }
        #endregion
    }
    #endregion // FalconTWPOpenLight

    #region FalconMarkerBeacon
    //
    // Removed - Does not support steady light function.  Use OM and MM Lights and wire outputs in parallel to MRKBCN lamp.
    //
    //
    //public class FalconMarkerBeacon : FalconLight
    //{
    //    private bool MarkBeaconLampOn = false;
    //    private bool OMbitset = false;
    //    private bool MMbitset = false;

    //    #region Construction/Destruction

    //    public FalconMarkerBeacon(string group, string label, Func<FlightData, bool> getFlightDataValue)
    //        : base(group, label)
    //    {
    //        this.getFlightDataValue = getFlightDataValue;

    //        timer = new DispatcherTimer();
    //        timer.Tick += timerTick;
    //    }

    //    #endregion // Construction/Destruction

    //    #region getFlightDataValue
    //    private readonly Func<FlightData, bool> getFlightDataValue;
    //    #endregion  //getFlightDataValue

    //    #region OnFlightDataChanged
    //    protected override void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
    //    {
    //        bool? oldValue = getValue(e.oldFlightData);
    //        bool? newValue = getValue(e.newFlightData);

    //        if (oldValue != newValue)
    //        {
    //            if ((int)(e.newFlightData.hsiBits & (int)HsiBits.MiddleMarker) != 0)
    //            {
    //                MMbitset = true;
    //                timer.Interval = TimeSpan.FromMilliseconds(250);
    //            }
    //            else { MMbitset = false; }

    //            if ((int)(e.newFlightData.hsiBits & (int)HsiBits.OuterMarker) != 0)
    //            {
    //                OMbitset = true;
    //                timer.Interval = TimeSpan.FromMilliseconds(500);
    //            }
    //            else { OMbitset = false; }

    //            if (MMbitset)
    //            {
    //                timer.Stop();
    //                raiseFalconLightChanged(false, true);
    //                timer.Start();
    //                MarkBeaconLampOn = true;
    //            }
    //            if (OMbitset)
    //            {
    //                timer.Stop();
    //                raiseFalconLightChanged(false, true);
    //                timer.Start();
    //                MarkBeaconLampOn = true;
    //            }
    //        }
    //    }
    //    #endregion  //OnFlightDataChanged

    //    #region getValue
    //    private bool? getValue(FlightData flightData)
    //    {
    //        if (flightData == null)
    //            return null;
    //        else
    //            return getFlightDataValue(flightData);
    //    }
    //    #endregion  //getValue

    //    #region Timer
    //    private void timerTick(object sender, EventArgs e)
    //    {
    //        if (OMbitset || MMbitset)   // OM or MM is being received. Start LightBitSet Marker Beacon Lamp.
    //        {
    //            if (MarkBeaconLampOn)   // If On, Turn Marker Beacon Lamp Off
    //            {
    //                MarkBeaconLampOn = false;
    //                timer.Stop();
    //                raiseFalconLightChanged(true, false);
    //                timer.Start();
    //            }
    //            else                    // If Off, Turn Marker Beacon Lamp On
    //            {
    //                MarkBeaconLampOn = true;
    //                timer.Stop();
    //                raiseFalconLightChanged(false, true);
    //                timer.Start();
    //            }
    //        } 
    //        else                        //Neither OM nor MM is being received.
    //        {
    //            timer.Stop();
    //            raiseFalconLightChanged(true, false);
    //        }
    //    }

    //    private DispatcherTimer timer;
    //}

    //#endregion  //Timer
    #endregion  //FalconMarkerBeacon
}

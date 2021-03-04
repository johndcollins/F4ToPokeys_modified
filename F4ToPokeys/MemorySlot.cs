using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using F4SharedMem;


namespace F4ToPokeys
{
    #region MemorySlot
    public class MemorySlot
    {
        #region Construction
        public MemorySlot(string label, Func<FlightData, uint> getFlightDataProperty, uint slotMask, int shiftBits, string displayShiftBits, bool bcd, string format, bool bupUhfFreq = false)
        {
            Label = label;
            Format = format;
            SlotMask = slotMask;
            ShiftBits = shiftBits;
            BCD = bcd;
            DisplayShiftBits = displayShiftBits;
            BupUhfFreq = bupUhfFreq;
            this.getFlightDataProperty = getFlightDataProperty;
        }
        #endregion

        #region Label
        public string Label { get; private set; }
        #endregion

        #region Format
        public string Format { get; private set; }
        #endregion

        #region getFlightDataProperty
        private readonly Func<FlightData, uint> getFlightDataProperty;
        #endregion
         
        #region SlotMask
        public uint SlotMask { get; private set; }
        #endregion

        #region ShiftBits
        public int ShiftBits { get; private set; }
        #endregion

        #region DisplayShiftBits
        public string DisplayShiftBits { get; private set; }
        #endregion

        public bool BCD { get; private set; }

        public bool BupUhfFreq { get; private set; }

        #region MemorySlotChanged
        public event EventHandler<MemorySlotChangedEventArgs> MemorySlotChanged
        {
            add
            {
                memorySlotChanged += value;

                ++nbUser;
                if (nbUser == 1)
                    FalconConnector.Singleton.FlightDataChanged += OnFlightDataChanged;
            }

            remove
            {
                memorySlotChanged -= value;

                --nbUser;
                if (nbUser == 0)
                    FalconConnector.Singleton.FlightDataChanged -= OnFlightDataChanged;
            }
        }

        protected void raiseMemorySlotChanged(uint? falconValue)
        {
            if (memorySlotChanged != null)
                memorySlotChanged(this, new MemorySlotChangedEventArgs(falconValue));
        }

        private EventHandler<MemorySlotChangedEventArgs> memorySlotChanged;
        private int nbUser = 0;
        #endregion

        #region OnFlightDataChanged
        private void OnFlightDataChanged(object sender, FlightDataChangedEventArgs e)
        {
            uint? oldValue = getValue(e.oldFlightData);
            uint? newValue = getValue(e.newFlightData);

            if (oldValue != newValue)
            {
                raiseMemorySlotChanged(newValue);
            }
        }
        #endregion

        #region Value
        private uint? getValue(FlightData flightData)
        {
            if (flightData == null)
                return null;
            else
                return getFlightDataProperty(flightData);
        }
        #endregion
    }
    #endregion

    #region MemorySlotChangedEventArgs
    public class MemorySlotChangedEventArgs : EventArgs
    {
        public MemorySlotChangedEventArgs(uint? falconValue)
        {
            this.falconValue = falconValue;
        }

        public readonly uint? falconValue;
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;

namespace F4ToPokeys
{
    public class MemorySlotOutput : MemorySlotConsumer
    {
        #region Construction/Destruction
        public MemorySlotOutput(int slotId)
        {
            SlotId = slotId;
        }
        #endregion // Construction/Destruction

        #region SlotId
        public int? SlotId
        {
            get { return slotId; }
            set
            {
                if (slotId == value)
                    return;
                slotId = value;
                RaisePropertyChanged("SlotId");

                updateStatus();
            }
        }

        private int? slotId;
        #endregion // SlotId

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

        #region owner
        public void setOwner(PoKeys poKeys)
        {
            owner = poKeys;
            //updateStatus();
        }

        private PoKeys owner;
        #endregion // owner

        #region updateStatus
        public override void updateStatus()
        {
            if (owner == null)
                return;

            if (!SlotId.HasValue)
            {
                Error = null;
            }
            else
            {
                uint currentValue = memorySlots[SlotId.GetValueOrDefault()];
                uint newValue = 0;
                if (Slot.BupUhfFreq)
                {
                    if (FalconValue.HasValue)
                    {
                        if (BupPresetFrequencies.ContainsKey(FalconValue.GetValueOrDefault()))
                            newValue = BupPresetFrequencies[FalconValue.GetValueOrDefault()];
                        else
                            newValue = 0;
                    }
                    else
                        newValue = 0;
                }
                else
                    newValue = FalconValue.GetValueOrDefault();

                uint maska = this.Slot.SlotMask << this.Slot.ShiftBits;
                currentValue = currentValue & ~maska;

                if (this.Slot.BCD)
                {
                    uint orig_value = newValue;
                    uint ten_millions = orig_value / 10000000;
                    orig_value = orig_value - (ten_millions * 10000000);
                    uint millions = orig_value / 1000000;
                    orig_value = orig_value - (millions * 1000000);
                    uint hundred_thousands = orig_value / 100000;
                    orig_value = orig_value - (hundred_thousands * 100000);
                    uint ten_thousands = orig_value / 10000;
                    orig_value = orig_value - (ten_thousands * 10000);
                    uint thousands = orig_value / 1000;
                    orig_value = orig_value - (thousands * 1000);
                    uint hundreds = orig_value / 100;
                    orig_value = orig_value - (hundreds * 100);
                    uint tens = orig_value / 10;
                    orig_value = orig_value - (tens * 10);
                    uint units = orig_value;
                    uint bcd_value = (ten_millions << 28) | (millions << 24) | (hundred_thousands << 20) | (ten_thousands << 16) | (thousands << 12) | (hundreds << 8) | (tens << 4) | units;

                    newValue = bcd_value;
                }

                newValue = currentValue | (newValue << this.Slot.ShiftBits);

                memorySlots[SlotId.GetValueOrDefault()] = newValue;

                writeMemorySlot(SlotId.GetValueOrDefault());
            }

        }

        private uint GetSlotValue()
        {
            uint result = 0;
            foreach (MemorySlotOutput outputSlot in owner.MemorySlotOutputList)
            {
                if (outputSlot.SlotId == SlotId)
                    result = result + ((uint)outputSlot.FalconValue.GetValueOrDefault() << outputSlot.Slot.ShiftBits);
            }

            return result;
        }

        #endregion // updateStatus

        #region writeMemorySlot
        private void writeMemorySlot(int slot)
        {
            if (string.IsNullOrEmpty(Error) && owner != null && owner.Connected && SlotId.HasValue)
            {
                byte[] data = BitConverter.GetBytes(memorySlots[slot]);
                owner.PokeysDevice.PoILWriteMemory(4, (ushort)(slot * 4), 4, ref data);
            }
        }
        #endregion // writeOutputState
    }

}

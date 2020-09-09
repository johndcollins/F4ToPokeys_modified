using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;

namespace F4ToPokeys
{
    public abstract class MemorySlotConsumer : BindableObject, IDisposable
    {
        #region Construction/Destruction
        protected MemorySlotConsumer()
        {
            WriteMemorySlot = new RelayCommand(executeWriteMemorySlot);
            ReadMemorySlot = new RelayCommand(executeReadMemorySlot);
        }

        public void Dispose()
        {
            if (slot != null)
                slot.MemorySlotChanged -= OnMemorySlotChanged;
        }
        #endregion

        #region MemorySlot
        [XmlIgnore]
        public MemorySlot Slot
        {
            get { return slot; }
            set
            {
                if (slot == value)
                    return;
                if (slot != null)
                    slot.MemorySlotChanged -= OnMemorySlotChanged;
                slot = value;
                if (slot != null)
                    slot.MemorySlotChanged += OnMemorySlotChanged;
                RaisePropertyChanged("Slot");
            }
        }
        private MemorySlot slot;
        #endregion

        #region FalconValue
        [XmlIgnore]
        public uint? FalconValue
        {
            get { return falconValue; }
            set
            {
                if (falconValue == value)
                    return;
                falconValue = value;
                RaisePropertyChanged("FalconValue");

                OnMemorySlotChanged(this, new MemorySlotChangedEventArgs(falconValue));
                //OutputTarget = FalconValueToServoValue(falconValue);
            }
        }
        private uint? falconValue;
        #endregion

        #region WriteMemorySlot
        [XmlIgnore]
        public RelayCommand WriteMemorySlot { get; private set; }
        
        public void executeWriteMemorySlot(object o)
        {
        //    OutputState = true;
        }
        #endregion

        #region OnMemorySlotChanged
        private void OnMemorySlotChanged(object sender, MemorySlotChangedEventArgs e)
        {
            if (FalconValue != e.falconValue)
            {
                FalconValue = e.falconValue;
                updateStatus();
            }
        }
        #endregion
        
        #region ReadMemorySlot
        [XmlIgnore]
        public RelayCommand ReadMemorySlot { get; private set; }
        
        public void executeReadMemorySlot(object o)
        {
        //    OutputState = true;
        }
        #endregion

        #region MemorySlot
        [XmlIgnore]
        //public uint [] MemorySlots(int Slot)

        //{
        //    get { return memorySlots[Slot]; }
        //    set
        //    {
        //        memorySlot[Slot] = value;
        //        RaisePropertyChanged("MemorySlot");

        //        writeMemorySlot();
        //    }
        //}
        protected static uint[] memorySlots = new uint[64];

        public abstract void updateStatus();
        #endregion 

    }
}

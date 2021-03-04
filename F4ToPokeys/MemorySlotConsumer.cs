using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;

namespace F4ToPokeys
{
    public abstract class MemorySlotConsumer : BindableObject, IDisposable
    {
        Dictionary<uint, uint> _bupPresetFrequencies;

        #region Construction/Destruction
        protected MemorySlotConsumer()
        {
            WriteMemorySlot = new RelayCommand(executeWriteMemorySlot);
            ReadMemorySlot = new RelayCommand(executeReadMemorySlot);

            _bupPresetFrequencies = new Dictionary<uint, uint>();
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

                if (slot.BupUhfFreq)
                    LoadBupFrequencies();

                RaisePropertyChanged("Slot");
            }
        }
        private MemorySlot slot;
        #endregion

        #region BupPresetFrequencies
        private void LoadBupFrequencies()
        {
            _bupPresetFrequencies.Clear();

            if (!LoadViperIni())
            {
                _bupPresetFrequencies.Add(1, 304800);
                _bupPresetFrequencies.Add(2, 273525);
                _bupPresetFrequencies.Add(3, 292300);
                _bupPresetFrequencies.Add(4, 292650);
                _bupPresetFrequencies.Add(5, 279600);
                _bupPresetFrequencies.Add(6, 250600);
                _bupPresetFrequencies.Add(7, 292650);
                _bupPresetFrequencies.Add(8, 292300);
                _bupPresetFrequencies.Add(9, 273525);
                _bupPresetFrequencies.Add(10, 253950);
                _bupPresetFrequencies.Add(11, 353100);
                _bupPresetFrequencies.Add(12, 275800);
                _bupPresetFrequencies.Add(13, 307300);
                _bupPresetFrequencies.Add(14, 339750);
                _bupPresetFrequencies.Add(15, 354000);
                _bupPresetFrequencies.Add(16, 318100);
                _bupPresetFrequencies.Add(17, 359300);
                _bupPresetFrequencies.Add(18, 324500);
                _bupPresetFrequencies.Add(19, 339100);
                _bupPresetFrequencies.Add(20, 280500);
            }
        }

        private bool LoadViperIni()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Viper_ini_path))
            {
                if (File.Exists(Properties.Settings.Default.Viper_ini_path))
                {
                    using (StreamReader reader = new StreamReader(Properties.Settings.Default.Viper_ini_path))
                    {
                        string currentLine = string.Empty;
                        while (!reader.EndOfStream)
                        {
                            currentLine = reader.ReadLine();
                            if (currentLine.ToLower().Contains("[radio]"))
                            {
                                currentLine = reader.ReadLine();
                                for (uint i = 1; i <= 20; i++)
                                {
                                    string[] lineFreq = currentLine.ToLower().Split('=');
                                    if (lineFreq.Length == 2)
                                    {
                                        uint freq = 0;
                                        uint.TryParse(lineFreq[1], out freq);
                                        _bupPresetFrequencies.Add(i, freq);
                                    }
                                    currentLine = reader.ReadLine();
                                }

                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public Dictionary<uint, uint> BupPresetFrequencies { get; }
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
        protected static uint[] memorySlots = new uint[64];

        public abstract void updateStatus();
        #endregion 

    }
}

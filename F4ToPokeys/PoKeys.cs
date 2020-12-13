using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;

namespace F4ToPokeys
{
    public class PoKeys : BindableObject, IDisposable
    {
        #region Construction/Destruction

        public PoKeys()
        {
            RemovePoKeysCommand = new RelayCommand(executeRemovePoKeys);
            AddDigitalOutputCommand = new RelayCommand(executeAddDigitalOutput);
            EnableMemorySlotOutputCommand = new RelayCommand(executeEnableMemorySlotOutput);
            DisableMemorySlotOutputCommand = new RelayCommand(executeDisableMemorySlotOutput);
            AddMatrixLedOutputCommand = new RelayCommand(executeAddMatrixLedOutput);
            AddPoExtBusOutputCommand = new RelayCommand(executeAddPoExtBusOutput);
            AddSevenSegmentDisplayCommand = new RelayCommand(executeAddSevenSegmentDisplay);
        }

        public void Dispose()
        {
            foreach (DigitalOutput digitalOutput in DigitalOutputList)
                digitalOutput.Dispose();

            foreach (MemorySlotOutput memorySlotOutput in MemorySlotOutputList)
                memorySlotOutput.Dispose();

            foreach (MatrixLedOutput matrixLedOutput in MatrixLedOutputList)
                matrixLedOutput.Dispose();

            foreach (PoExtBusOutput poExtBusOutput in PoExtBusOutputList)
                poExtBusOutput.Dispose();

            foreach (SevenSegmentDisplay sevenSegmentDisplay in SevenSegmentDisplayList)
                sevenSegmentDisplay.Dispose();

            PoVID6066.Dispose();

            Disconnect();
        }

        #endregion // Construction/Destruction

        #region AvailablePokeysList
        [XmlIgnore]
        public List<AvailablePoKeys> AvailablePokeysList
        {
            get
            {
                if (availablePokeysList == null)
                {
                    availablePokeysList = PoKeysEnumerator.Singleton.AvailablePoKeysList
                    .OrderBy(ap => ap.PokeysId)
                    .ToList();
                }
                return availablePokeysList;
            }
        }
        private List<AvailablePoKeys> availablePokeysList;

        [XmlIgnore]
        public PoKeysDevice_DLL.PoKeysDevice PokeysDevice { get; } = new PoKeysDevice_DLL.PoKeysDevice();
        #endregion

        #region PokeysConnected
        private bool connected = false;
        [XmlIgnore]
        public bool Connected { get { return connected; } }
        #endregion

        #region SelectedPokeys
        [XmlIgnore]
        public AvailablePoKeys SelectedPokeys
        {
            get { return selectedPokeys; }
            set
            {
                if (selectedPokeys == value)
                    return;

                if (selectedPokeys != null)
                    Disconnect();

                selectedPokeys = value;
                RaisePropertyChanged(nameof(SelectedPokeys));

                Connect();

                updateStatus();
                updateChildrenStatus();

                if (selectedPokeys == null)
                    Serial = null;
                else
                    Serial = selectedPokeys.PokeysSerial;
            }
        }
        private AvailablePoKeys selectedPokeys;
        #endregion

        #region Connect
        private void Connect()
        {
            if (selectedPokeys == null)
            {
                Error = Translations.Main.PokeysNotFoundError;
                return;
            }

            if (selectedPokeys.PokeysIndex == null)
            {
                Error = Translations.Main.PokeysNotFoundError;
                return;
            }

            PokeysDevice.EnumerateDevices();
            if (!PokeysDevice.ConnectToDevice(selectedPokeys.PokeysIndex.Value))
                Error = Translations.Main.PokeysConnectError;

            connected = true;
            RaisePropertyChanged("Connected");

            updateStatus();
        }
        #endregion

        #region Disconnect
        private void Disconnect()
        {
            PokeysDevice.DisconnectDevice();

            connected = false;
            RaisePropertyChanged("Connected");

            selectedPokeys = null;
            updateStatus();
        }
        #endregion

        #region Serial
        public int? Serial
        {
            get { return serial; }
            set
            {
                if (serial == value)
                    return;
                serial = value;
                RaisePropertyChanged(nameof(Serial));

                if (!serial.HasValue)
                {
                    SelectedPokeys = null;
                }
                else
                {
                    AvailablePoKeys availablePoKeys = PoKeysEnumerator.Singleton.AvailablePoKeysList
                        .FirstOrDefault(ap => ap.PokeysSerial == serial);

                    if (availablePoKeys == null)
                    {
                        availablePoKeys = new AvailablePoKeys(serial.Value, string.Empty, null, null);
                        availablePoKeys.Error = Translations.Main.PokeysNotFoundError;
                        PoKeysEnumerator.Singleton.AvailablePoKeysList.Add(availablePoKeys);
                    }

                    SelectedPokeys = availablePoKeys;
                }
            }
        }
        private int? serial;
        #endregion

        #region RemovePoKeysCommand
        [XmlIgnore]
        public RelayCommand RemovePoKeysCommand { get; private set; }

        private void executeRemovePoKeys(object o)
        {
            PoVID6066.DisablePulseEngine();

            Disconnect();

            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemovePoKeysText, SelectedPokeys?.PokeysId),
                Translations.Main.RemovePoKeysCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.PoKeysList.Remove(this);


            Dispose();
        }
        #endregion // RemovePoKeysCommand

        #region DigitalOutputList
        public ObservableCollection<DigitalOutput> DigitalOutputList
        {
            get { return digitalOutputList; }
            set
            {
                digitalOutputList = value;
                RaisePropertyChanged("DigitalOutputList");
            }
        }
        private ObservableCollection<DigitalOutput> digitalOutputList = new ObservableCollection<DigitalOutput>();
        #endregion // DigitalOutputList

        #region AddDigitalOutputCommand
        [XmlIgnore]
        public RelayCommand AddDigitalOutputCommand { get; private set; }

        private void executeAddDigitalOutput(object o)
        {
            DigitalOutput digitalOutput = new DigitalOutput();
            digitalOutput.setOwner(this);
            DigitalOutputList.Add(digitalOutput);
        }
        #endregion // AddDigitalOutputCommand

        //
        // Adding Pokeys Shared Memory Slot Support
        //
        #region MemorySlotList
        [XmlIgnore]
        public ObservableCollection<MemorySlotOutput> MemorySlotOutputList { get; } = new ObservableCollection<MemorySlotOutput>();

        private void InitMemorySlotList()
        {
            MemorySlotOutput memorySlotOutput = new MemorySlotOutput(0);
            memorySlotOutput.Slot = new MemorySlot("LightBits1", flightData => (uint)flightData.lightBits, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(1);
            memorySlotOutput.Slot = new MemorySlot("LightBits2", flightData => (uint)flightData.lightBits2, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(2);
            memorySlotOutput.Slot = new MemorySlot("LightBits3", flightData => (uint)flightData.lightBits3, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(3);
            memorySlotOutput.Slot = new MemorySlot("HsiBits", flightData => (uint)flightData.hsiBits, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(4);
            memorySlotOutput.Slot = new MemorySlot("BupUhfFreq", flightData => (uint)flightData.BupUhfFreq, 0xffffff, 0, "0 - 23", true, "6 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(4);
            memorySlotOutput.Slot = new MemorySlot("BupUhfPreset", flightData => (uint)flightData.BupUhfPreset, 0x1f, 24, "24 - 28", true, "int");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(5);
            memorySlotOutput.Slot = new MemorySlot("AUXTBand", flightData => (uint)(flightData.AuxTacanIsX ? 1 : 0 & (flightData.AUXTChan << 4)), 0x1, 0, " 0 -  3", false, "1 BCD Digit");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(5);
            memorySlotOutput.Slot = new MemorySlot("AUXTChan", flightData => (uint)flightData.AUXTChan, 0xfff, 4, " 4 - 15", true, "3 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(6);
            memorySlotOutput.Slot = new MemorySlot("IFFMode1Digit1", flightData => (uint)flightData.iffBackupMode1Digit1, 0xffff, 0, " 0 - 15", true, "4 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(7);
            memorySlotOutput.Slot = new MemorySlot("IFFMode1Digit2", flightData => (uint)flightData.iffBackupMode1Digit2, 0xffff, 0, " 0 - 15", true, "4 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(8);
            memorySlotOutput.Slot = new MemorySlot("IFFMode3ADigit1", flightData => (uint)flightData.iffBackupMode3ADigit1, 0xffff, 0, " 0 - 15", true, "4 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(9);
            memorySlotOutput.Slot = new MemorySlot("IFFMode3ADigit2", flightData => (uint)flightData.iffBackupMode3ADigit2, 0xffff, 0, " 0 - 15", true, "4 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(10);
            memorySlotOutput.Slot = new MemorySlot("Powerbits", flightData => (uint)flightData.powerBits, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(11);
            memorySlotOutput.Slot = new MemorySlot("Blinkbits", flightData => (uint)flightData.blinkBits, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(12);
            memorySlotOutput.Slot = new MemorySlot("Altbits", flightData => (uint)flightData.altBits, 0xffffffff, 0, " 0 - 31", false, "bool bits");
            addToSlotList(memorySlotOutput);
        }

        private void DeleteMemorySlotList()
        {
            foreach (MemorySlotOutput output in MemorySlotOutputList)
            {
                output.Dispose();
            }

            MemorySlotOutputList.Clear();
        }

        private void addToSlotList(MemorySlotOutput memorySlotOutput)
        {
            MemorySlotOutputList.Add(memorySlotOutput);
        }

        public bool MemorySlotOutputEnabled
        {
            get { return memorySlotOutputEnabled; }
            set
            {
                memorySlotOutputEnabled = value;

                if (memorySlotOutputEnabled)
                {
                    InitMemorySlotList();

                    foreach (MemorySlotOutput memorySlotOutput in MemorySlotOutputList)
                        memorySlotOutput.setOwner(this);
                }
                else
                    DeleteMemorySlotList();

                RaisePropertyChanged("MemorySlotOutputEnabled");
                RaisePropertyChanged("MemorySlotOutputList");
            }
        }

        private bool memorySlotOutputEnabled = false;
        #endregion // MemorySlotOutputList

        #region EnableMemorySlotOutputCommand
        [XmlIgnore]
        public RelayCommand EnableMemorySlotOutputCommand { get; private set; }

        private void executeEnableMemorySlotOutput(object o)
        {
            MemorySlotOutputEnabled = true;

            RaisePropertyChanged("AllowEnableMemorySlotOutput");
            RaisePropertyChanged("AllowDisableMemorySlotOutput");
        }

        [XmlIgnore]
        public bool AllowEnableMemorySlotOutput => !MemorySlotOutputEnabled;
        #endregion // AddMemorySlotOutputCommand

        #region DisableMemorySlotOutputCommand
        [XmlIgnore]
        public RelayCommand DisableMemorySlotOutputCommand { get; private set; }

        private void executeDisableMemorySlotOutput(object o)
        {
            MemorySlotOutputEnabled = false;

            RaisePropertyChanged("AllowEnableMemorySlotOutput");
            RaisePropertyChanged("AllowDisableMemorySlotOutput");
        }

        [XmlIgnore]
        public bool AllowDisableMemorySlotOutput => MemorySlotOutputEnabled;
        #endregion

        #region MatrixLedOutputList
        public ObservableCollection<MatrixLedOutput> MatrixLedOutputList
        {
            get { return matrixLedOutputList; }
            set
            {
                matrixLedOutputList = value;
                RaisePropertyChanged("MatrixLedOutputList");
            }
        }
        private ObservableCollection<MatrixLedOutput> matrixLedOutputList = new ObservableCollection<MatrixLedOutput>();
        #endregion

        #region AddMatrixLedOutputCommand
        [XmlIgnore]
        public RelayCommand AddMatrixLedOutputCommand { get; private set; }

        private void executeAddMatrixLedOutput(object o)
        {
            MatrixLedOutput matrixLedOutput = new MatrixLedOutput();
            matrixLedOutput.setOwner(this);
            MatrixLedOutputList.Add(matrixLedOutput);
        }
        #endregion

        #region PoExtBusOutputList
        public ObservableCollection<PoExtBusOutput> PoExtBusOutputList
        {
            get { return poExtBusOutputList; }
            set
            {
                poExtBusOutputList = value;
                RaisePropertyChanged("PoExtBusOutputList");
            }
        }
        private ObservableCollection<PoExtBusOutput> poExtBusOutputList = new ObservableCollection<PoExtBusOutput>();
        #endregion

        #region AddPoExtBusOutputCommand
        [XmlIgnore]
        public RelayCommand AddPoExtBusOutputCommand { get; private set; }

        private void executeAddPoExtBusOutput(object o)
        {
            PoExtBusOutput poExtBusOutput = new PoExtBusOutput();
            poExtBusOutput.setOwner(this);
            PoExtBusOutputList.Add(poExtBusOutput);
        }
        #endregion

        #region SevenSegmentDisplayList
        public ObservableCollection<SevenSegmentDisplay> SevenSegmentDisplayList
        {
            get { return sevenSegmentDisplayList; }
            set
            {
                sevenSegmentDisplayList = value;
                RaisePropertyChanged("SevenSegmentDisplayList");
            }
        }
        private ObservableCollection<SevenSegmentDisplay> sevenSegmentDisplayList = new ObservableCollection<SevenSegmentDisplay>();
        #endregion

        #region AddSevenSegmentDisplayCommand
        [XmlIgnore]
        public RelayCommand AddSevenSegmentDisplayCommand { get; private set; }

        private void executeAddSevenSegmentDisplay(object o)
        {
            SevenSegmentDisplay sevenSegmentDisplay = new SevenSegmentDisplay();
            sevenSegmentDisplay.setOwner(this);
            SevenSegmentDisplayList.Add(sevenSegmentDisplay);
        }
        #endregion

        #region SevenSegmentMatrixLed1Config
        private SevenSegmentMatrixLedConfig sevenSegmentMatrixLed1Config;

        public SevenSegmentMatrixLedConfig SevenSegmentMatrixLed1Config
        {
            get { return sevenSegmentMatrixLed1Config; }
            set
            {
                if (sevenSegmentMatrixLed1Config == value)
                    return;
                sevenSegmentMatrixLed1Config = value;
                RaisePropertyChanged("SevenSegmentMatrixLed1Config");
            }
        }

        public SevenSegmentMatrixLedConfig GetOrCreateSevenSegmentMatrixLed1Config()
        {
            if (SevenSegmentMatrixLed1Config == null)
                SevenSegmentMatrixLed1Config = new SevenSegmentMatrixLedConfig();
            return SevenSegmentMatrixLed1Config;
        }
        #endregion

        #region SevenSegmentMatrixLed2Config
        private SevenSegmentMatrixLedConfig sevenSegmentMatrixLed2Config;

        public SevenSegmentMatrixLedConfig SevenSegmentMatrixLed2Config
        {
            get { return sevenSegmentMatrixLed2Config; }
            set
            {
                if (sevenSegmentMatrixLed2Config == value)
                    return;
                sevenSegmentMatrixLed2Config = value;
                RaisePropertyChanged("SevenSegmentMatrixLed2Config");
            }
        }

        public SevenSegmentMatrixLedConfig GetOrCreateSevenSegmentMatrixLed2Config()
        {
            if (SevenSegmentMatrixLed2Config == null)
                SevenSegmentMatrixLed2Config = new SevenSegmentMatrixLedConfig();
            return SevenSegmentMatrixLed2Config;
        }
        #endregion

        #region PoKeysVID6066
        public PoKeysStepperVID6066 PoVID6066
        {
            get
            {
                if (poVID6066 == null)
                    poVID6066 = new PoKeysStepperVID6066();

                return poVID6066;
            }
            set
            {
                poVID6066 = value;
            }
        }

        private PoKeysStepperVID6066 poVID6066;
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

        #region owner

        public void setOwner(Configuration configuration)
        {
            owner = configuration;
            updateStatus();

            foreach (DigitalOutput digitalOutput in DigitalOutputList)
                digitalOutput.setOwner(this);

            foreach (MatrixLedOutput matrixLedOutput in MatrixLedOutputList)
                matrixLedOutput.setOwner(this);

            foreach (PoExtBusOutput poExtBusOutput in PoExtBusOutputList)
                poExtBusOutput.setOwner(this);

            foreach (SevenSegmentDisplay sevenSegmentDisplay in SevenSegmentDisplayList)
                sevenSegmentDisplay.setOwner(this);

            if (PoVID6066 != null)
                PoVID6066.setOwner(this);
        }

        private Configuration owner;

        #endregion // owner

        #region PokeysIndex
        [XmlIgnore]
        private int? pokeysIndex { get; set; }
        #endregion // PokeysIndex

        #region updateStatus

        private void updateStatus()
        {
            if (owner == null)
                return;

            if (SelectedPokeys == null)
            {
                Error = null;
                pokeysIndex = null;
            }
            else
            {
                Error = SelectedPokeys.Error;
                pokeysIndex = SelectedPokeys.PokeysIndex;
            }
        }

        private void updateChildrenStatus()
        {
            if (owner == null)
                return;

            foreach (DigitalOutput digitalOutput in DigitalOutputList)
                digitalOutput.updateStatus();

            foreach (MemorySlotOutput memorySlotOutput in MemorySlotOutputList)
                memorySlotOutput.updateStatus();

            foreach (MatrixLedOutput matrixLedOutput in MatrixLedOutputList)
                matrixLedOutput.updateStatus();

            foreach (PoExtBusOutput poExtBusOutput in PoExtBusOutputList)
                poExtBusOutput.updateStatus();

            foreach (SevenSegmentDisplay sevenSegmentDisplay in SevenSegmentDisplayList)
                sevenSegmentDisplay.updateStatus();
        }

        #endregion // updateStatus

        #region PoExtBus
        private PoExtBus poExtBus;

        [XmlIgnore]
        public PoExtBus PoExtBus
        {
            get
            {
                if (poExtBus == null)
                    poExtBus = new PoExtBus();
                return poExtBus;
            }
        }
        #endregion
    }
}

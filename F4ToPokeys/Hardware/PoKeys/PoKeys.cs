using F4SharedMem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class PoKeys : BindableObject, IDevice, IDisposable
    {
        #region Name (user-editable)
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value)
                    return;
                name = value;
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
        private string name;
        #endregion // Name

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
                return selectedPokeys?.PokeysId ?? Translations.Main.PoKeysConfigCaption;
            }
        }

        #region Construction/Destruction

        public PoKeys()
        {
            RemovePoKeysCommand = new RelayCommand(executeRemovePoKeys);
            AddDigitalOutputCommand = new RelayCommand(executeAddDigitalOutput);
            EnableMemorySlotOutputCommand = new RelayCommand(executeEnableMemorySlotOutput);
            DisableMemorySlotOutputCommand = new RelayCommand(executeDisableMemorySlotOutput);
            AddMatrixLedOutputCommand = new RelayCommand(executeAddMatrixLedOutput);
            AddPoExtBusOutputCommand = new RelayCommand(executeAddPoExtBusOutput);
            AllPoExtBusOnCommand = new RelayCommand(executeAllPoExtBusOn, canExecuteAllPoExtBus);
            AllPoExtBusOffCommand = new RelayCommand(executeAllPoExtBusOff, canExecuteAllPoExtBus);
            AllMatrixLedOnCommand = new RelayCommand(executeAllMatrixLedOn, canExecuteAllMatrixLed);
            AllMatrixLedOffCommand = new RelayCommand(executeAllMatrixLedOff, canExecuteAllMatrixLed);
            AllDigitalOutputOnCommand = new RelayCommand(executeAllDigitalOutputOn, canExecuteAllDigitalOutput);
            AllDigitalOutputOffCommand = new RelayCommand(executeAllDigitalOutputOff, canExecuteAllDigitalOutput);
            AddSevenSegmentDisplayCommand = new RelayCommand(executeAddSevenSegmentDisplay);

            // RelayCommand routes CanExecute through CommandManager.RequerySuggested,
            // which doesn't fire on raw collection changes. Nudge it whenever a list
            // gains/loses items so All On/All Off buttons enable on first add and
            // disable when the last item is removed without needing UI focus events.
            PoExtBusOutputList.CollectionChanged += (s, e) => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            MatrixLedOutputList.CollectionChanged += (s, e) => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            DigitalOutputList.CollectionChanged += (s, e) => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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
                RaisePropertyChanged(nameof(DisplayName));

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

            if (selectedPokeys.PokeysInfo == null)
            {
                Error = Translations.Main.PokeysNotFoundError;
                return;
            }

            PokeysDevice.EnumerateDevices();
            if (!PokeysDevice.ConnectToDevice(selectedPokeys.PokeysInfo))
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
            DigitalOutput digitalOutput = new DigitalOutput { Label = Translations.Main.DigitalOutputConfigCaption };
            digitalOutput.setOwner(this);
            DigitalOutputList.Add(digitalOutput);
        }
        #endregion // AddDigitalOutputCommand

        #region AllDigitalOutputOn / Off (testing helper: drive every Digital output high/low at once)
        [XmlIgnore]
        public RelayCommand AllDigitalOutputOnCommand { get; private set; }

        [XmlIgnore]
        public RelayCommand AllDigitalOutputOffCommand { get; private set; }

        private bool canExecuteAllDigitalOutput(object o)
        {
            return DigitalOutputList.Count > 0;
        }

        private void executeAllDigitalOutputOn(object o)
        {
            foreach (DigitalOutput output in DigitalOutputList)
                output.OutputState = true;
        }

        private void executeAllDigitalOutputOff(object o)
        {
            foreach (DigitalOutput output in DigitalOutputList)
                output.OutputState = false;
        }
        #endregion

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

            // Issue with Pokeys and negative numbers. Moved it to it's own int at pos 14
            //memorySlotOutput = new MemorySlotOutput(4);
            //memorySlotOutput.Slot = new MemorySlot("BupUhfPreset", flightData => (uint)flightData.BupUhfPreset, 0x1f, 24, "24 - 28", true, "int");
            //addToSlotList(memorySlotOutput);

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

            memorySlotOutput = new MemorySlotOutput(13);
            memorySlotOutput.Slot = new MemorySlot("PresetUhfFreq", flightData => (uint)flightData.BupUhfPreset, 0xffffff, 0, "0 - 23", true, "6 BCD Digits", true);
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(14);
            memorySlotOutput.Slot = new MemorySlot("BupUhfPresetChannel", flightData => (uint)flightData.BupUhfPreset, 0x1f, 0, "0 - 8", true, "2 BCD Digits");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(15);
            memorySlotOutput.Slot = new MemorySlot("ADIShowCommandBars", flightData => (uint)(FalconConnector.DetermineWhetherToShowILSCommandBars(flightData) ? 1: 0), 0x1, 0, "0", false, "bool");
            addToSlotList(memorySlotOutput);

            memorySlotOutput = new MemorySlotOutput(15);
            memorySlotOutput.Slot = new MemorySlot("ADIShowToFromFlags", flightData => (uint)(FalconConnector.DetermineWhetherToShowILSToFromFlags(flightData) ? 1 : 0), 0x1, 1, "1", false, "bool");
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
            MatrixLedOutput matrixLedOutput = new MatrixLedOutput { Label = Translations.Main.MatrixLedOutputConfigCaption };
            matrixLedOutput.setOwner(this);
            MatrixLedOutputList.Add(matrixLedOutput);
        }
        #endregion

        #region AllMatrixLedOn / Off (testing helper: drive every Matrix LED output high/low at once)
        [XmlIgnore]
        public RelayCommand AllMatrixLedOnCommand { get; private set; }

        [XmlIgnore]
        public RelayCommand AllMatrixLedOffCommand { get; private set; }

        private bool canExecuteAllMatrixLed(object o)
        {
            return MatrixLedOutputList.Count > 0;
        }

        private void executeAllMatrixLedOn(object o)
        {
            foreach (MatrixLedOutput output in MatrixLedOutputList)
                output.OutputState = true;
        }

        private void executeAllMatrixLedOff(object o)
        {
            foreach (MatrixLedOutput output in MatrixLedOutputList)
                output.OutputState = false;
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
            PoExtBusOutput poExtBusOutput = new PoExtBusOutput { Label = Translations.Main.PoExtBusOutputConfigCaption };
            poExtBusOutput.setOwner(this);
            PoExtBusOutputList.Add(poExtBusOutput);
        }
        #endregion

        #region AllPoExtBusOn / Off (testing helper: drive every configured PoExtBus output high/low at once)
        [XmlIgnore]
        public RelayCommand AllPoExtBusOnCommand { get; private set; }

        [XmlIgnore]
        public RelayCommand AllPoExtBusOffCommand { get; private set; }

        private bool canExecuteAllPoExtBus(object o)
        {
            return PoExtBusOutputList.Count > 0;
        }

        private void executeAllPoExtBusOn(object o)
        {
            foreach (PoExtBusOutput output in PoExtBusOutputList)
                output.OutputState = true;
        }

        private void executeAllPoExtBusOff(object o)
        {
            foreach (PoExtBusOutput output in PoExtBusOutputList)
                output.OutputState = false;
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
            {
                if (string.IsNullOrWhiteSpace(digitalOutput.Label))
                    digitalOutput.Label = Translations.Main.DigitalOutputConfigCaption;
                digitalOutput.setOwner(this);
            }

            foreach (MatrixLedOutput matrixLedOutput in MatrixLedOutputList)
            {
                if (string.IsNullOrWhiteSpace(matrixLedOutput.Label))
                    matrixLedOutput.Label = Translations.Main.MatrixLedOutputConfigCaption;
                matrixLedOutput.setOwner(this);
            }

            foreach (PoExtBusOutput poExtBusOutput in PoExtBusOutputList)
            {
                if (string.IsNullOrWhiteSpace(poExtBusOutput.Label))
                    poExtBusOutput.Label = Translations.Main.PoExtBusOutputConfigCaption;
                poExtBusOutput.setOwner(this);
            }

            foreach (SevenSegmentDisplay sevenSegmentDisplay in SevenSegmentDisplayList)
                sevenSegmentDisplay.setOwner(this);

            if (PoVID6066 != null)
                PoVID6066.setOwner(this);
        }

        private Configuration owner;

        #endregion // owner

        #region PokeysInfo
        [XmlIgnore]
        private PoKeysDevice_DLL.PoKeysDeviceInfo pokeysInfo { get; set; }
        #endregion // PokeysInfo

        #region updateStatus

        private void updateStatus()
        {
            if (owner == null)
                return;

            if (SelectedPokeys == null)
            {
                Error = null;
                pokeysInfo = null;
            }
            else
            {
                Error = SelectedPokeys.Error;
                pokeysInfo = SelectedPokeys.PokeysInfo;
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

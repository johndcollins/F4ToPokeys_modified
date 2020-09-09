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
            AddMatrixLedOutputCommand = new RelayCommand(executeAddMatrixLedOutput);
            AddPoExtBusOutputCommand = new RelayCommand(executeAddPoExtBusOutput);
            AddSevenSegmentDisplayCommand = new RelayCommand(executeAddSevenSegmentDisplay);
        }

        public void Dispose()
        {
            foreach (DigitalOutput digitalOutput in DigitalOutputList)
                digitalOutput.Dispose();

            foreach (MatrixLedOutput matrixLedOutput in MatrixLedOutputList)
                matrixLedOutput.Dispose();

            foreach (PoExtBusOutput poExtBusOutput in PoExtBusOutputList)
                poExtBusOutput.Dispose();

            foreach (SevenSegmentDisplay sevenSegmentDisplay in SevenSegmentDisplayList)
                sevenSegmentDisplay.Dispose();
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
                selectedPokeys = value;
                RaisePropertyChanged(nameof(SelectedPokeys));

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
        }

        private Configuration owner;

        #endregion // owner

        #region PokeysIndex
        [XmlIgnore]
        public int? PokeysIndex { get; private set; }
        #endregion // PokeysIndex

        #region updateStatus

        private void updateStatus()
        {
            if (owner == null)
                return;

            if (SelectedPokeys == null)
            {
                Error = null;
                PokeysIndex = null;
            }
            else
            {
                Error = SelectedPokeys.Error;
                PokeysIndex = SelectedPokeys.PokeysIndex;
            }
        }

        private void updateChildrenStatus()
        {
            if (owner == null)
                return;

            foreach (DigitalOutput digitalOutput in DigitalOutputList)
                digitalOutput.updateStatus();

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

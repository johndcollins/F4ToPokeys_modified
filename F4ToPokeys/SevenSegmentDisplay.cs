using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;
using System.Globalization;
using System.ComponentModel;

namespace F4ToPokeys
{
    public class SevenSegmentDisplay : BindableObject, IDisposable
    {
        #region Construction/Destruction
        public SevenSegmentDisplay()
        {
            RemoveSevenSegmentDisplayCommand = new RelayCommand(executeRemoveSevenSegmentDisplay);
            FalconGaugeFormat = new FalconGaugeFormat();
        }

        public void Dispose()
        {
            if (matrixLedConfig != null)
                matrixLedConfig.PropertyChanged -= OnMatrixLedConfigPropertyChanged;

            if (falconGauge != null)
                falconGauge.FalconGaugeChanged -= OnFalconGaugeChanged;
        }
        #endregion

        #region MatrixLed
        [XmlIgnore]
        public MatrixLed MatrixLed
        {
            get { return matrixLed; }
            set
            {
                if (matrixLed == value)
                    return;
                matrixLed = value;
                RaisePropertyChanged("MatrixLed");

                if (matrixLed == null)
                    MatrixLedName = null;
                else
                    MatrixLedName = matrixLed.Name;

                updateStatus();
                updateMatrixLedConfig();
            }
        }
        private MatrixLed matrixLed;
        #endregion

        #region MatrixLedName
        public string MatrixLedName
        {
            get { return matrixLedName; }
            set
            {
                if (matrixLedName == value)
                    return;
                matrixLedName = value;
                RaisePropertyChanged("MatrixLedName");

                if (string.IsNullOrEmpty(matrixLedName))
                    MatrixLed = null;
                else
                    MatrixLed = MatrixLed.AvailableMatrixLedList.FirstOrDefault(item => item.Name == matrixLedName);
            }
        }
        private string matrixLedName;
        #endregion

        #region MatrixLedConfig
        private SevenSegmentMatrixLedConfig matrixLedConfig;

        [XmlIgnore]
        public SevenSegmentMatrixLedConfig MatrixLedConfig
        {
            get { return matrixLedConfig; }
            set
            {
                if (matrixLedConfig == value)
                    return;
                if (matrixLedConfig != null)
                    matrixLedConfig.PropertyChanged -= OnMatrixLedConfigPropertyChanged;
                matrixLedConfig = value;
                if (matrixLedConfig != null)
                    matrixLedConfig.PropertyChanged += OnMatrixLedConfigPropertyChanged;
                RaisePropertyChanged("MatrixLedConfig");

                updateStatus();
            }
        }

        private void OnMatrixLedConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            setOutputStateDirty();
            writeOutputState();
        }

        private void updateMatrixLedConfig()
        {
            if (owner != null && MatrixLed != null)
                MatrixLedConfig = MatrixLed.GetSevenSegmentConfig(owner);
            else
                MatrixLedConfig = null;
        }
        #endregion

        #region FalconGauge
        [XmlIgnore]
        public FalconGauge FalconGauge
        {
            get { return falconGauge; }
            set
            {
                if (falconGauge == value)
                    return;
                if (falconGauge != null)
                    falconGauge.FalconGaugeChanged -= OnFalconGaugeChanged;
                falconGauge = value;
                if (falconGauge != null)
                    falconGauge.FalconGaugeChanged += OnFalconGaugeChanged;
                RaisePropertyChanged("FalconGauge");

                if (falconGauge == null)
                    FalconGaugeLabel = null;
                else
                    FalconGaugeLabel = falconGauge.Label;

                resetFalconValue();
                setDefaultFalconGaugeFormat();
            }
        }
        private FalconGauge falconGauge;
        #endregion

        #region FalconGaugeLabel
        public string FalconGaugeLabel
        {
            get { return falconGaugeLabel; }
            set
            {
                if (falconGaugeLabel == value)
                    return;
                falconGaugeLabel = value;
                RaisePropertyChanged("FalconGaugeLabel");

                if (string.IsNullOrEmpty(falconGaugeLabel))
                    FalconGauge = null;
                else
                    FalconGauge = FalconConnector.Singleton.GaugeList.FirstOrDefault(item => item.Label == falconGaugeLabel);
            }
        }
        private string falconGaugeLabel;
        #endregion

        #region OnFalconGaugeChanged
        private void OnFalconGaugeChanged(object sender, FalconGaugeChangedEventArgs e)
        {
            FalconValue = e.falconValue;
        }
        #endregion

        #region RemoveSevenSegmentDisplayCommand
        [XmlIgnore]
        public RelayCommand RemoveSevenSegmentDisplayCommand { get; private set; }

        private void executeRemoveSevenSegmentDisplay(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveSevenSegmentDisplayText, MatrixLed != null ? MatrixLed.Name : string.Empty),
                Translations.Main.RemoveSevenSegmentDisplayCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.SevenSegmentDisplayList.Remove(this);
            Dispose();
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

        #region FalconValue
        [XmlIgnore]
        public float? FalconValue
        {
            get { return falconValue; }
            set
            {
                if (falconValue == value)
                    return;
                falconValue = value;
                RaisePropertyChanged("FalconValue");

                updateFalconGaugeDigitValues();
            }
        }
        private float? falconValue;
        #endregion

        #region resetFalconValue
        public void resetFalconValue()
        {
            FalconValue = null;
        }
        #endregion

        #region FalconGaugeFormat
        private FalconGaugeFormat falconGaugeFormat;

        public FalconGaugeFormat FalconGaugeFormat
        {
            get { return falconGaugeFormat; }
            set
            {
                if (falconGaugeFormat == value)
                    return;
                if (falconGaugeFormat != null)
                    falconGaugeFormat.PropertyChanged -= OnFalconGaugeFormatPropertyChanged;
                falconGaugeFormat = value;
                if (falconGaugeFormat != null)
                    falconGaugeFormat.PropertyChanged += OnFalconGaugeFormatPropertyChanged;
                RaisePropertyChanged("FalconGaugeFormat");

                updateFalconGaugeDigits();
            }
        }

        private void OnFalconGaugeFormatPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TotalSize")
                updateFalconGaugeDigits();
            else if (e.PropertyName == "Format")
                updateFalconGaugeDigitValues();
        }

        private void setDefaultFalconGaugeFormat()
        {
            if (FalconGauge != null)
            {
                FalconGaugeFormat = new FalconGaugeFormat()
                {
                    TotalSize = FalconGauge.FormatTotalSize,
                    IntegralPartMinSize = FalconGauge.FormatIntegralPartMinSize,
                    FractionalPartSize = FalconGauge.FormatFractionalPartSize,
                    PadFractionalPartWithZero = true,
                };
            }
        }
        #endregion

        #region FalconGaugeDigits
        private readonly ObservableCollection<FalconGaugeDigit> falconGaugeDigits = new ObservableCollection<FalconGaugeDigit>();

        [XmlIgnore]
        public ObservableCollection<FalconGaugeDigit> FalconGaugeDigits
        {
            get { return falconGaugeDigits; }
        }
        #endregion

        #region updateFalconGaugeDigits
        private void updateFalconGaugeDigits()
        {
            if (owner == null)
                return;

            int totalSize = 0;
            if (FalconGaugeFormat != null)
            {
                totalSize = FalconGaugeFormat.TotalSize;
                if (totalSize < 0)
                    totalSize = 0;
            }

            while (FalconGaugeDigits.Count > totalSize)
            {
                FalconGaugeDigits[FalconGaugeDigits.Count - 1].SevenSegmentDigit = null;
                FalconGaugeDigits.RemoveAt(FalconGaugeDigits.Count - 1);
            }

            while (FalconGaugeDigits.Count < totalSize)
                FalconGaugeDigits.Add(new FalconGaugeDigit(this, FalconGaugeDigits.Count));

            foreach (SevenSegmentDigit sevenSegmentDigit in SevenSegmentDigits.Where(digit => digit.Position >= FalconGaugeDigits.Count).ToList())
                RemoveSevenSegmentDigit(sevenSegmentDigit);

            for (int position = 0; position != FalconGaugeDigits.Count; ++position)
                FalconGaugeDigits[position].SevenSegmentDigit = SevenSegmentDigits.FirstOrDefault(digit => digit.Position == position);

            setOutputStateDirty();
            updateFalconGaugeDigitValues();
        }

        private void updateFalconGaugeDigitValues()
        {
            if (owner == null)
                return;

            string outputString = FalconGaugeFormat != null ? FalconGaugeFormat.ToString(FalconValue) : string.Empty;

            using (IEnumerator<char> outputStringEnumerator = outputString.Reverse().GetEnumerator())
            {
                foreach (FalconGaugeDigit digit in FalconGaugeDigits)
                {
                    if (!outputStringEnumerator.MoveNext())
                    {
                        digit.Value = ' ';
                        digit.DecimalPoint = false;
                    }
                    else
                    {
                        if (outputStringEnumerator.Current != '.')
                        {
                            digit.Value = outputStringEnumerator.Current;
                            digit.DecimalPoint = false;
                        }
                        else
                        {
                            digit.DecimalPoint = true;

                            if (!outputStringEnumerator.MoveNext())
                            {
                                digit.Value = ' ';
                            }
                            else
                            {
                                digit.Value = outputStringEnumerator.Current;
                            }
                        }
                    }
                }
            }

            writeOutputState();
        }
        #endregion

        #region SevenSegmentDigits
        private List<SevenSegmentDigit> sevenSegmentDigits = new List<SevenSegmentDigit>();

        public List<SevenSegmentDigit> SevenSegmentDigits
        {
            get { return sevenSegmentDigits; }
            set
            {
                sevenSegmentDigits = value;
                RaisePropertyChanged("SevenSegmentDigits");
            }
        }

        public void AddSevenSegmentDigit(SevenSegmentDigit sevenSegmentDigit)
        {
            List<SevenSegmentDigit> newList = SevenSegmentDigits.ToList();
            newList.Add(sevenSegmentDigit);
            SevenSegmentDigits = newList.OrderBy(digit => digit.Position).ToList();

            setOutputStateDirty();
            writeOutputState();
        }

        public void RemoveSevenSegmentDigit(SevenSegmentDigit sevenSegmentDigit)
        {
            List<SevenSegmentDigit> newList = SevenSegmentDigits.ToList();
            newList.Remove(sevenSegmentDigit);
            SevenSegmentDigits = newList.OrderBy(digit => digit.Position).ToList();
        }
        #endregion

        #region owner
        public void setOwner(PoKeys poKeys)
        {
            owner = poKeys;
            updateStatus();
            updateMatrixLedConfig();
            if (SevenSegmentDigits != null)
                SevenSegmentDigits = SevenSegmentDigits.OrderBy(digit => digit.Position).ToList();
            updateFalconGaugeDigits();
        }

        private PoKeys owner;
        #endregion // owner

        #region updateStatus
        public void updateStatus()
        {
            if (owner == null)
                return;

            if (MatrixLedConfig == null)
            {
                Error = null;
            }
            else if (!owner.PokeysIndex.HasValue)
            {
                Error = null;
            }
            else
            {
                PoKeysDevice_DLL.PoKeysDevice poKeysDevice = PoKeysEnumerator.Singleton.PoKeysDevice;

                if (!poKeysDevice.ConnectToDevice(owner.PokeysIndex.Value))
                {
                    Error = Translations.Main.PokeysConnectError;
                }
                else
                {
                    if (!MatrixLed.IsEnabled())
                    {
                        Error = Translations.Main.MatrixLedErrorNotEnabled;
                    }
                    else
                    {
                        Error = null;
                    }

                    poKeysDevice.DisconnectDevice();
                }
            }

            setOutputStateDirty();
            writeOutputState();
        }
        #endregion // updateStatus

        #region writeOutputState

        private void setOutputStateDirty()
        {
            foreach (SevenSegmentDigit digit in SevenSegmentDigits)
                foreach (SevenSegmentDigitSegment segment in digit.Segments)
                    segment.Dirty = true;
        }

        private void writeOutputState()
        {
            if (string.IsNullOrEmpty(Error) && owner != null && owner.PokeysIndex.HasValue && MatrixLedConfig != null)
            {
                PoKeysDevice_DLL.PoKeysDevice poKeysDevice = PoKeysEnumerator.Singleton.PoKeysDevice;

                if (!poKeysDevice.ConnectToDevice(owner.PokeysIndex.Value))
                {
                    Error = Translations.Main.PokeysConnectError;
                }
                else
                {
                    foreach (SevenSegmentDigit digit in SevenSegmentDigits)
                    {
                        for (int segmentPosition = 0; segmentPosition < 8; ++segmentPosition)
                        {
                            SevenSegmentDigitSegment segment = digit.Segments[segmentPosition];

                            if (segment.Dirty)
                            {
                                bool setPixelOk;

                                if (MatrixLedConfig.DigitOnRow)
                                    setPixelOk = MatrixLed.SetPixel((byte)(digit.Index - 1), (byte)(MatrixLedConfig.SegmentIndexes[segmentPosition] - 1), segment.Value);
                                else
                                    setPixelOk = MatrixLed.SetPixel((byte)(MatrixLedConfig.SegmentIndexes[segmentPosition] - 1), (byte)(digit.Index - 1), segment.Value);

                                if (!setPixelOk)
                                    Error = Translations.Main.MatrixLedErrorWrite;

                                segment.Dirty = false;
                            }
                        }
                    }

                    poKeysDevice.DisconnectDevice();
                }
            }
        }

        #endregion
    }
}

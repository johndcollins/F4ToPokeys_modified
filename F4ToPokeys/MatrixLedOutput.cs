using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;

namespace F4ToPokeys
{
    public class MatrixLedOutput : FalconLightConsumer
    {
        #region Construction/Destruction
        public MatrixLedOutput()
        {
            RemoveMatrixLedOutputCommand = new RelayCommand(executeRemoveMatrixLedOutput);
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

                updateStatus();

                if (matrixLed == null)
                    MatrixLedName = null;
                else
                    MatrixLedName = matrixLed.Name;
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

        #region Row
        public byte? Row
        {
            get { return row; }
            set
            {
                if (row == value)
                    return;
                row = value;
                RaisePropertyChanged("Row");

                updateStatus();
            }
        }
        private byte? row;
        #endregion

        #region Column
        private byte? column;

        public byte? Column
        {
            get { return column; }
            set
            {
                if (column == value)
                    return;
                column = value;
                RaisePropertyChanged("Column");

                updateStatus();
            }
        }
        #endregion

        #region RemoveMatrixLedOutputCommand
        [XmlIgnore]
        public RelayCommand RemoveMatrixLedOutputCommand { get; private set; }

        private void executeRemoveMatrixLedOutput(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveMatrixLedOutputText, MatrixLed != null ? MatrixLed.Name : string.Empty, Row, Column),
                Translations.Main.RemoveMatrixLedOutputCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.MatrixLedOutputList.Remove(this);
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

        #region owner
        public void setOwner(PoKeys poKeys)
        {
            owner = poKeys;
            updateStatus();
        }

        private PoKeys owner;
        #endregion // owner

        #region updateStatus
        public void updateStatus()
        {
            if (owner == null)
                return;

            if (MatrixLed == null || !Row.HasValue || !Column.HasValue)
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
                    if (!MatrixLed.IsPixelEnabled((byte)(Row.Value - 1), (byte)(Column.Value - 1)))
                    {
                        Error = string.Format(Translations.Main.MatrixLedPixelErrorNotEnabled, Row, Column);
                    }
                    else
                    {
                        Error = null;
                    }

                    poKeysDevice.DisconnectDevice();
                }
            }

            writeOutputState();
        }
        #endregion // updateStatus

        #region writeOutputState
        protected override void writeOutputState()
        {
            if (string.IsNullOrEmpty(Error) && owner != null && owner.PokeysIndex.HasValue && MatrixLed != null && Row.HasValue && Column.HasValue)
            {
                PoKeysDevice_DLL.PoKeysDevice poKeysDevice = PoKeysEnumerator.Singleton.PoKeysDevice;

                if (!poKeysDevice.ConnectToDevice(owner.PokeysIndex.Value))
                {
                    Error = Translations.Main.PokeysConnectError;
                }
                else
                {
                    if (!MatrixLed.SetPixel((byte)(Row.Value - 1), (byte)(Column.Value - 1), OutputState))
                    {
                        Error = Translations.Main.MatrixLedErrorWrite;
                    }

                    poKeysDevice.DisconnectDevice();
                }
            }
        }
        #endregion // writeOutputState
    }
}

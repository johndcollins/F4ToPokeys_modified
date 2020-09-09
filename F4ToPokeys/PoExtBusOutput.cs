using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;

namespace F4ToPokeys
{
    public class PoExtBusOutput : FalconLightConsumer
    {
        #region Construction/Destruction
        public PoExtBusOutput()
        {
            RemovePoExtBusOutputCommand = new RelayCommand(executeRemovePoExtBusOutput);
        }
        #endregion

        #region DeviceId
        public int? DeviceId
        {
            get { return deviceId; }
            set
            {
                if (deviceId == value)
                    return;
                deviceId = value;
                RaisePropertyChanged("DeviceId");

                updateStatus();
            }
        }
        private int? deviceId;
        #endregion

        #region PinId
        public char? PinId
        {
            get { return pinId; }
            set
            {
                if (pinId == value)
                    return;
                pinId = value;
                RaisePropertyChanged("PinId");

                updateStatus();
            }
        }
        private char? pinId;
        #endregion

        #region RemovePoExtBusOutputCommand
        [XmlIgnore]
        public RelayCommand RemovePoExtBusOutputCommand { get; private set; }

        private void executeRemovePoExtBusOutput(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemovePoExtBusOutputText, DeviceId, PinId),
                Translations.Main.RemovePoExtBusOutputCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.PoExtBusOutputList.Remove(this);
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

            if (!DeviceId.HasValue || !PinId.HasValue)
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
                    if (!owner.PoExtBus.IsOutputEnabled(DeviceId.Value, PinId.Value))
                    {
                        Error = Translations.Main.PoExtBusErrorNotEnabled;
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
            if (string.IsNullOrEmpty(Error) && owner != null && owner.PokeysIndex.HasValue && DeviceId.HasValue && PinId.HasValue)
            {
                PoKeysDevice_DLL.PoKeysDevice poKeysDevice = PoKeysEnumerator.Singleton.PoKeysDevice;

                if (!poKeysDevice.ConnectToDevice(owner.PokeysIndex.Value))
                {
                    Error = Translations.Main.PokeysConnectError;
                }
                else
                {
                    if (!owner.PoExtBus.SetOutput(DeviceId.Value, PinId.Value, OutputState))
                    {
                        Error = Translations.Main.PoExtBusErrorWrite;
                    }

                    poKeysDevice.DisconnectDevice();
                }
            }
        }
        #endregion // writeOutputState
    }
}

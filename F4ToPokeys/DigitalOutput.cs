using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;

namespace F4ToPokeys
{
    public class DigitalOutput : FalconLightConsumer
    {
        #region Construction/Destruction
        static DigitalOutput()
        {
            pinIdList = new List<byte>();
            for (byte pinId = 1; pinId <= 55; ++pinId)
            {
                pinIdList.Add(pinId);
            }
        }

        public DigitalOutput()
        {
            RemoveDigitalOutputCommand = new RelayCommand(executeRemoveDigitalOutput);
        }
        #endregion // Construction/Destruction

        #region PinIdList
        [XmlIgnore]
        public static List<byte> PinIdList
        {
            get { return pinIdList; }
        }
        private static readonly List<byte> pinIdList;
        #endregion // PinIdList

        #region PinId
        public byte? PinId
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
        private byte? pinId;
        #endregion // PinId

        #region RemoveDigitalOutputCommand
        [XmlIgnore]
        public RelayCommand RemoveDigitalOutputCommand { get; private set; }

        private void executeRemoveDigitalOutput(object o)
        {
            MessageBoxResult result = MessageBox.Show(
                string.Format(Translations.Main.RemoveDigitalOutputText, PinId),
                Translations.Main.RemoveDigitalOutputCaption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
            owner.DigitalOutputList.Remove(this);
            Dispose();
        }
        #endregion // RemoveDigitalOutputCommand

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

            if (!PinId.HasValue)
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
                    byte pinFunction = 0;
                    if (!poKeysDevice.GetPinData((byte)(PinId.Value - 1), ref pinFunction))
                    {
                        Error = Translations.Main.DigitalOutputErrorGetIOType;
                    }
                    else
                    {
                        if ((pinFunction & 0x4) == 0)
                        {
                            Error = Translations.Main.DigitalOutputErrorBadIOType;
                        }
                        else
                        {
                            Error = null;
                        }
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
            if (string.IsNullOrEmpty(Error) && owner != null && owner.PokeysIndex.HasValue && PinId.HasValue)
            {
                PoKeysDevice_DLL.PoKeysDevice poKeysDevice = PoKeysEnumerator.Singleton.PoKeysDevice;

                if (!poKeysDevice.ConnectToDevice(owner.PokeysIndex.Value))
                {
                    Error = Translations.Main.PokeysConnectError;
                }
                else
                {
                    if (!poKeysDevice.SetOutput((byte)(PinId.Value - 1), OutputState))
                    {
                        Error = Translations.Main.DigitalOutputErrorWrite;
                    }

                    poKeysDevice.DisconnectDevice();
                }
            }
        }
        #endregion // writeOutputState
    }
}

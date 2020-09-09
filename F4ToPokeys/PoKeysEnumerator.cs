using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoKeysDevice_DLL;

namespace F4ToPokeys
{
    public class PoKeysEnumerator
    {
        #region Singleton
        public static PoKeysEnumerator Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new PoKeysEnumerator();
                return singleton;
            }
        }
        private static PoKeysEnumerator singleton;
        #endregion // Singleton

        #region Construction/Destruction

        public PoKeysEnumerator()
        {
            availablePoKeysList = new List<AvailablePoKeys>();
            refreshAvailablePoKeysList();
        }

        private void refreshAvailablePoKeysList()
        {
            AvailablePoKeysList.Clear();

            int nbPokeys = PoKeysDevice.EnumerateDevices();

            for (int pokeysIndex = 0; pokeysIndex < nbPokeys; ++pokeysIndex)
            {
                if (PoKeysDevice.ConnectToDevice(pokeysIndex))
                {
                    int pokeysSerial = PoKeysDevice.GetDeviceSerialNumber();
                    string pokeysName = PoKeysDevice.GetDeviceName();

                    byte pokeysUserId = 0;
                    bool pokeysUserIdOk = false;
                    if (PoKeysDevice.GetUserID(ref pokeysUserId))
                        pokeysUserIdOk = true;

                    AvailablePoKeys availablePoKeys = new AvailablePoKeys(pokeysSerial, pokeysName, pokeysUserIdOk ? pokeysUserId : (byte?)null, pokeysIndex);
                    AvailablePoKeysList.Add(availablePoKeys);

                    PoKeysDevice.DisconnectDevice();
                }
            }
        }

        #endregion // Construction/Destruction

        #region AvailablePoKeysList
        public List<AvailablePoKeys> AvailablePoKeysList
        {
            get { return availablePoKeysList; }
        }
        private readonly List<AvailablePoKeys> availablePoKeysList;
        #endregion // AvailablePoKeysList

        #region PoKeysDevice
        public readonly PoKeysDevice PoKeysDevice = new PoKeysDevice();
        #endregion // PoKeysDevice
    }

    #region AvailablePoKeys
    public class AvailablePoKeys : BindableObject
    {
        #region Construction/Destruction
        public AvailablePoKeys(int pokeysSerial, string pokeysName, byte? pokeysUserId, int? pokeysIndex)
        {
            PokeysSerial = pokeysSerial;
            PokeysName = pokeysName;
            PokeysUserId = pokeysUserId;
            PokeysIndex = pokeysIndex;
        }
        #endregion // Construction/Destruction

        #region PokeysSerial
        public int PokeysSerial { get; private set; }
        #endregion // Pokeys Device Serial Number

        #region PokeysName
        public string PokeysName { get; private set; }
        #endregion // Pokeys Device Name

        #region PokeysUserId
        public byte? PokeysUserId { get; private set; }
        #endregion // PokeysUserId

        #region PokeysIndex
        public int? PokeysIndex { get; private set; }
        #endregion // PokeysIndex

        #region PokeysId
        public string PokeysId { get { return PokeysName + " - " + PokeysUserId + " (" + PokeysSerial + ")"; } }
        #endregion // Pokeys ID 

        #region Error
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
    }
    #endregion // AvailablePoKeys
}

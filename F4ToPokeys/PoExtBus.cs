using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F4ToPokeys
{
    public class PoExtBus
    {
        static PoExtBus()
        {
            availableDeviceIdList = Enumerable.Range(1, 10).ToList();
            availablePinIdList = Enumerable.Range(0, 8).Select(i => (char)('A' + i)).ToList();
        }

        #region AvailableDeviceIdList
        private static readonly List<int> availableDeviceIdList;

        public static List<int> AvailableDeviceIdList
        {
            get { return availableDeviceIdList; }
        }
        #endregion

        #region AvailablePinIdList
        private static readonly List<char> availablePinIdList;

        public static List<char> AvailablePinIdList
        {
            get { return availablePinIdList; }
        }
        #endregion

        public bool IsEnabled()
        {
            byte auxilaryBusEnabled = 0;
            if (!PoKeysEnumerator.Singleton.PoKeysDevice.AuxilaryBusGetData(ref auxilaryBusEnabled))
                return false;

            return auxilaryBusEnabled == 1;
        }

        public bool IsOutputEnabled(int deviceId, char pinId)
        {
            if (deviceId < 1 || deviceId > 10)
                return false;

            if (pinId < 'A' || pinId > 'H')
                return false;

            return IsEnabled();
        }

        public bool SetOutput(int deviceId, char pinId, bool outputState)
        {
            int pinMask = 1 << ('H' - pinId);

            if (outputState)
                dataBytes[10 - deviceId] |= (byte)pinMask;
            else
                dataBytes[10 - deviceId] &= (byte)~pinMask;

            return PoKeysEnumerator.Singleton.PoKeysDevice.AuxilaryBusSetData(1, dataBytes);
        }

        private byte[] dataBytes = new byte[10];
    }
}

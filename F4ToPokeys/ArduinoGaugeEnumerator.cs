using System.Collections.Generic;
using System.Windows.Documents;

namespace F4ToPokeys
{
    public class ArduinoGaugeEnumerator
    {
        #region Singleton
        private static ArduinoGaugeEnumerator singleton;
        public static ArduinoGaugeEnumerator Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new ArduinoGaugeEnumerator(); ;
                return singleton;
            }
        }
        #endregion

        public List<ArduinoGaugeDevice> AvailableArduinoGaugeDeviceList { get; }

        public ArduinoGaugeEnumerator()
        {
            AvailableArduinoGaugeDeviceList = new List<ArduinoGaugeDevice>();
            RefreshAvailableArduinoGaugeDeviceList();
        }

        private void RefreshAvailableArduinoGaugeDeviceList()
        {
            AvailableArduinoGaugeDeviceList.Clear();
            var items = ArduinoGaugeDriver.GetConnectedDevices();
            AvailableArduinoGaugeDeviceList.AddRange(items);
        }
    }
}
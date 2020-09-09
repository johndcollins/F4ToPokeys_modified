using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pololu.UsbWrapper;
using Pololu.Usc;

namespace F4ToPokeys
{
    public class PololuMaestroEnumerator
    {
        #region Singleton
        public static PololuMaestroEnumerator Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new PololuMaestroEnumerator();
                return singleton;
            }
        }
        private static PololuMaestroEnumerator singleton;
        #endregion

        #region Construction
        public PololuMaestroEnumerator()
        {
            availablePololuMaestroList = new List<DeviceListItem>();
            refreshAvailablePololuMaestroList();
        }
        #endregion

        #region AvailablePololuMaestroList
        public List<DeviceListItem> AvailablePololuMaestroList
        {
            get { return availablePololuMaestroList; }
        }

        private readonly List<DeviceListItem> availablePololuMaestroList;

        private void refreshAvailablePololuMaestroList()
        {
            AvailablePololuMaestroList.Clear();
            AvailablePololuMaestroList.AddRange(Usc.getConnectedDevices());
        }
        #endregion
    }
}

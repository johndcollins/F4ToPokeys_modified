using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F4ToPokeys
{
    public class MatrixLed
    {
        #region enum Ids
        public enum Ids
        {
            MatrixLed1,
            MatrixLed2
        }
        #endregion

        #region AvailableMatrixLedList
        private static readonly List<MatrixLed> availableMatrixLedList;

        public static List<MatrixLed> AvailableMatrixLedList
        {
            get { return availableMatrixLedList; }
        }
        #endregion

        #region AvailableIndexList
        private static readonly List<byte> availableIndexList;

        public static List<byte> AvailableIndexList
        {
            get { return availableIndexList; }
        }
        #endregion

        #region Construction
        static MatrixLed()
        {
            availableMatrixLedList = new List<MatrixLed>()
            {
                new MatrixLed(MatrixLed.Ids.MatrixLed1, "9-10-11"),
                new MatrixLed(MatrixLed.Ids.MatrixLed2, "23-24-25"),
            };

            availableIndexList = Enumerable.Range(1, 8).Select(i => (byte)i).ToList();
        }

        public MatrixLed(Ids id, string name)
        {
            Id = id;
            Name = name;
        }

        public Ids Id { get; private set; }
        public string Name { get; private set; }
        #endregion

        #region IsEnabled
        public bool IsEnabled()
        {
            return IsEnabled(null, null);
        }

        public bool IsPixelEnabled(byte row, byte column)
        {
            return IsEnabled(row, column);
        }

        private bool IsEnabled(byte? row, byte? column)
        {
            bool enabled1 = false;
            byte rows1 = 0;
            byte cols1 = 0;
            bool enabled2 = false;
            byte rows2 = 0;
            byte cols2 = 0;
            if (!PoKeysEnumerator.Singleton.PoKeysDevice.MatrixLEDGetSettings(ref enabled1, ref rows1, ref cols1, ref enabled2, ref rows2, ref cols2))
                return false;

            switch (Id)
            {
                case Ids.MatrixLed1: return enabled1 && (!row.HasValue || row.Value <= rows1) && (!column.HasValue || column.Value <= cols1);
                case Ids.MatrixLed2: return enabled2 && (!row.HasValue || row.Value <= rows2) && (!column.HasValue || column.Value <= cols2);
                default: return false;
            }
        }
        #endregion

        #region SetPixel
        public bool ClearAll(bool invert)
        {
            switch (Id)
            {
                case Ids.MatrixLed1: return PoKeysEnumerator.Singleton.PoKeysDevice.MatrixLED1ClearAll(invert);
                case Ids.MatrixLed2: return PoKeysEnumerator.Singleton.PoKeysDevice.MatrixLED2ClearAll(invert);
                default: return false;
            }
        }

        public bool SetPixel(byte row, byte column, bool value)
        {
            switch (Id)
            {
                case Ids.MatrixLed1: return PoKeysEnumerator.Singleton.PoKeysDevice.MatrixLED1SetPixel(row, column, value);
                case Ids.MatrixLed2: return PoKeysEnumerator.Singleton.PoKeysDevice.MatrixLED2SetPixel(row, column, value);
                default: return false;
            }
        }
        #endregion

        #region GetSevenSegmentConfig
        public SevenSegmentMatrixLedConfig GetSevenSegmentConfig(PoKeys poKeys)
        {
            switch (Id)
            {
                case Ids.MatrixLed1: return poKeys.GetOrCreateSevenSegmentMatrixLed1Config();
                case Ids.MatrixLed2: return poKeys.GetOrCreateSevenSegmentMatrixLed2Config();
                default: return null;
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F4ToPokeys
{
    public class FalconGaugeDigit : BindableObject
    {
        #region Construction/Destruction
        static FalconGaugeDigit()
        {
            availableIndexList = new List<string>();
            availableIndexList.Add(string.Empty);
            availableIndexList.AddRange(Enumerable.Range(1, 8).Select(i => i.ToString()));
        }

        public FalconGaugeDigit(SevenSegmentDisplay sevenSegmentDisplay, int position)
        {
            this.sevenSegmentDisplay = sevenSegmentDisplay;
            this.position = position;
        }

        private readonly SevenSegmentDisplay sevenSegmentDisplay;
        private readonly int position;
        #endregion

        #region Value
        private char myValue;

        public char Value
        {
            get { return myValue; }
            set
            {
                if (myValue == value)
                    return;
                myValue = value;
                RaisePropertyChanged("Value");

                if (SevenSegmentDigit != null)
                    SevenSegmentDigit.Value = value;
            }
        }
        #endregion

        #region DecimalPoint
        private bool decimalPoint;

        public bool DecimalPoint
        {
            get { return decimalPoint; }
            set
            {
                if (decimalPoint == value)
                    return;
                decimalPoint = value;
                RaisePropertyChanged("DecimalPoint");

                if (SevenSegmentDigit != null)
                    SevenSegmentDigit.SegmentDP.Value = value;
            }
        }
        #endregion

        #region SevenSegmentDigit
        private SevenSegmentDigit sevenSegmentDigit;

        public SevenSegmentDigit SevenSegmentDigit
        {
            get { return sevenSegmentDigit; }
            set
            {
                if (sevenSegmentDigit == value)
                    return;
                if (sevenSegmentDigit != null)
                    sevenSegmentDisplay.RemoveSevenSegmentDigit(sevenSegmentDigit);
                sevenSegmentDigit = value;
                RaisePropertyChanged("SevenSegmentDigit");

                if (sevenSegmentDigit == null)
                {
                    SevenSegmentDigitIndex = null;
                }
                else
                {
                    SevenSegmentDigitIndex = sevenSegmentDigit.Index;
                    SevenSegmentDigit.Value = Value;
                    SevenSegmentDigit.SegmentDP.Value = DecimalPoint;
                }
            }
        }
        #endregion

        #region SevenSegmentDigitIndex
        private byte? sevenSegmentDigitIndex;

        public byte? SevenSegmentDigitIndex
        {
            get { return sevenSegmentDigitIndex; }
            set
            {
                if (sevenSegmentDigitIndex == value)
                    return;
                sevenSegmentDigitIndex = value;
                RaisePropertyChanged("SevenSegmentDigitIndex");

                if (sevenSegmentDigitIndex.HasValue)
                {
                    removeOtherSevenSegmentDigit(sevenSegmentDigitIndex.Value);

                    if (SevenSegmentDigit == null || SevenSegmentDigit.Index != sevenSegmentDigitIndex.Value)
                    {
                        SevenSegmentDigit = new SevenSegmentDigit() { Index = sevenSegmentDigitIndex.Value, Position = position };
                        sevenSegmentDisplay.AddSevenSegmentDigit(SevenSegmentDigit);
                    }
                }
                else
                {
                    SevenSegmentDigit = null;
                }
            }
        }
        #endregion

        #region removeOtherSevenSegmentDigit
        private void removeOtherSevenSegmentDigit(byte index)
        {
            foreach (FalconGaugeDigit falconGaugeDigit in sevenSegmentDisplay.FalconGaugeDigits.Where(digit => digit != this && digit.SevenSegmentDigitIndex == index))
                falconGaugeDigit.SevenSegmentDigitIndex = null;

            foreach (SevenSegmentDigit oldSevenSegmentDigit in sevenSegmentDisplay.SevenSegmentDigits.Where(digit => digit.Index == index && digit.Position != position).ToList())
                sevenSegmentDisplay.RemoveSevenSegmentDigit(oldSevenSegmentDigit);
        }
        #endregion

        #region AvailableIndexList
        private static readonly List<string> availableIndexList;

        public static List<string> AvailableIndexList
        {
            get { return availableIndexList; }
        }
        #endregion
    }
}

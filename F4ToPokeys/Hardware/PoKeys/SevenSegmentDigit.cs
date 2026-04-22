using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class SevenSegmentDigit : BindableObject
    {
        #region Construction
        public SevenSegmentDigit()
        {
            initializeSegments();
        }
        #endregion

        #region Index
        private byte index;

        public byte Index
        {
            get { return index; }
            set
            {
                if (index == value)
                    return;
                index = value;
                RaisePropertyChanged("Index");
            }
        }
        #endregion

        #region Position
        private int position;

        public int Position
        {
            get { return position; }
            set
            {
                if (position == value)
                    return;
                position = value;
                RaisePropertyChanged("Position");
            }
        }
        #endregion

        #region Value
        private char myValue;

        [XmlIgnore]
        public char Value
        {
            get { return myValue; }
            set
            {
                if (myValue == value)
                    return;
                myValue = value;
                RaisePropertyChanged("Value");

                updateSegments();
            }
        }
        #endregion

        #region SegmentA
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentA
        {
            get { return segments[0]; }
        }
        #endregion

        #region SegmentB
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentB
        {
            get { return segments[1]; }
        }
        #endregion

        #region SegmentC
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentC
        {
            get { return segments[2]; }
        }
        #endregion

        #region SegmentD
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentD
        {
            get { return segments[3]; }
        }
        #endregion

        #region SegmentE
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentE
        {
            get { return segments[4]; }
        }
        #endregion

        #region SegmentF
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentF
        {
            get { return segments[5]; }
        }
        #endregion

        #region SegmentG
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentG
        {
            get { return segments[6]; }
        }
        #endregion

        #region SegmentDP
        [XmlIgnore]
        public SevenSegmentDigitSegment SegmentDP
        {
            get { return segments[7]; }
        }
        #endregion

        #region Segments
        private readonly SevenSegmentDigitSegment[] segments = new SevenSegmentDigitSegment[8];

        [XmlIgnore]
        public SevenSegmentDigitSegment[] Segments
        {
            get { return segments; }
        }

        private void initializeSegments()
        {
            foreach (int segmentIndex in Enumerable.Range(0, 8))
                segments[segmentIndex] = new SevenSegmentDigitSegment();
        }
        #endregion

        #region updateSegments
        private void updateSegments()
        {
            switch (Value)
            {
                case '0':
                    SegmentA.Value = true;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = true;
                    SegmentE.Value = true;
                    SegmentF.Value = true;
                    SegmentG.Value = false;
                    break;

                case '1':
                    SegmentA.Value = false;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = false;
                    SegmentE.Value = false;
                    SegmentF.Value = false;
                    SegmentG.Value = false;
                    break;

                case '2':
                    SegmentA.Value = true;
                    SegmentB.Value = true;
                    SegmentC.Value = false;
                    SegmentD.Value = true;
                    SegmentE.Value = true;
                    SegmentF.Value = false;
                    SegmentG.Value = true;
                    break;

                case '3':
                    SegmentA.Value = true;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = true;
                    SegmentE.Value = false;
                    SegmentF.Value = false;
                    SegmentG.Value = true;
                    break;

                case '4':
                    SegmentA.Value = false;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = false;
                    SegmentE.Value = false;
                    SegmentF.Value = true;
                    SegmentG.Value = true;
                    break;

                case '5':
                    SegmentA.Value = true;
                    SegmentB.Value = false;
                    SegmentC.Value = true;
                    SegmentD.Value = true;
                    SegmentE.Value = false;
                    SegmentF.Value = true;
                    SegmentG.Value = true;
                    break;

                case '6':
                    SegmentA.Value = true;
                    SegmentB.Value = false;
                    SegmentC.Value = true;
                    SegmentD.Value = true;
                    SegmentE.Value = true;
                    SegmentF.Value = true;
                    SegmentG.Value = true;
                    break;

                case '7':
                    SegmentA.Value = true;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = false;
                    SegmentE.Value = false;
                    SegmentF.Value = false;
                    SegmentG.Value = false;
                    break;

                case '8':
                    SegmentA.Value = true;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = true;
                    SegmentE.Value = true;
                    SegmentF.Value = true;
                    SegmentG.Value = true;
                    break;

                case '9':
                    SegmentA.Value = true;
                    SegmentB.Value = true;
                    SegmentC.Value = true;
                    SegmentD.Value = true;
                    SegmentE.Value = false;
                    SegmentF.Value = true;
                    SegmentG.Value = true;
                    break;

                case '-':
                    SegmentA.Value = false;
                    SegmentB.Value = false;
                    SegmentC.Value = false;
                    SegmentD.Value = false;
                    SegmentE.Value = false;
                    SegmentF.Value = false;
                    SegmentG.Value = true;
                    break;

                default:
                    SegmentA.Value = false;
                    SegmentB.Value = false;
                    SegmentC.Value = false;
                    SegmentD.Value = false;
                    SegmentE.Value = false;
                    SegmentF.Value = false;
                    SegmentG.Value = false;
                    break;
            }
        }
        #endregion
    }
}

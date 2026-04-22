using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class SevenSegmentMatrixLedConfig : BindableObject
    {
        #region DigitOnRow
        private bool digitOnRow = true;

        public bool DigitOnRow
        {
            get { return digitOnRow; }
            set
            {
                if (digitOnRow == value)
                    return;
                digitOnRow = value;
                RaisePropertyChanged("DigitOnRow");
                RaisePropertyChanged("DigitOnColumn");
            }
        }

        [XmlIgnore]
        public bool DigitOnColumn
        {
            get { return !DigitOnRow; }
            set { DigitOnRow = !value; }
        }
        #endregion

        #region SegmentIndexA
        public byte SegmentIndexA
        {
            get { return segmentIndexes[0]; }
            set
            {
                if (segmentIndexes[0] == value)
                    return;
                segmentIndexes[0] = value;
                RaisePropertyChanged("SegmentIndexA");
            }
        }
        #endregion

        #region SegmentIndexB
        public byte SegmentIndexB
        {
            get { return segmentIndexes[1]; }
            set
            {
                if (segmentIndexes[1] == value)
                    return;
                segmentIndexes[1] = value;
                RaisePropertyChanged("SegmentIndexB");
            }
        }
        #endregion

        #region SegmentIndexC
        public byte SegmentIndexC
        {
            get { return segmentIndexes[2]; }
            set
            {
                if (segmentIndexes[2] == value)
                    return;
                segmentIndexes[2] = value;
                RaisePropertyChanged("SegmentIndexC");
            }
        }
        #endregion

        #region SegmentIndexD
        public byte SegmentIndexD
        {
            get { return segmentIndexes[3]; }
            set
            {
                if (segmentIndexes[3] == value)
                    return;
                segmentIndexes[3] = value;
                RaisePropertyChanged("SegmentIndexD");
            }
        }
        #endregion

        #region SegmentIndexE
        public byte SegmentIndexE
        {
            get { return segmentIndexes[4]; }
            set
            {
                if (segmentIndexes[4] == value)
                    return;
                segmentIndexes[4] = value;
                RaisePropertyChanged("SegmentIndexE");
            }
        }
        #endregion

        #region SegmentIndexF
        public byte SegmentIndexF
        {
            get { return segmentIndexes[5]; }
            set
            {
                if (segmentIndexes[5] == value)
                    return;
                segmentIndexes[5] = value;
                RaisePropertyChanged("SegmentIndexF");
            }
        }
        #endregion

        #region SegmentIndexG
        public byte SegmentIndexG
        {
            get { return segmentIndexes[6]; }
            set
            {
                if (segmentIndexes[6] == value)
                    return;
                segmentIndexes[6] = value;
                RaisePropertyChanged("SegmentIndexG");
            }
        }
        #endregion

        #region SegmentIndexDP
        public byte SegmentIndexDP
        {
            get { return segmentIndexes[7]; }
            set
            {
                if (segmentIndexes[7] == value)
                    return;
                segmentIndexes[7] = value;
                RaisePropertyChanged("SegmentIndexDP");
            }
        }
        #endregion

        #region SegmentIndexes
        private readonly byte[] segmentIndexes = { 8, 7, 6, 5, 4, 3, 2, 1 };

        [XmlIgnore]
        public byte[] SegmentIndexes
        {
            get { return segmentIndexes; }
        }
        #endregion
    }
}

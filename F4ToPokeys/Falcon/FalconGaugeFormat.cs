using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml.Serialization;

namespace F4ToPokeys
{
    public class FalconGaugeFormat : BindableObject
    {
        #region Construction/Destruction
        public FalconGaugeFormat()
        {
            IncrementTotalSizeCommand = new RelayCommand(executeIncrementTotalSize);
            DecrementTotalSizeCommand = new RelayCommand(executeDecrementTotalSize, canExecuteDecrementTotalSize);

            updateFormat();
        }
        #endregion

        #region TotalSize
        private int totalSize;

        public int TotalSize
        {
            get { return totalSize; }
            set
            {
                if (totalSize == value)
                    return;
                totalSize = value;
                RaisePropertyChanged("TotalSize");
            }
        }
        #endregion

        #region IntegralPartMinSize
        private int integralPartMinSize;

        public int IntegralPartMinSize
        {
            get { return integralPartMinSize; }
            set
            {
                if (integralPartMinSize == value)
                    return;
                integralPartMinSize = value;
                RaisePropertyChanged("IntegralPartMinSize");

                updateFormat();
            }
        }
        #endregion

        #region FractionalPartSize
        private int fractionalPartSize;

        public int FractionalPartSize
        {
            get { return fractionalPartSize; }
            set
            {
                if (fractionalPartSize == value)
                    return;
                fractionalPartSize = value;
                RaisePropertyChanged("FractionalPartSize");

                updateFormat();
            }
        }
        #endregion

        #region PadFractionalPartWithZero
        private bool padFractionalPartWithZero;

        public bool PadFractionalPartWithZero
        {
            get { return padFractionalPartWithZero; }
            set
            {
                if (padFractionalPartWithZero == value)
                    return;
                padFractionalPartWithZero = value;
                RaisePropertyChanged("PadFractionalPartWithZero");

                updateFormat();
            }
        }
        #endregion

        #region IncrementTotalSizeCommand
        [XmlIgnore]
        public RelayCommand IncrementTotalSizeCommand { get; private set; }

        private void executeIncrementTotalSize(object o)
        {
            ++TotalSize;
        }
        #endregion

        #region DecrementTotalSizeCommand
        [XmlIgnore]
        public RelayCommand DecrementTotalSizeCommand { get; private set; }

        private void executeDecrementTotalSize(object o)
        {
            --TotalSize;
        }

        private bool canExecuteDecrementTotalSize(object o)
        {
            return TotalSize > 0;
        }
        #endregion

        #region Format
        private string format;

        [XmlIgnore]
        public string Format
        {
            get { return format; }
            private set
            {
                if (format == value)
                    return;
                format = value;
                RaisePropertyChanged("Format");
            }
        }
        #endregion

        #region updateFormat
        private void updateFormat()
        {
            StringBuilder formatBuilder = new StringBuilder();

            if (IntegralPartMinSize > 0)
                formatBuilder.Append('0', IntegralPartMinSize);
            else
                formatBuilder.Append('0');

            if (FractionalPartSize > 0)
            {
                formatBuilder.Append('.');
                formatBuilder.Append(PadFractionalPartWithZero ? '0' : '#', FractionalPartSize);
            }

            Format = formatBuilder.ToString();
        }
        #endregion

        #region ToString
        public string ToString(float? value)
        {
            if (!value.HasValue)
                return string.Empty;

            return value.Value.ToString(Format, CultureInfo.InvariantCulture);
        }
        #endregion
    }
}

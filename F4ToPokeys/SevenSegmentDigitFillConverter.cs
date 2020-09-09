using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace F4ToPokeys
{
    public class SevenSegmentDigitFillConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Membres
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool? segmentOn = values[0] as bool?;
            Brush fillOn = (Brush)values[1];
            Brush fillOff = (Brush)values[2];
            return segmentOn == true ? fillOn : fillOff;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

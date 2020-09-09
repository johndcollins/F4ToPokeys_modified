using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace F4ToPokeys
{
    public class SevenSegmentDigitIndexConverter : IValueConverter
    {
        #region IValueConverter Membres
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte? index = (byte?)value;
            if (index.HasValue)
                return index.ToString();
            else
                return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (string)value;
            if (text == string.Empty)
                return (byte?)null;
            else
                return byte.Parse(text);
        }
        #endregion
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace ISSS3.Classes {
    public class IsValueLessThanMultiple : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (!(values[0] is double numericValue) || !(values[1] is double numericParameter))
                return false;
            return numericValue <= numericParameter;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
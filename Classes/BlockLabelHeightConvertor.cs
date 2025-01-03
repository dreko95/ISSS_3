using System;
using System.Globalization;
using System.Windows.Data;

namespace ISSS3.Classes {
    public class BlockLabelHeightConvertor : IValueConverter {
        public dynamic Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (double?)value >= LabelBlock.TwoLinesHeightLimit;

        public dynamic ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new Exception("The method or operation is not implemented.");
    }
}
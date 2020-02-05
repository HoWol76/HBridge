using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Shutters
{
    public class BooleanToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string))
            {
                return null;
            }

            string doublesString = (string)parameter;
            var doublesStrings = doublesString.Split('|');
            if (doublesStrings.Count() != 2)
            {
                return null;
            }
            if (!double.TryParse(doublesStrings[0], out double trueValue))
            {
                return null;
            }
            if (!double.TryParse(doublesStrings[1], out double falseValue))
            {
                return null;
            }
            bool flag = (bool)value;
            if (flag)
            {
                return trueValue;
            }
            return falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

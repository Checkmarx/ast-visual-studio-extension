using System;
using System.Globalization;
using System.Windows.Data;

namespace ast_visual_studio_extension.CxExtension
{
    /// <summary>
    /// This class is used to calculate values for CxWindowControl.xaml file
    /// </summary>
    internal class XAMLCalculations : IValueConverter
    {
        #region IValueConverter Members

        // Calculate the scroll bar height in the ResultInfoPanel for description section when the extension window is resized
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            int gridHeigh = System.Convert.ToInt32(value);

            return gridHeigh - 212;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

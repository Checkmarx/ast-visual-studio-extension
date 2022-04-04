using System;
using System.Globalization;
using System.Windows.Data;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    /// <summary>
    /// This class is used to calculate values for CxWindowControl.xaml file
    /// </summary>
    internal class XAMLCalculations : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double gridHeigh = System.Convert.ToDouble(values[0]);
            
            if (gridHeigh == 0) return gridHeigh;
            
            bool triageCommentVisible = System.Convert.ToBoolean(values[1]);

            double offset = triageCommentVisible ? 201 : 146;

            return (double) gridHeigh - offset;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Going back to what you had isn't supported.");
        }

        #endregion
    }
}

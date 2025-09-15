using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace ast_visual_studio_extension
{
    public class ResourceKeyToColorConverter : IValueConverter
    {
        public static bool IsDarkTheme => IsDarkThemeMethod();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ThemeResourceKey resourceKey)
            {
                var color = VSColorTheme.GetThemedColor(resourceKey);
                return Color.FromArgb(color.A, color.R, color.G, color.B);
            }

            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static bool IsDarkThemeMethod()
        {
            var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.SystemWindowBrushKey);

            double brightness = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B);

            return brightness < 128;
        }
    }

    public class ThemeManager : INotifyPropertyChanged
    {
        private static readonly Lazy<ThemeManager> _instance = new Lazy<ThemeManager>(() => new ThemeManager());
        public static ThemeManager Instance => _instance.Value;

        private bool _isDarkTheme;

        public event PropertyChangedEventHandler PropertyChanged;

        private ThemeManager()
        {
            _isDarkTheme = ResourceKeyToColorConverter.IsDarkThemeMethod();

            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            private set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkTheme)));
                }
            }
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            IsDarkTheme = ResourceKeyToColorConverter.IsDarkThemeMethod();
        }
    }
}
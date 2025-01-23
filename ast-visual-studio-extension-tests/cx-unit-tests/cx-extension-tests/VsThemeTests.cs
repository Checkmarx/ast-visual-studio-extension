using System.Windows;
using System.Windows.Controls;
using ast_visual_studio_extension;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class VsThemeTests
    {
        [Fact]
        public void SetUseVsTheme_ShouldApplyTheme_WhenValueIsTrue()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var element = new ContentControl();
                VsTheme.SetUseVsTheme(element, true);

                var isUsingTheme = VsTheme.GetUseVsTheme(element);

                Assert.True(isUsingTheme);
                Assert.NotNull(element.Resources);
            });
        }

        [Fact]
        public void GetUseVsTheme_ShouldReturnFalse_WhenElementIsNotThemed()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var element = new ContentControl();

                var isUsingTheme = VsTheme.GetUseVsTheme(element);

                Assert.False(isUsingTheme);
            });
        }

        [Fact]
        public void GetUseVsTheme_ShouldReturnTrue_WhenElementIsThemed()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var element = new ContentControl();
                VsTheme.SetUseVsTheme(element, true);

                var isUsingTheme = VsTheme.GetUseVsTheme(element);

                Assert.True(isUsingTheme);
            });
        }

        [Fact]
        public void UseVsThemePropertyChanged_ShouldApplyTheme_WhenValueIsTrue()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var element = new ContentControl();
                var dependencyObject = (DependencyObject)element;
                var args = new DependencyPropertyChangedEventArgs(VsTheme.UseVsThemeProperty, false, true);

                typeof(VsTheme).GetMethod("UseVsThemePropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, new object[] { dependencyObject, args });

                var isUsingTheme = VsTheme.GetUseVsTheme(element);
                Assert.True(isUsingTheme);
            });
        }

        [Fact]
        public void UseVsThemePropertyChanged_ShouldRemoveTheme_WhenValueIsFalse()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var element = new ContentControl();
                var dependencyObject = (DependencyObject)element;
                var args = new DependencyPropertyChangedEventArgs(VsTheme.UseVsThemeProperty, true, false);

                // Act
                typeof(VsTheme).GetMethod("UseVsThemePropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, new object[] { dependencyObject, args });

                // Assert
                var isUsingTheme = VsTheme.GetUseVsTheme(element);
                Assert.False(isUsingTheme);
            });
        }
    }
}

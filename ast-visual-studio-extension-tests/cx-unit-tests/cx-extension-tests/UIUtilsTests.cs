using System.Collections.Generic;
using System.Windows.Controls;
using Xunit;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxExtension.Enums;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class UIUtilsTests
    {
        [Fact]
        public void CreateTreeViewItemHeader_ShouldReturnTextBlock()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                string severity = Severity.HIGH.ToString();
                string displayName = "Test_Item";

                TextBlock result = UIUtils.CreateTreeViewItemHeader(severity, displayName);

                Assert.NotNull(result);
                Assert.IsType<TextBlock>(result);
                Assert.Contains("Test_Item", result.Tag.ToString());
            });
        }

        [Fact]
        public void CreateTreeViewItemWithItemsSource_ShouldReturnTreeViewItem()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                string headerText = "Header";
                List<TreeViewItem> source = new List<TreeViewItem>
            {
                new TreeViewItem { Header = "Item1" },
                new TreeViewItem { Header = "Item2" }
            };

                TreeViewItem result = UIUtils.CreateTreeViewItemWithItemsSource(headerText, source);

                Assert.NotNull(result);
                Assert.IsType<TreeViewItem>(result);
                Assert.Equal(headerText, result.Tag);
                Assert.Equal(source, result.ItemsSource);
            });
        }

        [Fact]
        public void CreateTextBlock_ShouldReturnTextBlock()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                string message = "Test Message";

                TextBlock result = UIUtils.CreateTextBlock(message);

                Assert.NotNull(result);
                Assert.IsType<TextBlock>(result);
                Assert.Equal(message, result.Text);
            });
        }

        [Fact]
        public void CreateSeverityLabelWithIcon_ShouldReturnStackPanel()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                string severity = Severity.HIGH.ToString();

                StackPanel result = UIUtils.CreateSeverityLabelWithIcon(severity);

                Assert.NotNull(result);
                Assert.IsType<StackPanel>(result);
                Assert.Equal(2, result.Children.Count);
                Assert.IsType<Image>(result.Children[0]);
                Assert.IsType<Label>(result.Children[1]);
            });
        }

        [Fact]
        public void CreateLabelWithImage_ShouldReturnStackPanel()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                string message = "Test Message";
                string icon = CxConstants.ICON_FLAG;

                StackPanel result = UIUtils.CreateLabelWithImage(message, icon);

                Assert.NotNull(result);
                Assert.IsType<StackPanel>(result);
                Assert.Equal(2, result.Children.Count);
                Assert.IsType<Image>(result.Children[0]);
                Assert.IsType<Label>(result.Children[1]);
            });
        }
    }
}

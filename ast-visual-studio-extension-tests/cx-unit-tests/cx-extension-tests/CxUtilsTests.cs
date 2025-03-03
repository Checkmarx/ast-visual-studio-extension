using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Models;

using System.Windows.Controls;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class CxUtilsTests
    {
        [Theory]
        [InlineData("CRITICAL", Severity.CRITICAL)]
        [InlineData("HIGH", Severity.HIGH)]
        [InlineData("MEDIUM", Severity.MEDIUM)]
        [InlineData("LOW", Severity.LOW)]
        [InlineData("INFO", Severity.INFO)]
        public void GetSeverityFromString_ShouldReturnCorrectSeverity(string severity, Severity expectedSeverity)
        {
            var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.GetSeverityFromString(severity);

            Assert.Equal(expectedSeverity, result);
        }


        [Theory]
        [InlineData("Project2", ComboboxType.PROJECTS, new[] { "Project1", "Project2" }, 1)]
        [InlineData("Scan2", ComboboxType.SCANS, new[] { "Scan1", "Scan2" }, 1)]
        [InlineData("NonExistent", ComboboxType.PROJECTS, new[] { "Project1", "Project2" }, -1)]
        public void GetItemIndexInCombo_ShouldReturnCorrectIndexForTag(string searchValue, ComboboxType comboType, string[] items, int expectedIndex)
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var comboBox = new ComboBox();

                foreach (var item in items)
                {
                    object tag = null;

                    if (comboType == ComboboxType.PROJECTS)
                        tag = new Project { Id = item };
                    else if (comboType == ComboboxType.SCANS)
                        tag = new Scan { ID = item };

                    var comboBoxItem = new ComboBoxItem { Tag = tag };
                    comboBox.Items.Add(comboBoxItem);
                }

                var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.GetItemIndexInCombo(searchValue, comboBox, comboType);

                Assert.Equal(expectedIndex, result);
            });
        }


        [Theory]
        [InlineData("Branch2", ComboboxType.BRANCHES, new[] { "Branch1", "Branch2" }, 1)]
        [InlineData("HIGH", ComboboxType.SEVERITY, new[] { "LOW", "HIGH" }, 1)]
        [InlineData("CONFIRMED", ComboboxType.STATE, new[] { "TO_VERIFY", "CONFIRMED" }, 1)]
        [InlineData("NonExistent", ComboboxType.BRANCHES, new[] { "Branch1", "Branch2" }, -1)]
        public void GetItemIndexInCombo_ShouldReturnCorrectIndexForContent(string searchValue, ComboboxType comboType, string[] items, int expectedIndex)
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var comboBox = new ComboBox();
                foreach (var item in items)
                {
                    comboBox.Items.Add(new ComboBoxItem { Content = item });
                }

                var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.GetItemIndexInCombo(searchValue, comboBox, comboType);

                Assert.Equal(expectedIndex, result);
            });
        }

        [Theory]
        [InlineData("ThisIsAVeryLongFileNameThatExceedsTheMaximumAllowedLength.txt", "...NameThatExceedsTheMaximumAllowedLength.txt")]
        [InlineData("ShortFileName.txt", "ShortFileName.txt")]
        public void CapToLen_ShouldHandleFileNameProperly(string fileName, string expected)
        {
            var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.CapToLen(fileName);

            Assert.Equal(expected, result);
        }



        [Theory]
        [InlineData("urgent", "Urgent")]
        [InlineData("To_Verify", "To Verify")]
        [InlineData("confirmed", "Confirmed")]
        [InlineData("proposed", "Proposed")]
        [InlineData("Not_Exploitable", "Not Exploitable")]
        [InlineData("notIgnored", "Notignored")]
        [InlineData("Not_Ignored", "Not Ignored")]
        [InlineData("ignored", "Ignored")]
        [InlineData("in_review", "In Review")]

        public void FormatStateName_ShouldReturnFormattedName(string input, string expected)
        {
            string result = ast_visual_studio_extension.CxExtension.Utils.UIUtils.FormatStateName(input);
            Assert.Equal(expected, result);
        }
    }
}

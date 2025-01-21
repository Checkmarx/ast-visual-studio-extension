using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        //[Fact]
        //public void GetCxWrapper_ShouldReturnWrapper()
        //{
        //    // Arrange
        //    var packageMock = new Mock<AsyncPackage>();
        //    var preferencesMock = new Mock<CxPreferencesModule>();
        //    var config = new CxConfig { ApiKey = "test-api-key" };
        //    preferencesMock.Setup(p => p.GetCxConfig()).Returns(config);
        //    packageMock.Setup(p => p.GetDialogPage(typeof(CxPreferencesModule))).Returns(preferencesMock.Object);
        //    var resultsTree = new TreeView();

        //    // Act
        //    var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.GetCxWrapper(packageMock.Object, resultsTree, typeof(CxWrapper));

        //    // Assert
        //    Assert.NotNull(result);
        //}

        //[Fact]
        //public void GetCxWrapper_ShouldHandleException()
        //{
        //    var staThread = new Thread(() =>
        //    {
        //        // Arrange
        //        var packageMock = new Mock<AsyncPackage>();
        //        var resultsTree = new TreeView();//The calling thread must be STA, because many UI components require this

        //        // Act
        //        var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.GetCxWrapper(packageMock.Object, resultsTree, typeof(CxWrapper));

        //        // Assert
        //        Assert.Null(result);
        //        Assert.Single(resultsTree.Items);
        //    });
        //    staThread.SetApartmentState(ApartmentState.STA); // Set to STA mode
        //    staThread.Start();
        //    staThread.Join();
        //}

        [Theory]
        [InlineData("Project2", ComboboxType.PROJECTS, new[] { "Project1", "Project2" }, 1)]
        [InlineData("Scan2", ComboboxType.SCANS, new[] { "Scan1", "Scan2" }, 1)]
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


        //[Fact]
        //public void DisplayMessageInInfoBar_ShouldCallShowInfoBarAsync()
        //{
        //    // Arrange
        //    var packageMock = new Mock<AsyncPackage>();
        //    var message = "Test message";
        //    var severity = new ImageMoniker();

        //    // Act
        //    ast_visual_studio_extension.CxExtension.Utils.CxUtils.DisplayMessageInInfoBar(packageMock.Object, message, severity);

        //    // Assert
        //    // Verify that ShowInfoBarAsync was called
        //}

        //[Fact]
        //public void DisplayMessageInInfoWithLinkBar_ShouldCallShowInfoBarWithLinkAsync()
        //{
        //    // Arrange
        //    var packageMock = new Mock<AsyncPackage>();
        //    var message = "Test message";
        //    var severity = new ImageMoniker();
        //    var linkDisplayName = "Link";
        //    var linkId = "http://example.com";

        //    // Act
        //    ast_visual_studio_extension.CxExtension.Utils.CxUtils.DisplayMessageInInfoWithLinkBar(packageMock.Object, message, severity, linkDisplayName, linkId);

        //    // Assert
        //    // Verify that ShowInfoBarWithLinkAsync was called
        //}

        //[Fact]
        //public void AreCxCredentialsDefined_ShouldReturnTrueWhenCredentialsAreDefined()
        //{
        //    // Arrange
        //    var packageMock = new Mock<AsyncPackage>();
        //    var preferencesMock = new Mock<CxPreferencesModule>();
        //    var config = new CxConfig { ApiKey = "test-api-key" };
        //    preferencesMock.Setup(p => p.GetCxConfig()).Returns(config);
        //    packageMock.Setup(p => p.GetDialogPage(typeof(CxPreferencesModule))).Returns(preferencesMock.Object);

        //    // Act
        //    var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.AreCxCredentialsDefined(packageMock.Object);

        //    // Assert
        //    Assert.True(result);
        //}

        //[Fact]
        //public void AreCxCredentialsDefined_ShouldReturnFalseWhenCredentialsAreNotDefined()
        //{
        //    // Arrange
        //    var packageMock = new Mock<AsyncPackage>();
        //    var preferencesMock = new Mock<CxPreferencesModule>();
        //    var config = new CxConfig { ApiKey = "" };
        //    preferencesMock.Setup(p => p.GetCxConfig()).Returns(config);
        //    packageMock.Setup(p => p.GetDialogPage(typeof(CxPreferencesModule))).Returns(preferencesMock.Object);

        //    // Act
        //    var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.AreCxCredentialsDefined(packageMock.Object);

        //    // Assert
        //    Assert.False(result);
        //}

        [Theory]
        [InlineData("ThisIsAVeryLongFileNameThatExceedsTheMaximumAllowedLength.txt", "...NameThatExceedsTheMaximumAllowedLength.txt")]
        [InlineData("ShortFileName.txt", "ShortFileName.txt")]
        public void CapToLen_ShouldHandleFileNameProperly(string fileName, string expected)
        {
            var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.CapToLen(fileName);

            Assert.Equal(expected, result);
        }
    }
}

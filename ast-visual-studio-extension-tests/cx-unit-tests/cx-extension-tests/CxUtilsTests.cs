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

        [Fact]
        public void GetItemIndexInCombo_ShouldReturnCorrectIndex()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var comboBox = new ComboBox();
                var item1 = new ComboBoxItem { Content = "Item 1" };
                var item2 = new ComboBoxItem { Content = "Item 2" };
                comboBox.Items.Add(item1);
                comboBox.Items.Add(item2);

                var result = ast_visual_studio_extension.CxExtension.Utils.CxUtils.GetItemIndexInCombo("Item 2", comboBox, ComboboxType.STATE);

                Assert.Equal(1, result);
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

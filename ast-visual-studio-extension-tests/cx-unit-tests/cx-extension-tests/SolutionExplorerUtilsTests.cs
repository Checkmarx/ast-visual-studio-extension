using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxExtension.Utils;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class SolutionExplorerUtilsTests
    {
        #region PrepareFileName Method Tests
        [Fact]
        public void PrepareFileName_ShouldFormatCorrectly_WhenStartingWithSlash()
        {
            // Arrange: Provide input with starting slash
            string input = "/folder/file.cs";

            // Act: Call PrepareFileName method
            var result = SolutionExplorerUtils.PrepareFileName(input);

            // Assert: Ensure the file path is formatted correctly
            Assert.Equal("folder\\file.cs", result);
        }

        [Fact]
        public void PrepareFileName_ShouldFormatCorrectly_WhenNoStartingSlash()
        {
            // Arrange: Provide input without starting slash
            string input = "folder/file.cs";

            // Act: Call PrepareFileName method
            var result = SolutionExplorerUtils.PrepareFileName(input);

            // Assert: Ensure the file path is formatted correctly
            Assert.Equal("folder\\file.cs", result);
        }

        [Fact]
        public void PrepareFileName_ShouldHandleEmptyString()
        {
            // Arrange: Provide empty string input
            string input = "";

            // Act: Call PrepareFileName method
            var result = SolutionExplorerUtils.PrepareFileName(input);

            // Assert: Ensure the result is still empty
            Assert.Equal("", result);
        }

        #endregion
    }
}

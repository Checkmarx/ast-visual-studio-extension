using System;
using System.Collections.Generic;
using ast_visual_studio_extension.CxWrapper.Models;
using ast_visual_studio_extension.CxExtension.Utils;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class ResultUtilsTests
    {
        #region Test for FormatFilenameLine Method

        [Fact]
        public void FormatFilenameLine_ShouldFormatCorrectly_WhenValidInputsProvided()
        {
            // Arrange
            string filename = "/path/to/file1.cs";
            int? line = 42;
            string ruleName = "Rule1";

            // Act
            string result = ResultUtils.FormatFilenameLine(filename, line, ruleName);

            // Assert
            Assert.Equal("Rule1 (/file1.cs:42)", result);
        }

        [Fact]
        public void FormatFilenameLine_ShouldReturnNull_WhenFilenameIsNull()
        {
            // Arrange
            string filename = null;
            int? line = 42;
            string ruleName = "Rule1";

            // Act
            string result = ResultUtils.FormatFilenameLine(filename, line, ruleName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FormatFilenameLine_ShouldReturnNull_WhenLineIsNull()
        {
            // Arrange
            string filename = "/path/to/file1.cs";
            int? line = null;
            string ruleName = "Rule1";

            // Act
            string result = ResultUtils.FormatFilenameLine(filename, line, ruleName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FormatFilenameLine_ShouldReturnNull_WhenRuleNameIsNull()
        {
            // Arrange
            string filename = "/path/to/file1.cs";
            int? line = 42;
            string ruleName = null;

            // Act
            string result = ResultUtils.FormatFilenameLine(filename, line, ruleName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FormatFilenameLine_ShouldReturnNull_WhenAllInputsAreNull()
        {
            // Arrange
            string filename = null;
            int? line = null;
            string ruleName = null;

            // Act
            string result = ResultUtils.FormatFilenameLine(filename, line, ruleName);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Test for HandleFileNameAndLine Method

        [Fact]
        public void HandleFileNameAndLine_ShouldAppendFileAndLine_WhenNodesArePresent()
        {
            // Arrange
            var result = new Result
            {
                Data = new Data
                {
                    Nodes = new List<Node>
                    {
                        new Node { FileName = "/path/to/file1.cs", Line = 42 }
                    }
                }
            };
            string displayName = "Vulnerability1";

            // Act
            string updatedDisplayName = ResultUtils.HandleFileNameAndLine(result, displayName);

            // Assert
            Assert.Equal("Vulnerability1\n (/file1.cs:42)", updatedDisplayName);
        }

        [Fact]
        public void HandleFileNameAndLine_ShouldAppendFile_WhenLineIsZeroOrNegative()
        {
            // Arrange
            var result = new Result
            {
                Data = new Data
                {
                    Nodes = new List<Node>
                    {
                        new Node { FileName = "/path/to/file1.cs", Line = 0 }
                    }
                }
            };
            string displayName = "Vulnerability1";

            // Act
            string updatedDisplayName = ResultUtils.HandleFileNameAndLine(result, displayName);

            // Assert
            Assert.Equal("Vulnerability1\n (/file1.cs)", updatedDisplayName);
        }

        [Fact]
        public void HandleFileNameAndLine_ShouldHandleEmptyFileNameCorrectly()
        {
            // Arrange
            var result = new Result
            {
                Data = new Data
                {
                    Nodes = new List<Node>
                    {
                        new Node { FileName = "", Line = 42 }
                    }
                }
            };
            string displayName = "Vulnerability1";

            // Act
            string updatedDisplayName = ResultUtils.HandleFileNameAndLine(result, displayName);

            // Assert
            Assert.Equal("Vulnerability1\n (:42)", updatedDisplayName);
        }

        [Fact]
        public void HandleFileNameAndLine_ShouldNotModifyDisplayName_WhenNodesIsEmpty()
        {
            // Arrange
            var result = new Result
            {
                Data = new Data
                {
                    Nodes = new List<Node>() // Empty nodes list
                }
            };
            string displayName = "Vulnerability1";

            // Act
            string updatedDisplayName = ResultUtils.HandleFileNameAndLine(result, displayName);

            // Assert
            Assert.Equal("Vulnerability1", updatedDisplayName);
        }

        [Fact]
        public void HandleFileNameAndLine_ShouldNotModifyDisplayName_WhenNodesIsNull()
        {
            // Arrange
            var result = new Result
            {
                Data = new Data
                {
                    Nodes = null // Null nodes
                }
            };
            string displayName = "Vulnerability1";

            // Act
            string updatedDisplayName = ResultUtils.HandleFileNameAndLine(result, displayName);

            // Assert
            Assert.Equal("Vulnerability1", updatedDisplayName);
        }

        #endregion
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for FindingsTreeNode, FileNode, VulnerabilityNode, and SeverityCount (INotifyPropertyChanged, display text).
    /// </summary>
    public class FindingsTreeNodeTests
    {
        #region VulnerabilityNode - PrimaryDisplayText

        [Fact]
        public void PrimaryDisplayText_AscaScanner_ReturnsDescription()
        {
            var node = new VulnerabilityNode { Scanner = ScannerType.ASCA, Description = "SQL Injection found" };
            Assert.Equal("SQL Injection found ", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_OssScanner_ReturnsSeverityRiskPackageFormat()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.OSS,
                Severity = "High",
                Description = "lodash",
                PackageName = "lodash",
                PackageVersion = "4.17.19"
            };
            Assert.Equal("High-risk package: lodash@4.17.19", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_OssScanner_NullVersion_NoAtSign()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.OSS,
                Severity = "Critical",
                Description = "axios"
            };
            Assert.Equal("Critical-risk package: axios", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_OssScanner_StripsCve()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.OSS,
                Severity = "High",
                Description = "lodash (CVE-2021-23337)"
            };
            Assert.Equal("High-risk package: lodash", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_OssScanner_EmptyDescription_UsesPackageName()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.OSS,
                Severity = "Medium",
                Description = "",
                PackageName = "axios",
                PackageVersion = "1.0.0"
            };
            Assert.Equal("Medium-risk package: axios@1.0.0", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_OssScanner_NullDescription_UsesPackageName()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.OSS,
                Severity = "Low",
                PackageName = "pkg",
                PackageVersion = null
            };
            Assert.Equal("Low-risk package: pkg", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_SecretsScanner_ReturnsSeverityRiskSecretFormat()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.Secrets,
                Severity = "Critical",
                Description = "generic-api-key"
            };
            Assert.Equal("Critical-risk secret: generic-api-key", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_ContainersScanner_ReturnsSeverityRiskImageFormat()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.Containers,
                Severity = "Critical",
                Description = "nginx:latest"
            };
            Assert.Equal("Critical-risk container image: nginx:latest", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_IacScanner_ReturnsDescription()
        {
            var node = new VulnerabilityNode { Scanner = ScannerType.IaC, Description = "Healthcheck Not Set" };
            Assert.Equal("Healthcheck Not Set ", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_GroupedByLineMessage_ReturnsRawMessage()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.IaC,
                Description = "4 IAC issues detected on this line"
            };
            Assert.Equal("4 IAC issues detected on this line", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_AscaGroupedByLineMessage_ReturnsRawMessage()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.ASCA,
                Description = "3 ASCA violations detected on this line"
            };
            Assert.Equal("3 ASCA violations detected on this line", node.PrimaryDisplayText);
        }

        #endregion

        #region VulnerabilityNode - SecondaryDisplayText

        [Fact]
        public void SecondaryDisplayText_ContainsLineAndColumn()
        {
            var node = new VulnerabilityNode { Line = 42, Column = 5 };
            Assert.Equal("Checkmarx One Assist [Ln 42, Col 5]", node.SecondaryDisplayText);
        }

        #endregion

        #region VulnerabilityNode - DisplayText

        [Fact]
        public void DisplayText_CombinesPrimaryAndSecondary()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.ASCA,
                Description = "Issue",
                Line = 10,
                Column = 3
            };
            Assert.Contains("Issue", node.DisplayText);
            Assert.Contains("Ln 10", node.DisplayText);
            Assert.Contains("Col 3", node.DisplayText);
        }

        #endregion

        #region VulnerabilityNode - INotifyPropertyChanged

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnDescriptionChange()
        {
            var node = new VulnerabilityNode();
            var changedProperties = new List<string>();
            node.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            node.Description = "new value";

            Assert.Contains("Description", changedProperties);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnSeverityChange()
        {
            var node = new VulnerabilityNode();
            var changedProperties = new List<string>();
            node.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            node.Severity = "High";

            Assert.Contains("Severity", changedProperties);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnLineChange()
        {
            var node = new VulnerabilityNode();
            var changedProperties = new List<string>();
            node.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            node.Line = 42;

            Assert.Contains("Line", changedProperties);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnColumnChange()
        {
            var node = new VulnerabilityNode();
            var changedProperties = new List<string>();
            node.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            node.Column = 5;

            Assert.Contains("Column", changedProperties);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnFilePathChange()
        {
            var node = new VulnerabilityNode();
            var changedProperties = new List<string>();
            node.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            node.FilePath = @"C:\new\path.cs";

            Assert.Contains("FilePath", changedProperties);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnScannerChange()
        {
            var node = new VulnerabilityNode();
            var changedProperties = new List<string>();
            node.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            node.Scanner = ScannerType.IaC;

            Assert.Contains("Scanner", changedProperties);
        }

        #endregion

        #region FileNode

        [Fact]
        public void FileNode_DefaultConstructor_InitializesCollections()
        {
            var fileNode = new FileNode();

            Assert.NotNull(fileNode.SeverityCounts);
            Assert.NotNull(fileNode.Vulnerabilities);
            Assert.Empty(fileNode.SeverityCounts);
            Assert.Empty(fileNode.Vulnerabilities);
        }

        [Fact]
        public void FileNode_PropertyChanged_FiresOnFileNameChange()
        {
            var node = new FileNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.FileName = "test.cs";

            Assert.Contains("FileName", changed);
        }

        [Fact]
        public void FileNode_PropertyChanged_FiresOnFilePathChange()
        {
            var node = new FileNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.FilePath = @"C:\src\test.cs";

            Assert.Contains("FilePath", changed);
        }

        #endregion

        #region SeverityCount

        [Fact]
        public void SeverityCount_PropertyChanged_FiresOnCountChange()
        {
            var sc = new SeverityCount();
            var changed = new List<string>();
            sc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            sc.Count = 5;

            Assert.Contains("Count", changed);
        }

        [Fact]
        public void SeverityCount_PropertyChanged_FiresOnSeverityChange()
        {
            var sc = new SeverityCount();
            var changed = new List<string>();
            sc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            sc.Severity = "High";

            Assert.Contains("Severity", changed);
        }

        [Fact]
        public void SeverityCount_PropertyChanged_FiresOnIconChange()
        {
            var sc = new SeverityCount();
            var changed = new List<string>();
            sc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            sc.Icon = null;

            Assert.Contains("Icon", changed);
        }

        [Fact]
        public void FileNode_PropertyChanged_FiresOnFileIconChange()
        {
            var node = new FileNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.FileIcon = null;

            Assert.Contains("FileIcon", changed);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnPackageNameChange()
        {
            var node = new VulnerabilityNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.PackageName = "lodash";

            Assert.Contains("PackageName", changed);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnPackageVersionChange()
        {
            var node = new VulnerabilityNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.PackageVersion = "4.17.19";

            Assert.Contains("PackageVersion", changed);
        }

        [Fact]
        public void VulnerabilityNode_PropertyChanged_FiresOnSeverityIconChange()
        {
            var node = new VulnerabilityNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.SeverityIcon = null;

            Assert.Contains("SeverityIcon", changed);
        }

        [Fact]
        public void PrimaryDisplayText_EmptyDescriptionAndPackageName_Oss_ShowsEmptyName()
        {
            var node = new VulnerabilityNode
            {
                Scanner = ScannerType.OSS,
                Severity = "High",
                Description = null,
                PackageName = null
            };
            Assert.Equal("High-risk package: ", node.PrimaryDisplayText);
        }

        [Fact]
        public void PrimaryDisplayText_IacEmptyDescription_StillFormats()
        {
            var node = new VulnerabilityNode { Scanner = ScannerType.IaC, Description = "" };
            Assert.Equal(" ", node.PrimaryDisplayText);
        }

        [Fact]
        public void SecondaryDisplayText_ZeroLineAndColumn_StillFormats()
        {
            var node = new VulnerabilityNode { Line = 0, Column = 0 };
            Assert.Contains("Ln 0", node.SecondaryDisplayText);
            Assert.Contains("Col 0", node.SecondaryDisplayText);
        }

        [Fact]
        public void DisplayText_ContainsDisplayName()
        {
            var node = new VulnerabilityNode { Description = "Test", Line = 1, Column = 1 };
            Assert.Contains(CxAssistConstants.DisplayName, node.DisplayText);
        }

        [Fact]
        public void FileNode_PropertyChanged_FiresOnSeverityCountsChange()
        {
            var node = new FileNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.SeverityCounts = new System.Collections.ObjectModel.ObservableCollection<SeverityCount>();

            Assert.Contains("SeverityCounts", changed);
        }

        [Fact]
        public void FileNode_PropertyChanged_FiresOnVulnerabilitiesChange()
        {
            var node = new FileNode();
            var changed = new List<string>();
            node.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            node.Vulnerabilities = new System.Collections.ObjectModel.ObservableCollection<VulnerabilityNode>();

            Assert.Contains("Vulnerabilities", changed);
        }

        #endregion
    }
}

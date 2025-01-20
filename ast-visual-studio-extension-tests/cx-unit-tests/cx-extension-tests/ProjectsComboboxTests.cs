//using ast_visual_studio_extension.CxExtension.Toolbar;
//using ast_visual_studio_extension.CxExtension.Utils;
//using ast_visual_studio_extension.CxWrapper.Models;
//using ast_visual_studio_extension.CxExtension.Panels;
//using Microsoft.VisualStudio.Shell;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Windows.Controls;
//using Xunit;

//namespace ast_visual_studio_extension.Tests
//{
//    public class ProjectsComboboxTests
//    {
//        private readonly Mock<CxToolbar> mockCxToolbar;
//        private readonly Mock<BranchesCombobox> mockBranchesCombobox;
//        private readonly ProjectsCombobox projectsCombobox;

//        public ProjectsComboboxTests()
//        {
//            mockCxToolbar = new Mock<CxToolbar>();
//            mockBranchesCombobox = new Mock<BranchesCombobox>(mockCxToolbar.Object, null);
//            projectsCombobox = new ProjectsCombobox(mockCxToolbar.Object, mockBranchesCombobox.Object);
//        }

//        [Fact]
//        public async Task LoadProjectsAsync_ShouldPopulateProjectsCombobox()
//        {
//            // Arrange
//            mockCxToolbar.Setup(toolbar => toolbar.Package).Returns(new Mock<AsyncPackage>().Object);
//            mockCxToolbar.Setup(toolbar => toolbar.ProjectsCombo).Returns(new ComboBox());

//            // Act
//            await projectsCombobox.LoadProjectsAsync();

//            // Assert
//            Assert.True(mockCxToolbar.Object.ProjectsCombo.IsEnabled);
//            Assert.True(mockCxToolbar.Object.ScansCombo.IsEnabled);
//        }

//        [Fact]
//        public async Task ResetExtensionAsync_ShouldResetComboboxesAndResults()
//        {
//            // Arrange
//            mockCxToolbar.Setup(toolbar => toolbar.Package).Returns(new Mock<AsyncPackage>().Object);
//            mockCxToolbar.Setup(toolbar => toolbar.ProjectsCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.BranchesCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ScansCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ResultsTreePanel).Returns(new Mock<ResultsTreePanel>(null, null, null, null).Object);

//            // Act
//            await projectsCombobox.ResetExtensionAsync();

//            // Assert
//            Assert.Equal(CxConstants.TOOLBAR_LOADING_PROJECTS, mockCxToolbar.Object.ProjectsCombo.Text);
//            Assert.Equal(CxConstants.TOOLBAR_LOADING_BRANCHES, mockCxToolbar.Object.BranchesCombo.Text);
//            Assert.Equal(CxConstants.TOOLBAR_LOADING_SCANS, mockCxToolbar.Object.ScansCombo.Text);
//            Assert.False(mockCxToolbar.Object.ProjectsCombo.IsEnabled);
//            Assert.False(mockCxToolbar.Object.BranchesCombo.IsEnabled);
//            Assert.False(mockCxToolbar.Object.ScansCombo.IsEnabled);
//        }

//        [Fact]
//        public async Task LoadProjectsComboboxAsync_ShouldLoadProjects()
//        {
//            // Arrange
//            var mockCxWrapper = new Mock<CxCLI.CxWrapper>();
//            mockCxWrapper.Setup(wrapper => wrapper.GetProjects()).Returns(new List<Project> { new Project { Name = "Project1" } });
//            mockCxToolbar.Setup(toolbar => toolbar.Package).Returns(new Mock<AsyncPackage>().Object);
//            mockCxToolbar.Setup(toolbar => toolbar.ProjectsCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ResultsTree).Returns(new TreeView());

//            // Act
//            await projectsCombobox.LoadProjectsComboboxAsync();

//            // Assert
//            Assert.Equal(CxConstants.TOOLBAR_SELECT_PROJECT, mockCxToolbar.Object.ProjectsCombo.Text);
//            Assert.Single(mockCxToolbar.Object.ProjectsCombo.Items);
//        }

//        [Fact]
//        public void OnChangeProject_ShouldUpdateComboboxesAndResults()
//        {
//            // Arrange
//            var comboBoxItem = new ComboBoxItem { Content = "Project1", Tag = new Project { Id = "1" } };
//            var comboBox = new ComboBox();
//            comboBox.Items.Add(comboBoxItem);
//            comboBox.SelectedItem = comboBoxItem;
//            mockCxToolbar.Setup(toolbar => toolbar.ProjectsCombo).Returns(comboBox);
//            mockCxToolbar.Setup(toolbar => toolbar.BranchesCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ScansCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ResultsTreePanel).Returns(new Mock<ResultsTreePanel>(null, null, null, null).Object);

//            // Act
//            projectsCombobox.OnChangeProject(comboBox, null);

//            // Assert
//            Assert.Equal(CxConstants.TOOLBAR_LOADING_BRANCHES, mockCxToolbar.Object.BranchesCombo.Text);
//            Assert.Equal(CxConstants.TOOLBAR_SELECT_SCAN, mockCxToolbar.Object.ScansCombo.Text);
//        }

//        [Fact]
//        public void ResetOthersComboBoxesAndResults_ShouldResetComboboxesAndResults()
//        {
//            // Arrange
//            mockCxToolbar.Setup(toolbar => toolbar.BranchesCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ScansCombo).Returns(new ComboBox());
//            mockCxToolbar.Setup(toolbar => toolbar.ResultsTreePanel).Returns(new Mock<ResultsTreePanel>(null, null, null, null).Object);

//            // Act
//            projectsCombobox.ResetOthersComboBoxesAndResults();

//            // Assert
//            Assert.False(mockCxToolbar.Object.BranchesCombo.IsEnabled);
//            Assert.False(mockCxToolbar.Object.ScansCombo.IsEnabled);
//            Assert.Equal(CxConstants.TOOLBAR_SELECT_BRANCH, mockCxToolbar.Object.BranchesCombo.Text);
//            Assert.Equal(CxConstants.TOOLBAR_SELECT_SCAN, mockCxToolbar.Object.ScansCombo.Text);
//        }
//    }
//}

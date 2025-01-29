using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core.AutomationElements;
using System.Threading.Tasks;
using System.Linq;

namespace UITests
{
    [TestClass]
    public class ScanTests : BaseTest
    {

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ClassInitialize(context);
        }


        private async Task ClickRefreshButtonAsync()
        {
            var refreshBtn = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "RefreshBtn", "Refresh button not found in Checkmarx window");
            refreshBtn.WaitUntilEnabled().Click();

            await Task.Delay(2000);
        }
        private async Task<AutomationElement[]> ExpandComboboxAndGetItemsAsync(AutomationElement combobox, int maxRetries = 3, int delayMs = 2000)
        {
            combobox.AsComboBox().Expand();
            var retries = 0;
            AutomationElement[] items;

            do
            {
                items = combobox.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));

                if (items.Length == 0 && retries < maxRetries)
                {
                    await Task.Delay(delayMs);
                    combobox.AsComboBox().Expand();
                    retries++;
                }
                else
                {
                    break;
                }

            } while (retries <= maxRetries);

            return items;
        }

        private async Task SelectItemFromComboboxAsync(AutomationElement combobox, int maxRetries = 3, int delayMs = 2000)
        {
            await Task.Delay(delayMs);

            var items = await ExpandComboboxAndGetItemsAsync(combobox, maxRetries, delayMs);

            Assert.IsTrue(items.Length > 0, combobox + " list is empty even after retries.");

            items[0].Patterns.SelectionItem.Pattern.Select();
            Assert.IsTrue(items[0].Patterns.SelectionItem.Pattern.IsSelected.Value, "Item was not selected.");
            await Task.Delay(delayMs);
        }

        [TestMethod]
        public async Task ProjectComboboxSelectItemAsync()
        {
            await ClickRefreshButtonAsync();
            
            var projectsCombobox = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "ProjectsCombobox", "Projects combobox not found in Checkmarx window");
            
            await SelectItemFromComboboxAsync(projectsCombobox);

            var selectedProject = projectsCombobox.AsComboBox().SelectedItem;
            Assert.IsNotNull(selectedProject, "No project was selected");
        }

        [TestMethod]
        public async Task BranchesComboboxSelectItemAsync()
        {
            await ProjectComboboxSelectItemAsync();

            var branchCombobox = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "BranchesCombobox", "Branches combobox not found in Checkmarx window");

            await SelectItemFromComboboxAsync(branchCombobox);

            var selectedProject = branchCombobox.AsComboBox().SelectedItem;
            Assert.IsNotNull(selectedProject, "No branch was selected");

        }
        [TestMethod]
        public async Task ScansComboboxSelectItemAsync()
        {
            await BranchesComboboxSelectItemAsync();
            var scanCombobox = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "ScansCombobox", "Scans combobox not found in Checkmarx window");

            await SelectItemFromComboboxAsync(scanCombobox);

            var selectedProject = scanCombobox.AsComboBox().SelectedItem;
            Assert.IsNotNull(selectedProject, "No scan was selected");

            var treeViewResults = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "TreeViewResults", "Tree view results not found in Checkmarx window");

            var results = treeViewResults.FindAllDescendants();
            Assert.AreNotEqual(0, results.Length);
        }

        [TestMethod]
        public async Task ProjectsComboboxSearchAsync()
        {
            var searchText = "Project";
            await ClickRefreshButtonAsync();

            var projectsCombobox = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "ProjectsCombobox", "Projects combobox not found in Checkmarx window");
           
            var projectTextBox = TestUtils.GetElementByAutomationIdWithNotNullCheck(projectsCombobox, "PART_EditableTextBox", "Project text box not found in Projects combobox");
            await Task.Delay(2000);

            projectTextBox.AsTextBox().Enter(searchText);
            await Task.Delay(2000);

            var projectList = await ExpandComboboxAndGetItemsAsync(projectsCombobox);

            bool allContainSearchText = projectList.All(project => project.Name.Contains(searchText));
            Assert.IsTrue(allContainSearchText, $"Not all project names contain the text '{searchText}'");

            var branchCombobox = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "BranchesCombobox", "Branches combobox not found in Checkmarx window");
            Assert.IsFalse(branchCombobox.IsEnabled, "Branch combobox is enabled before project selection");

            var scanCombobox = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "ScansCombobox", "Scans combobox not found in Checkmarx window");
            Assert.IsFalse(scanCombobox.IsEnabled, "Scans combobox is enabled before project selection");

            var scanStartBtn = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "ScanStartBtn", "ScanStartBtn button not found in Checkmarx window");
            Assert.IsFalse(scanStartBtn.IsEnabled, "ScanStartBtn button is enabled before project and branch selection");
        }
    }
}

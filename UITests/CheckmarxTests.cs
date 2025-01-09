using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core.AutomationElements;
using System.Threading.Tasks;

namespace UITests
{
    [TestClass]
    public class CheckmarxTests : BaseTest
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ClassInitialize(context);
        }

        [TestMethod]
        public async Task OpenCheckmarxWindow()
        {
            Task.Delay(60000).Wait();
            // Find the View menu
            var viewMenu = _mainWindow.FindFirstDescendant(cf => cf.ByName("View"));
            Assert.IsNotNull(viewMenu, "View menu not found");

            // Open the "View" menu by clicking it
            viewMenu.WaitUntilEnabled().Click();
            await Task.Delay(500);

            var allMenuItems = _mainWindow.FindAllDescendants(cf =>
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem));
            bool foundOtherWindows = false;


            foreach (var menuItem in allMenuItems)
            {
                if (menuItem.Name == "Other Windows")
                {
                    foundOtherWindows = true;
                    menuItem.WaitUntilEnabled().Click();
                    await Task.Delay(1000);

                    // Now select a specific window from the list "Checkmarx"
                    var checkmarxOption = _mainWindow.FindFirstDescendant(cf => cf.ByName("Checkmarx"));
                    Assert.IsNotNull(checkmarxOption, "Checkmarx option not found in Other Windows menu");
                    checkmarxOption.WaitUntilEnabled().Click();
                    break;
                }
            }
            Assert.IsTrue(foundOtherWindows, "Other Windows menu item not found");

        }
    }
}

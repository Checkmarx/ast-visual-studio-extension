using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core.AutomationElements;
using System.Threading.Tasks;
using System.IO;

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
            // Take a screenshot at the beginning of the test
            TakeScreenshot("screenshot");
            
            var descendents = _mainWindow.FindAllDescendants();
            Console.WriteLine($"Descendants of main window: {descendents.Length}");
            Console.WriteLine("Descendant names:");
            foreach (var descendent in descendents)
            {
                Console.WriteLine(descendent.Name);
                Console.WriteLine(descendent.ControlType);
                Console.WriteLine(descendent.ClassName);
                Console.WriteLine(descendent.IsAvailable);
                Console.WriteLine(descendent.IsEnabled);
                Console.WriteLine(descendent.IsOffscreen);
                Console.WriteLine(descendent.HelpText);
                Console.WriteLine("\n\n\n\n");
            }

            if (descendents.Length == 0)
            {
                Console.WriteLine("Empty");
            }
            
            Console.WriteLine("Writing descendant names to file");

            // File path for writing descendant names
            // Find the View menu
            var viewMenu = _mainWindow.FindFirstDescendant(cf => cf.ByName("View"));

            // Assert View menu is not null
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

                    // Select a specific window from the list "Checkmarx"
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

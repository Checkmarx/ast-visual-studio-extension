using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core.AutomationElements;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
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
    // Take a screenshot at the start of the test
    TakeScreenshot("screenshot_start.png");

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

/// <summary>
/// Captures the current screen and saves it to a file.
/// </summary>
/// <param name="filePath">The file path to save the screenshot.</param>
private void TakeScreenshot(string screenshotName)
{
    try
    {
        var screenshot = _mainWindow.Capture();
        var filePath = Path.Combine("D:\\a\\ast-visual-studio-extension\\ast-visual-studio-extension\\Screenshots\\", $"{screenshotName}.png");
        screenshot.Save(filePath, ImageFormat.Png);
        Console.WriteLine($"Screenshot saved to: {filePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to take screenshot: {ex.Message}");
    }
}

    }
}

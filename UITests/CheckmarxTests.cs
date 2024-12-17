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
            var viewMenu = _mainWindow.FindFirstDescendant(cf => cf.ByName("View"));
            if (viewMenu != null)
            {
                viewMenu.WaitUntilEnabled().Click();
                await Task.Delay(1000);

                var allMenuItems = _mainWindow.FindAllDescendants(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem));

                foreach (var menuItem in allMenuItems)
                {
                    if (menuItem.Name == "Other Windows")
                    {
                        menuItem.WaitUntilEnabled().Click();
                        await Task.Delay(2000);

                        var checkmarxOption = _mainWindow.FindFirstDescendant(cf => cf.ByName("Checkmarx"));
                        if (checkmarxOption != null)
                        {
                            checkmarxOption.WaitUntilEnabled().Click();
                        }
                        break;
                    }
                }
            }
        }
    }
}
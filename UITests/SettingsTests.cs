using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core.AutomationElements;
using System.Threading.Tasks;
using FlaUI.Core.Definitions;

namespace UITests
{
    [TestClass]
    public class SettingsTests : BaseTest
    {
        private static AutomationElement _settingsWindow;
        private const int ShortDelay = 500;
        private const int LongDelay = 1500;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ClassInitialize(context);
        }

        private static async Task OpenSettingsWindowAsync()
        {
            if (_settingsWindow == null)
            {
                var settingsButton = TestUtils.GetElementByAutomationIdWithNotNullCheck(_checkmarxWindow, "SettingsBtn", "Settings button not found in Checkmarx window");

                settingsButton.WaitUntilEnabled().Click();
                await Task.Delay(ShortDelay);

                _settingsWindow = TestUtils.GetElementByNameWithNotNullCheck(_mainWindow, "Checkmarx settings", "Settings window not found");
            }
        }

        [TestMethod]
        public async Task StartScanWithAscaAsync()
        {
            await OpenSettingsWindowAsync();

            var ascaButton = TestUtils.GetElementByAutomationIdWithNotNullCheck(_mainWindow, "ascaCheckBox", "ASCA button not found in settings window");
           
            var togglePattern = ascaButton.Patterns.Toggle.Pattern;
            if (togglePattern.ToggleState != ToggleState.On)
            {
                ascaButton.WaitUntilEnabled().Click();
                await Task.Delay(ShortDelay);
            }
            
            var ascaIsStarted = TestUtils.GetElementByNameWithNotNullCheck(_mainWindow, "AI Secure Coding Assistant Engine started", "ASCA is not started");
        }
        
        [TestMethod]
        public async Task CheckValidation()
        {
            await OpenSettingsWindowAsync();

            var checkValidationButton = TestUtils.GetElementByAutomationIdWithNotNullCheck(_mainWindow, "button1", "Check Validation button not found in settings window");

            checkValidationButton.WaitUntilEnabled().DoubleClick();
            await Task.Delay(LongDelay);

            var validateResults = TestUtils.GetElementByAutomationIdWithNotNullCheck(_mainWindow, "lblValidationResult", "Validate failed");
        }
    }
}

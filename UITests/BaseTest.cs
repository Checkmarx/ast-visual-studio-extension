using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.UIA3;
using FlaUI.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using AutomationWindow = FlaUI.Core.AutomationElements.Window;
using FlaUI.Core.Capturing;

namespace UITests
{
    [TestClass]
    public class BaseTest
    {
        protected static UIA3Automation   _automation;
        protected static Application      _app;
        protected static AutomationWindow _mainWindow;
        protected static DTE              _dte;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Initialize automation and launch VS
            _automation = new UIA3Automation();
            _app        = Application.Launch("devenv.exe","/ResetSettings General");

            // Wait for launch VS
            Task.Delay(15000).Wait();
            _mainWindow = WaitForMainWindow();

            // Handle initial setup
            SetupVisualStudio().Wait();
        }

        private static async Task SetupVisualStudio()
        {
            // Find and click the "Continue without code" button
            var continueWithoutCodeButton = _mainWindow.FindFirstDescendant(cf => cf.ByName("Continue without code"));
            if (continueWithoutCodeButton != null)
            {
                var invokePattern = continueWithoutCodeButton.Patterns.Invoke.Pattern;
                invokePattern.Invoke();
                _mainWindow = WaitForMainWindow();
            }
            else
            {
                Console.WriteLine("Continue without code button not found");
                _mainWindow = WaitForMainWindow();
            }

            var dkipThis = _mainWindow.FindFirstDescendant(cf => cf.ByName("Skip this for now."));
            if (dkipThis != null)
            {
                var invokePattern = dkipThis.Patterns.Invoke.Pattern;
                Console.WriteLine("Skip this for now button  FOUND");
                invokePattern.Invoke();
                _mainWindow = WaitForMainWindow();
            }
            else
            {
                Console.WriteLine("Skip this for now button not found");
                _mainWindow = WaitForMainWindow();
            }
        }

        public static AutomationWindow WaitForMainWindow(int timeoutInSeconds = 30)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutInSeconds)
            {
                try
                {
                    // Wait until the main window is available
                    Task.Delay(5000).Wait();
                    var window = _app.GetMainWindow(_automation);
                    if (window != null && window.IsAvailable)
                    {
                        return window;
                    }
                }
                catch (Exception)
                {
                    // Ignore exception, retry until timeout
                }

                Task.Delay(500).Wait();
            }

            throw new TimeoutException("Main window did not appear within the timeout period.");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                if (_app != null)
                {
                    _app.Close();
                    _app.Dispose();
                }
            }
            finally
            {
                if (_automation != null)
                {
                    _automation.Dispose();
                }

                _mainWindow = null;
            }
        }

        public void TakeScreenshot(string screenshotName)
        {
            try
            {
                var screen = Capture.Screen();
                var screenshotPath =
                    Path.Combine("D:\\a\\ast-visual-studio-extension\\ast-visual-studio-extension\\Screenshots\\",
                                 $"{screenshotName}.png");
                screen.ToFile(screenshotPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take screenshot: {ex.Message}");
            }
        }
    }
}
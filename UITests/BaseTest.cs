﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.UIA3;
using FlaUI.Core;
using System;
using System.Threading.Tasks;
using EnvDTE;
using AutomationWindow = FlaUI.Core.AutomationElements.Window;
using FlaUI.Core.AutomationElements;

namespace UITests
{
   [TestClass]
    public class BaseTest
    {
        protected static UIA3Automation _automation;
        protected static Application _app;
        protected static AutomationWindow _mainWindow;
        protected static DTE _dte;
        protected static AutomationElement _checkmarxWindow;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Initialize automation and launch VS
            _automation = new UIA3Automation();
            _app = Application.Launch("devenv.exe");

            // Wait for launch VS
            Task.Delay(15000).Wait();
            _mainWindow = WaitForMainWindow();

            // Handle initial setup
            SetupVisualStudio().Wait();
            OpenCheckmarxWindowAsync().Wait();
        }
        protected static async Task OpenCheckmarxWindowAsync()
        {
            if (_checkmarxWindow == null)
            {
                _checkmarxWindow = _mainWindow.FindFirstDescendant(cf => cf.ByName("Checkmarx"));
                if (_checkmarxWindow != null)
                { return; }

                var viewMenu = TestUtils.GetElementByNameWithNotNullCheck(_mainWindow, "View", "View menu not found");
                viewMenu.WaitUntilEnabled().Click();
                await Task.Delay(500);

                var otherWindows = TestUtils.GetElementByNameWithNotNullCheck(_mainWindow, "Other Windows", "Other Windows menu item not found");

                otherWindows.WaitUntilEnabled().Click();
                await Task.Delay(500);

                var checkmarxOption = TestUtils.GetElementByNameWithNotNullCheck(_mainWindow, "Checkmarx", "Checkmarx option not found in menu");
                checkmarxOption.WaitUntilEnabled().Click();
                await Task.Delay(1000);

                _checkmarxWindow = TestUtils.GetElementByNameWithNotNullCheck(_mainWindow, "Checkmarx", "Checkmarx window not found");
            }
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
    }
}
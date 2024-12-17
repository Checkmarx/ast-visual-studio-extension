using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.UIA3;
using FlaUI.Core;
using System;
using System.Threading.Tasks;
using EnvDTE;
using AutomationWindow = FlaUI.Core.AutomationElements.Window;

namespace UITests
{
   [TestClass]
   public class BaseTest
   {
       protected static UIA3Automation _automation;
       protected static Application _app;
       protected static AutomationWindow _mainWindow;
       protected static DTE _dte;

       [ClassInitialize]
       public static void ClassInitialize(TestContext context)
       {
           // Initialize automation and launch VS
           _automation = new UIA3Automation();
           _app = Application.Launch("devenv.exe");

           // Wait until the main window is available
           _mainWindow = WaitForMainWindow();

           // Handle initial setup
           SetupVisualStudio().Wait();
       }

       private static async Task SetupVisualStudio()
       {
           await Task.Delay(5000);

           var continueWithoutCodeButton = _mainWindow.FindFirstDescendant(cf => cf.ByName("Continue without code"));
           if (continueWithoutCodeButton != null)
           {
               var invokePattern = continueWithoutCodeButton.Patterns.Invoke.Pattern;
               invokePattern.Invoke();
               await Task.Delay(2000);
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
                   Task.Delay(10000).Wait();
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
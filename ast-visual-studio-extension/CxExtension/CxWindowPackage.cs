using ast_visual_studio_extension.CxExtension.Commands;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#14110", "#14112", "1.0", IconResourceID = 14400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus1.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindow(typeof(CxWindow),Style = VsDockStyle.Tabbed,Orientation = ToolWindowOrientation.Right,Window = EnvDTE.Constants.vsWindowKindOutput,Transient = false)]
    [ProvideToolWindow(typeof(DevAssist.UI.FindingsWindow.DevAssistFindingsWindow), Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Bottom, Window = EnvDTE.Constants.vsWindowKindOutput, Transient = false)]
    [Guid(PackageGuidString)]
    public sealed class CxWindowPackage : AsyncPackage
    {
        /// <summary>
        /// CxWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "63d5f3b4-a254-4bef-974b-0733c306ed2c";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                ConfigureLog4net();

                // Command to create Checkmarx extension main window
                await CxWindowCommand.InitializeAsync(this);

                // Test Gutter Icons Direct Command (tool command only, not visible in menu)
                await TestGutterIconsDirectCommand.InitializeAsync(this);

                // Test Error List Customization Command (POC for AST-133228)
                await TestErrorListCustomizationCommand.InitializeAsync(this);

                // Show Findings Window Command (POC for AST-133228 - Custom Tool Window)
                // Command still works programmatically but not visible in menu
                await ShowFindingsWindowCommand.InitializeAsync(this);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
            }
        }
        private string GetLogFilePath()
        {
            string dirName = Debugger.IsAttached ? "ast-visual-studio-extension-debug" : "ast-visual-studio-extension";
            var logDirectory = Path.Combine(Path.GetTempPath(), dirName, "Logs");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            return Path.Combine(logDirectory, "ast-extension.log");
        }

        private string GetLog4netConfigPath()
        {
            if (Debugger.IsAttached)
            {
                return Path.Combine(Environment.CurrentDirectory, "log4net.config");
            }

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string baseDirectory = Path.GetDirectoryName(assemblyLocation);

            string[] possiblePaths = new string[]
            {
                    Path.Combine(baseDirectory, "log4net.config"), // Installed extension location
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config"), // Base directory
            };
            return possiblePaths.FirstOrDefault(File.Exists);
        }

        private void ConfigureLog4net()
        {
            try
            {
                string logFilePath = GetLogFilePath();
                GlobalContext.Properties["CxLogFileName"] = logFilePath;

                string log4netConfigPath = GetLog4netConfigPath();

                if (log4netConfigPath == null)
                {
                    throw new FileNotFoundException("log4net.config not found in expected locations.");
                }
                var logRepository = LogManager.GetRepository();
                XmlConfigurator.Configure(logRepository, new FileInfo(log4netConfigPath));

                var appender = ((Hierarchy)logRepository).Root.Appenders[0];
                if (appender is RollingFileAppender fileAppender && fileAppender.File != logFilePath)
                {
                    fileAppender.File = logFilePath;
                    fileAppender.ActivateOptions();
                }
            }
            catch (Exception ex)
            {
                var fallbackLogger = LogManager.GetLogger(typeof(CxWindowPackage));
                fallbackLogger?.Error("Error during log4net configuration", ex);
            }
        }
        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (toolWindowType == typeof(CxWindow).GUID)
            {
                return this;
            }

            return base.GetAsyncToolWindowFactory(toolWindowType);
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(CxWindow))
            {
                return "CxWindow loading";
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            return Task.FromResult(this as object);
        }

        #endregion
    }
}

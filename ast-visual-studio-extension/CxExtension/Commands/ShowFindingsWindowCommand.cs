using System;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using ast_visual_studio_extension.CxExtension.DevAssist.UI.FindingsWindow;

namespace ast_visual_studio_extension.CxExtension.Commands
{
    /// <summary>
    /// Command handler to show and populate the DevAssist Findings window
    /// </summary>
    internal sealed class ShowFindingsWindowCommand
    {
        public const int CommandId = 0x0110;
        public static readonly Guid CommandSet = new Guid("a6e8b6e3-8e3e-4e3e-8e3e-8e3e8e3e8e3f");

        private readonly AsyncPackage package;

        private ShowFindingsWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ShowFindingsWindowCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ShowFindingsWindowCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Show the existing Checkmarx window (not the standalone DevAssistFindingsWindow)
                ToolWindowPane window = this.package.FindToolWindow(typeof(CxWindow), 0, true);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create Checkmarx window");
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

                // Get the CxWindowControl and switch to DevAssist tab
                var cxWindow = window as CxWindow;
                if (cxWindow != null && cxWindow.Content is CxWindowControl cxWindowControl)
                {
                    // Switch to the DevAssist Findings tab
                    cxWindowControl.SwitchToDevAssistTab();

                    // Get the DevAssist Findings Control and populate with test data
                    var findingsControl = cxWindowControl.GetDevAssistFindingsControl();
                    if (findingsControl != null)
                    {
                        PopulateTestData(findingsControl);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing DevAssist findings: {ex.Message}");
            }
        }

        private void PopulateTestData(DevAssistFindingsControl control)
        {
            if (control == null) return;

            var fileNodes = new ObservableCollection<FileNode>();

            // File 1: go.mod with High and Medium vulnerabilities
            var file1 = new FileNode
            {
                FileName = "go.mod",
                FilePath = "C:\\Projects\\TestProject\\go.mod",
                FileIcon = LoadIcon("document.png")
            };

            file1.Vulnerabilities.Add(new VulnerabilityNode
            {
                Severity = "High",
                SeverityIcon = LoadSeverityIcon("High"),
                PackageName = "helm.sh/helm/v3",
                PackageVersion = "v3.18.2",
                Line = 38,
                Column = 1,
                FilePath = file1.FilePath
            });

            file1.Vulnerabilities.Add(new VulnerabilityNode
            {
                Severity = "Medium",
                SeverityIcon = LoadSeverityIcon("Medium"),
                PackageName = "github.com/docker/docker",
                PackageVersion = "v20.10.7",
                Line = 42,
                Column = 1,
                FilePath = file1.FilePath
            });

            // Add severity counts
            file1.SeverityCounts.Add(new SeverityCount { Severity = "High", Count = 1, Icon = LoadSeverityIcon("High") });
            file1.SeverityCounts.Add(new SeverityCount { Severity = "Medium", Count = 1, Icon = LoadSeverityIcon("Medium") });

            fileNodes.Add(file1);

            // File 2: package.json with Malicious and Low vulnerabilities
            var file2 = new FileNode
            {
                FileName = "package.json",
                FilePath = "C:\\Projects\\TestProject\\package.json",
                FileIcon = LoadIcon("document.png")
            };

            file2.Vulnerabilities.Add(new VulnerabilityNode
            {
                Severity = "Malicious",
                SeverityIcon = LoadSeverityIcon("Malicious"),
                PackageName = "evil-package",
                PackageVersion = "1.0.0",
                Line = 15,
                Column = 4,
                FilePath = file2.FilePath
            });

            file2.Vulnerabilities.Add(new VulnerabilityNode
            {
                Severity = "Low",
                SeverityIcon = LoadSeverityIcon("Low"),
                PackageName = "old-library",
                PackageVersion = "2.3.1",
                Line = 23,
                Column = 4,
                FilePath = file2.FilePath
            });

            // Add severity counts
            file2.SeverityCounts.Add(new SeverityCount { Severity = "Malicious", Count = 1, Icon = LoadSeverityIcon("Malicious") });
            file2.SeverityCounts.Add(new SeverityCount { Severity = "Low", Count = 1, Icon = LoadSeverityIcon("Low") });

            fileNodes.Add(file2);

            // Use SetAllFileNodes to enable filtering
            control.SetAllFileNodes(fileNodes);
        }

        /// <summary>
        /// Detect if Visual Studio is using dark theme
        /// </summary>
        private bool IsDarkTheme()
        {
            try
            {
                // Get the VS theme color using PlatformUI
                var color = Microsoft.VisualStudio.PlatformUI.VSColorTheme.GetThemedColor(Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowBackgroundColorKey);

                // Calculate brightness (simple luminance formula)
                int brightness = (int)Math.Sqrt(
                    color.R * color.R * 0.299 +
                    color.G * color.G * 0.587 +
                    color.B * color.B * 0.114);

                // If brightness is less than 128, it's a dark theme
                return brightness < 128;
            }
            catch
            {
                // Default to dark theme if detection fails
                return true;
            }
        }

        /// <summary>
        /// Load severity icon based on severity level - uses JetBrains PNG icons with theme support
        /// </summary>
        private System.Windows.Media.ImageSource LoadSeverityIcon(string severity)
        {
            try
            {
                // Determine theme folder
                string themeFolder = IsDarkTheme() ? "Dark" : "Light";

                // Build the icon path
                string iconName = severity.ToLower();
                string iconPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/DevAssist/Icons/{themeFolder}/{iconName}.png";

                // Load the PNG image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading severity icon for {severity}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load generic file icon
        /// </summary>
        private System.Windows.Media.ImageSource LoadIcon(string iconName)
        {
            try
            {
                // Use existing info icon as placeholder for file icon
                var uri = new Uri("pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/info.png", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Important for cross-thread access
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading file icon: {ex.Message}");
                return null;
            }
        }
    }
}


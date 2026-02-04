using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using EnvDTE;

namespace ast_visual_studio_extension.CxExtension.Commands
{
    /// <summary>
    /// POC Command to test Visual Studio Error List customization capabilities
    /// Tests: Custom icons, severity-based styling, context menus, grouping
    /// Part of AST-133228 - POC - Problem Window Customization Validation
    /// </summary>
    internal sealed class TestErrorListCustomizationCommand
    {
        public const int CommandId = 0x0109;
        public static readonly Guid CommandSet = new Guid("a6e8b6e3-8e3e-4e3e-8e3e-8e3e8e3e8e3e");

        private readonly AsyncPackage package;
        private ErrorListProvider _errorListProvider;

        private TestErrorListCustomizationCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static TestErrorListCustomizationCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TestErrorListCustomizationCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                System.Diagnostics.Debug.WriteLine("DevAssist: Testing Error List customization...");

                // Initialize ErrorListProvider
                if (_errorListProvider == null)
                {
                    _errorListProvider = new ErrorListProvider(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider)
                    {
                        ProviderName = "Checkmarx DevAssist",
                        ProviderGuid = new Guid("12345678-1234-1234-1234-123456789ABC")
                    };
                }

                _errorListProvider.Tasks.Clear();

                // Test 1: Add tasks with different severities
                AddTestTask("Malicious", TaskErrorCategory.Error, "SQL Injection detected", "TestFile.cs", 10, 5);
                AddTestTask("Critical", TaskErrorCategory.Error, "Remote Code Execution vulnerability", "TestFile.cs", 25, 12);
                AddTestTask("High", TaskErrorCategory.Error, "Cross-Site Scripting (XSS) found", "TestFile.cs", 42, 8);
                AddTestTask("Medium", TaskErrorCategory.Warning, "Hardcoded password detected", "TestFile.cs", 58, 15);
                AddTestTask("Low", TaskErrorCategory.Message, "Weak encryption algorithm used", "TestFile.cs", 75, 20);

                // Test 2: Add tasks from different files (test grouping)
                AddTestTask("High", TaskErrorCategory.Error, "Path Traversal vulnerability", "AnotherFile.cs", 15, 3);
                AddTestTask("Medium", TaskErrorCategory.Warning, "Insecure deserialization", "AnotherFile.cs", 30, 7);

                // Show the Error List window
                var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                if (dte != null)
                {
                    dte.ExecuteCommand("View.ErrorList");
                }

                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"✅ Error List Customization Test Complete!\n\n" +
                    $"Added 7 test items to Error List:\n" +
                    $"• 3 Errors (Malicious, Critical, High)\n" +
                    $"• 2 Warnings (Medium)\n" +
                    $"• 2 Messages (Low)\n\n" +
                    $"TESTING:\n" +
                    $"❓ Can we add custom icons per severity?\n" +
                    $"❓ Can we customize the appearance?\n" +
                    $"❓ Can we add right-click context menu?\n" +
                    $"❓ Does grouping by file work?\n\n" +
                    $"Check the Error List window to see the results.",
                    "Test Error List Customization - POC",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist: Error in Error List test: {ex.Message}\n{ex.StackTrace}");
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error: {ex.Message}",
                    "Test Error List Customization",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private void AddTestTask(string severity, TaskErrorCategory category, string description, string file, int line, int column)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var task = new ErrorTask
            {
                Category = TaskCategory.CodeSense,
                ErrorCategory = category,
                Text = $"[{severity}] {description} (DevAssist)",
                Document = file,
                Line = line - 1,
                Column = column,
                HelpKeyword = $"CX-{severity}-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            // Test: Can we add custom properties?
            // Note: ErrorTask doesn't have a way to add custom icons or metadata directly

            // Add navigation handler
            task.Navigate += (s, e) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Navigating to:\n{file}\nLine: {line}, Column: {column}\nSeverity: {severity}",
                    "Navigation Test",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            };

            _errorListProvider.Tasks.Add(task);
        }
    }
}


using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace ast_visual_studio_extension.CxExtension.Commands
{
    /// <summary>
    /// This class represents the command used to create a tool window for the Checkmarx plugin
    /// </summary>
    internal sealed class CxWindowCommand
    {
        public static readonly Guid CommandSet = new Guid("e46cd6d8-268d-4e77-9074-071e72a25f39");
        
        public const int CxWindowCommandId = 0x0100;

        public const string guidCxWindowPackageCmdSet = "e46cd6d8-268d-4e77-9074-071e72a25f39";

        private readonly AsyncPackage package;

        private CxWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CxWindowCommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static CxWindowCommand Instance
        {
            get;
            private set;
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new CxWindowCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = this.package.JoinableTaskFactory.RunAsync(async delegate
              {
                  ToolWindowPane window = await this.package.ShowToolWindowAsync(typeof(CxWindow), 0, true, this.package.DisposalToken);
                  if ((null == window) || (null == window.Frame))
                  {
                      throw new NotSupportedException("Cannot create tool window");
                  }
              });
        }
    }
}

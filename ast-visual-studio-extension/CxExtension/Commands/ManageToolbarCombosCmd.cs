using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using ast_visual_studio_extension.CxExtension.Toolbar;

namespace ast_visual_studio_extension.CxExtension
{
    /// <summary>
    /// This class represents the command used to manage all toolbar comboboxes events
    /// </summary>
    internal sealed class ManageToolbarCombosCmd
    {
        public static readonly Guid CommandSet = new Guid("e46cd6d8-268d-4e77-9074-071e72a25f39");

        public const int ScansComboboxCommandId = 0x103;
        public const int ScansComboboxGetListCommandId = 0x14;

        private readonly AsyncPackage package;

        public ManageToolbarCombosCmd(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            if (commandService != null)
            {
                ScansCombobox scansCombobox = new ScansCombobox(this.package);

                // Command used to handle on change event for scans combobox
                commandService.AddCommand(scansCombobox.GetOnChangeScanCommand());

                // Command used to load scans combobox with a list of scan ids
                commandService.AddCommand(scansCombobox.GetOnLoadScansCommand());
            }
        }       

        public static ManageToolbarCombosCmd Instance
        {
            get;
            private set;
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new ManageToolbarCombosCmd(package, commandService);
        }
    }
}

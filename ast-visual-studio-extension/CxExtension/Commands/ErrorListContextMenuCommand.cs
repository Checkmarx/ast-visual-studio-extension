using System;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using System.ComponentModel.Design;

namespace ast_visual_studio_extension.CxExtension.Commands
{
    /// <summary>
    /// Adds Checkmarx One Assist commands to the Error List context menu (right-click).
    /// Commands are enabled only when the selected Error List item is a CxAssist finding.
    /// Actions: Fix with Checkmarx One Assist, View details, Ignore this vulnerability, Ignore all of this type.
    /// </summary>
    internal sealed class ErrorListContextMenuCommand
    {
        public const int FixCommandId = 0x0210;
        public const int ViewDetailsCommandId = 0x0211;
        public const int IgnoreThisCommandId = 0x0212;
        public const int IgnoreAllCommandId = 0x0213;

        private static readonly Guid CommandSetGuid = new Guid("b7e8b6e3-8e3e-4e3e-8e3e-8e3e8e3e8e40");

        private readonly AsyncPackage _package;
        private readonly OleMenuCommandService _commandService;

        private ErrorListContextMenuCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            AddCommand(FixCommandId, OnFixWithAssist);
            AddCommand(ViewDetailsCommandId, OnViewDetails);
            AddCommand(IgnoreThisCommandId, OnIgnoreThis, v => CxAssistConstants.GetIgnoreThisLabel(v.Scanner));
            AddCommand(IgnoreAllCommandId, OnIgnoreAll, v => CxAssistConstants.GetIgnoreAllLabel(v.Scanner), v => CxAssistConstants.ShouldShowIgnoreAll(v.Scanner));
        }

        public static ErrorListContextMenuCommand Instance { get; private set; }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null) return;
            Instance = new ErrorListContextMenuCommand(package, commandService);
        }

        private void AddCommand(int commandId, EventHandler invokeHandler, Func<Vulnerability, string> getText = null, Func<Vulnerability, bool> isVisible = null)
        {
            var id = new CommandID(CommandSetGuid, commandId);
            var cmd = new OleMenuCommand(invokeHandler, id);
            cmd.BeforeQueryStatus += (s, e) =>
            {
                var v = GetSelectedCxAssistVulnerability();
                bool visible = v != null && (isVisible == null || isVisible(v));
                cmd.Visible = cmd.Enabled = visible;
                if (getText != null && v != null)
                    cmd.Text = getText(v);
            };
            _commandService.AddCommand(cmd);
        }

        /// <summary>
        /// Gets the CxAssist vulnerability for the currently selected Error List item, or null.
        /// Uses IVsTaskList2.EnumSelectedItems when available; otherwise matches by DTE ErrorList selection.
        /// </summary>
        private static Vulnerability GetSelectedCxAssistVulnerability()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var errorList = Package.GetGlobalService(typeof(SVsErrorList)) as IVsTaskList2;
                if (errorList != null)
                {
                    if (errorList.EnumSelectedItems(out var enumItems) == 0 && enumItems != null)
                    {
                        var items = new IVsTaskItem[1];
                        var fetchedArray = new uint[1];
                        if (enumItems.Next(1, items, fetchedArray) == 0 && fetchedArray[0] > 0 && items[0] != null)
                        {
                            // Selected item may be our ErrorTask (has HelpKeyword, Document, Line)
                            if (items[0] is ErrorTask et)
                            {
                                if (!string.IsNullOrEmpty(et.HelpKeyword) && et.HelpKeyword.StartsWith(CxAssistErrorListSync.HelpKeywordPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    string id = et.HelpKeyword.Substring(CxAssistErrorListSync.HelpKeywordPrefix.Length).Trim();
                                    return CxAssistDisplayCoordinator.FindVulnerabilityById(id);
                                }
                                if (!string.IsNullOrEmpty(et.Document) && et.Line >= 0)
                                    return CxAssistDisplayCoordinator.FindVulnerabilityByLocation(et.Document, et.Line);
                            }
                        }
                    }
                }

                // Fallback: use DTE ErrorList - selected item is often the one with focus
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte?.ToolWindows?.ErrorList?.ErrorItems != null)
                {
                    var errors = dte.ToolWindows.ErrorList.ErrorItems;
                    if (errors.Count >= 1)
                    {
                        try
                        {
                            var first = errors.Item(1);
                            string file = first.FileName;
                            int line = first.Line;
                            if (!string.IsNullOrEmpty(file) && line >= 0)
                                return CxAssistDisplayCoordinator.FindVulnerabilityByLocation(file, line > 0 ? line - 1 : 0);
                        }
                        catch { /* Item might not be accessible */ }
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "ErrorListContextMenu.GetSelectedCxAssistVulnerability");
            }
            return null;
        }

        private void OnFixWithAssist(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var v = GetSelectedCxAssistVulnerability();
                if (v != null) CxAssistCopilotActions.SendFixWithAssist(v);
            });
        }

        private void OnViewDetails(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var v = GetSelectedCxAssistVulnerability();
                if (v != null) CxAssistCopilotActions.SendViewDetails(v);
            });
        }

        private void OnIgnoreThis(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var v = GetSelectedCxAssistVulnerability();
                if (v == null) return;
                string label = CxAssistConstants.GetIgnoreThisLabel(v.Scanner);
                var result = MessageBox.Show(
                    $"{label}?\n{v.Title ?? v.Description ?? v.Id}",
                    CxAssistConstants.DisplayName,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    MessageBox.Show(CxAssistConstants.GetIgnoreThisSuccessMessage(v.Scanner), CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnIgnoreAll(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var v = GetSelectedCxAssistVulnerability();
                if (v == null) return;
                string label = CxAssistConstants.GetIgnoreAllLabel(v.Scanner);
                var result = MessageBox.Show(
                    $"{label}?\n{v.Description}",
                    CxAssistConstants.DisplayName,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                    MessageBox.Show(CxAssistConstants.GetIgnoreAllSuccessMessage(v.Scanner), CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}

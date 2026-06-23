using System;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using System.Collections.Generic;
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

        private readonly AsyncPackage _package;
        private readonly OleMenuCommandService _commandService;

        private ErrorListContextMenuCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            AddCommand(FixCommandId, OnFixWithAssist);
            AddCommand(ViewDetailsCommandId, OnViewDetails);
            AddCommand(IgnoreThisCommandId, OnIgnoreThis, v => CxAssistConstants.GetIgnoreThisLabel(v.Scanner));
            AddCommand(IgnoreAllCommandId, OnIgnoreAll, v => CxAssistConstants.GetIgnoreAllLabel(v.Scanner),
                v => CxAssistConstants.ShouldShowIgnoreAll(v.Scanner));
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
            var id = new CommandID(CommandGuids.ErrorListCommandSetGuid, commandId);
            var cmd = new OleMenuCommand(invokeHandler, id);
            cmd.BeforeQueryStatus += (s, e) =>
            {
                CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: BeforeQueryStatus called for command {commandId}");
                var v = GetSelectedCxAssistVulnerability();
                bool visible = v != null && (isVisible == null || isVisible(v));
                CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Command {commandId} - vulnerability={v?.Title ?? "null"}, visible={visible}");
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
                    CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu: IVsTaskList2 available");
                    if (errorList.EnumSelectedItems(out var enumItems) == 0 && enumItems != null)
                    {
                        var items = new IVsTaskItem[1];
                        var fetchedArray = new uint[1];
                        if (enumItems.Next(1, items, fetchedArray) == 0 && fetchedArray[0] > 0 && items[0] != null)
                        {
                            CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu: Found selected item");
                            // Selected item may be our ErrorTask (has HelpKeyword, Document, Line)
                            if (items[0] is ErrorTask et)
                            {
                                CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Selected is ErrorTask - HelpKeyword='{et.HelpKeyword}', Document='{et.Document}', Line={et.Line}");
                                if (!string.IsNullOrEmpty(et.HelpKeyword) && et.HelpKeyword.StartsWith(CxAssistErrorListSync.HelpKeywordPrefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    string id = et.HelpKeyword.Substring(CxAssistErrorListSync.HelpKeywordPrefix.Length).Trim();
                                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Extracted ID={id}");
                                    var v = CxAssistDisplayCoordinator.FindVulnerabilityById(id);
                                    if (v != null)
                                    {
                                        CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Found vulnerability: {v.Title}");
                                        return v;
                                    }
                                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Vulnerability not found by ID");
                                }

                                if (!string.IsNullOrEmpty(et.Document) && et.Line >= 0)
                                {
                                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Trying location lookup: {et.Document}:{et.Line}");
                                    var v = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(et.Document, et.Line);
                                    if (v != null)
                                    {
                                        CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Found by location: {v.Title}");
                                        return v;
                                    }
                                }
                            }
                            else
                            {
                                CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Item not ErrorTask, type={items[0]?.GetType().Name ?? "null"}");
                            }
                        }
                    }
                }
                else
                {
                    CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu: IVsTaskList2 not available");
                }

                // Fallback: use DTE ErrorList - iterate through all items to find selected one
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte?.ToolWindows?.ErrorList?.ErrorItems != null)
                {
                    var errors = dte.ToolWindows.ErrorList.ErrorItems;
                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Fallback DTE method - {errors.Count} error items");

                    if (errors.Count >= 1)
                    {
                        try
                        {
                            // Iterate through items to find CxAssist ones
                            for (int i = 1; i <= errors.Count; i++)
                            {
                                var item = errors.Item(i);
                                if (item == null) continue;

                                string file = item.FileName;
                                int line = item.Line;
                                if (!string.IsNullOrEmpty(file))
                                {
                                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Checking item {i} - {file}:{line}");
                                    var v = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(file, line > 0 ? line - 1 : 0);
                                    if (v != null)
                                    {
                                        CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Found vulnerability at item {i}");
                                        return v;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Error iterating items: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "ErrorListContextMenu.GetSelectedCxAssistVulnerability");
                CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu: Exception in GetSelectedCxAssistVulnerability: {ex.Message}");
            }
            CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu: Failed to find CxAssist vulnerability");
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
                try
                {
                    CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu.OnIgnoreThis: Handler called");
                    var v = GetSelectedCxAssistVulnerability();
                    if (v == null)
                    {
                        CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu.OnIgnoreThis: No vulnerability found");
                        return;
                    }
                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu.OnIgnoreThis: Ignoring {v.Title}");
                    IgnoreManager.AddIgnoredEntry(v);
                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                    if (dte?.StatusBar != null)
                        dte.StatusBar.Text = CxAssistConstants.GetIgnoreThisSuccessMessage(v);
                    CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu.OnIgnoreThis: Success");
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "ErrorListContextMenu.OnIgnoreThis");
                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu.OnIgnoreThis: Exception - {ex.Message}");
                }
            });
        }

        private void OnIgnoreAll(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu.OnIgnoreAll: Handler called");
                    var v = GetSelectedCxAssistVulnerability();
                    if (v == null)
                    {
                        CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu.OnIgnoreAll: No vulnerability found");
                        return;
                    }
                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu.OnIgnoreAll: Ignoring all of type {v.Scanner}");
                    var all = CxAssistDisplayCoordinator.GetCurrentFindings() ?? new List<Vulnerability>();
                    IgnoreManager.AddAllIgnoredEntry(v, all);
                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                    if (dte?.StatusBar != null)
                        dte.StatusBar.Text = CxAssistConstants.GetIgnoreAllSuccessMessage(v.Scanner);
                    CxAssistOutputPane.WriteToOutputPane("ErrorListContextMenu.OnIgnoreAll: Success");
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "ErrorListContextMenu.OnIgnoreAll");
                    CxAssistOutputPane.WriteToOutputPane($"ErrorListContextMenu.OnIgnoreAll: Exception - {ex.Message}");
                }
            });
        }
    }
}

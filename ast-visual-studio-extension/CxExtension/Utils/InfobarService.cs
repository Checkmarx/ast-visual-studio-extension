using ast_visual_studio_extension.CxExtension.Toolbar;
using Microsoft;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class InfobarService : IVsInfoBarUIEvents
    {
        private readonly IServiceProvider serviceProvider;
        private uint cookie;

        private InfobarService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        private static InfobarService Instance { get; set; }

        public static InfobarService Initialize(IServiceProvider serviceProvider)
        {
            if(Instance == null)
            {
                Instance = new InfobarService(serviceProvider);
            }
            
            return Instance;
        }

        /// <summary>
        /// Show a message in the info bar
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageSeverity"></param>
        /// <returns></returns>
        public async Task ShowInfoBarAsync(string message, ImageMoniker messageSeverity, bool autoDismiss)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            InfoBarTextSpan text = new InfoBarTextSpan(message);
            InfoBarTextSpan[] spans = new InfoBarTextSpan[] { text };
            InfoBarModel infoBarModel = new InfoBarModel(spans, messageSeverity, isCloseButtonVisible: true);
            await DisplayInfoBarAsync(infoBarModel, autoDismiss);
        }

        /// <summary>
        /// Show a message in the info bar with a http link
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageSeverity"></param>
        /// <param name="linkDisplayName"></param>
        /// <param name="linkId"></param>
        /// <returns></returns>
        public async Task ShowInfoBarWithLinkAsync(string message, ImageMoniker messageSeverity, string linkDisplayName, string linkId, bool autoDismiss)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            InfoBarTextSpan text = new InfoBarTextSpan(message);
            InfoBarHyperlink hyperLink = new InfoBarHyperlink(linkDisplayName, linkId);
            InfoBarTextSpan[] spans = new InfoBarTextSpan[] { text };
            InfoBarActionItem[] actions = new InfoBarActionItem[] { hyperLink };
            InfoBarModel infoBarModel = new InfoBarModel(spans, actions, messageSeverity, isCloseButtonVisible: true);
            await DisplayInfoBarAsync(infoBarModel, autoDismiss);
        }

        /// <summary>
        /// Show info bar
        /// </summary>
        /// <param name="infoBarModel"></param>
        /// <returns></returns>
        private async Task DisplayInfoBarAsync(InfoBarModel infoBarModel, bool autoDismiss)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;

            Assumes.Present(factory);

            IVsInfoBarUIElement element = factory.CreateInfoBar(infoBarModel);

            element.Advise(this, out cookie);

            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);

                var host = (IVsInfoBarHost)obj;
                if (host == null)
                {
                    return;
                }

                host.AddInfoBar(element);

                if (autoDismiss)
                {
                    await Task.Delay(10000);
                    host.RemoveInfoBar(element);
                }
            }
        }

        /// <summary>
        /// Trigerred when a info bar link is clicked
        /// </summary>
        /// <param name="infoBarUIElement"></param>
        /// <param name="actionItem"></param>
        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string actionId = (string) actionItem.ActionContext;

            bool isGuid = Guid.TryParse(actionId, out var guid);

            if (string.Equals(actionId, CxConstants.CODEBASHING_OPEN_HTTP_LINK_ID, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Process.Start(actionItem.Text);
            }
            else if (string.Equals(actionId, CxConstants.RUN_SCAN_ACTION, StringComparison.OrdinalIgnoreCase))
            {
                _ = CxToolbar.instance.ScanStartedAsync();
            }
            else if (isGuid)
            {
                _ = CxToolbar.instance.ScansCombobox.LoadScanByIdAsync(actionId);
            }
            infoBarUIElement.Close();
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            infoBarUIElement.Unadvise(cookie);
        }
    }
}
